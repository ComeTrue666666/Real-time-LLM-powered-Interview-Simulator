from pydantic import BaseModel
from dotenv import load_dotenv
from fastapi import FastAPI, UploadFile, File, Body
from openai import OpenAI
from fastapi.responses import FileResponse
from pathlib import Path
import tempfile
import httpx
import os

load_dotenv()

app = FastAPI()
@app.get("/debug/env")
def debug_env():
    openai_key = os.getenv("OPENAI_API_KEY", "").strip()
    deepseek_key = os.getenv("DEEPSEEK_API_KEY")

    return {
        "openai_key_exists": openai_key is not None and len(openai_key) > 0,
        "openai_key_prefix": openai_key[:7] if openai_key else None,
        "deepseek_key_exists": deepseek_key is not None and len(deepseek_key) > 0,
        "deepseek_key_prefix": deepseek_key[:7] if deepseek_key else None,
    }


@app.get("/debug/internet")
def debug_internet():
    try:
        response = httpx.get("https://api.openai.com/v1/models", timeout=10)
        return {
            "status_code": response.status_code,
            "response_preview": response.text[:300]
        }
    except Exception as e:
        return {
            "error_type": type(e).__name__,
            "error_message": str(e)
        }
    

@app.get("/debug/openai-auth")
def debug_openai_auth():
    try:
        client = OpenAI(api_key=os.getenv("OPENAI_API_KEY", "").strip())

        response = client.chat.completions.create(
            model="gpt-4o-mini",
            messages=[
                {"role": "user", "content": "Say OK only."}
            ],
            max_tokens=5
        )

        return {
            "success": True,
            "message": response.choices[0].message.content
        }

    except Exception as e:
        return {
            "success": False,
            "error_type": type(e).__name__,
            "error_message": repr(e)
        }   
    
@app.get("/debug/openai-raw-auth")
def debug_openai_raw_auth():
    try:
        openai_key = os.getenv("OPENAI_API_KEY", "").strip()

        if not openai_key:
            return {
                "success": False,
                "error": "OPENAI_API_KEY is missing"
            }

        response = httpx.post(
            "https://api.openai.com/v1/chat/completions",
            headers={
                "Authorization": f"Bearer {openai_key}",
                "Content-Type": "application/json"
            },
            json={
                "model": "gpt-4o-mini",
                "messages": [
                    {"role": "user", "content": "Say OK only."}
                ],
                "max_tokens": 5
            },
            timeout=30
        )

        return {
            "success": response.status_code == 200,
            "status_code": response.status_code,
            "response_preview": response.text[:500]
        }

    except Exception as e:
        return {
            "success": False,
            "error_type": type(e).__name__,
            "error_message": repr(e)
        }


class TTSRequest(BaseModel):
    text: str

BASE_DIR = Path(__file__).resolve().parent
TTS_OUTPUT_PATH = Path("/tmp/debug_interviewer_voice.mp3")

deepseek_api_key = os.getenv("DEEPSEEK_API_KEY", "").strip()
openai_api_key = os.getenv("OPENAI_API_KEY", "").strip()

deepseek_client = OpenAI(
    api_key=deepseek_api_key,
    base_url="https://api.deepseek.com"
)

openai_client = OpenAI(
    api_key=openai_api_key
)


class InterviewRequest(BaseModel):
    task: str
    jobCategory: str
    difficulty: str
    personality: str
    interviewHistory: str = ""
    latestUserAnswer: str = ""


@app.get("/")
def health_check():
    return {
        "status": "Backend is running",
        "service": "LLM Interview Simulator Backend"
    }


@app.post("/interview")
def interview(request: InterviewRequest):
    system_prompt = f"""
You are a technical interviewer inside a VR interview simulator.

Interview settings:
- Job category: {request.jobCategory}
- Difficulty: {request.difficulty}
- Interviewer personality: {request.personality}

Your goal is to create a natural spoken interview, not a written exam.
Use the interview history to remember what the candidate already said.
Do not repeat the same topic unless you are intentionally digging deeper.
When useful, refer back to an earlier answer naturally.

Behavior rules:
- Sound like a real person speaking out loud.
- Be conversational, agile, and responsive.
- React briefly to the candidate's answer before asking the next question.
- Refer to specific details from the candidate's previous answer when possible.
- Ask only one main question at a time.
- Do not give long lectures.
- Do not sound like a chatbot.
- Do not say things like "As an AI" or "I am an AI interviewer."
- Avoid rigid phrases like "Next question" or "Question number".
- Avoid bullet points unless giving final feedback.
- Keep each normal interviewer response around 2 to 5 short sentences.
- Use natural transitions like:
  "Got it."
  "That makes sense."
  "Interesting, let’s dig into that."
  "Okay, let me push a little deeper there."
  "I see where you're going with that."

Interview style:
- If the candidate gives a strong answer, acknowledge it briefly and ask a deeper follow-up.
- If the answer is vague, politely ask for a concrete example.
- If the answer is incorrect, do not immediately reveal the answer. Ask a guiding follow-up.
- If the candidate sounds nervous or unclear, keep the tone professional but supportive.
- Make the interview feel dynamic and human.

Difficulty behavior:
- Easy: ask simpler questions and give more supportive transitions.
- Medium: ask practical follow-ups and expect some detail.
- Hard: challenge assumptions, ask tradeoffs, edge cases, and deeper reasoning.

Personality behavior:
- Friendly: Use a warm and encouraging tone.
            Use short supportive reactions.
            Example style:
            "Nice, that's a solid start."
            "That's okay, let's think through it together."
- Normal: professional, balanced, realistic.
- Unfriendly: Use a serious, skeptical interview tone.
                Challenge vague answers.
                Do not insult the candidate.
                Example style:
                "I'm not fully convinced by that yet."
                "That's a bit general. Can you be more specific?"

Important output rule:
Return only the exact words the interviewer should say aloud.
Do not include labels like "Interviewer:".
Do not include stage directions.
"""

    if request.task == "first_question":
        user_prompt = """
Start the interview naturally.

The candidate is interviewing for a {request.jobCategory} role.

Ask a warm opening question that feels like the beginning of a real interview.

Do not say "Question 1".
Do not explain the interview rules.
Do not ask multiple questions.

A good style would be similar to:
"Thanks for joining me today. To start, could you tell me a little about yourself and what interested you in this role?"

Now generate the opening interviewer line.
"""

    elif request.task == "follow_up":
        user_prompt = f"""
Here is the interview conversation so far:

{request.interviewHistory}

The candidate's latest answer was:

{request.latestUserAnswer}

Generate the interviewer's next spoken response.

The response should:
1. Briefly react to the candidate's answer.
2. Mention one specific idea from their answer if possible.
3. Ask one natural follow-up question.
4. Keep it short and spoken.
5. Avoid sounding like a test generator.

Do not say "Question".
Do not include "Interviewer:".
Do not use bullet points.
Do not give final feedback yet.

Generate only the next interviewer line.
"""

    elif request.task == "final_feedback":
        user_prompt = f"""
The interview is now finished.

Here is the full conversation:

{request.interviewHistory}

Give final feedback to the candidate.

The feedback should include:
- A short realistic closing sentence.
- Score out of 10.
- 2 strengths.
- 2 areas to improve.
- 1 practical next-step suggestion.

Tone should match the interviewer personality:
{request.personality}

Keep it clear and not too long.

Use this format:

Thanks for going through the interview with me.

Score: X/10

Strengths:
- ...
- ...

Areas to Improve:
- ...
- ...

Next Step:
- ...
"""

    else:
        return {"text": "Error: unknown task type."}

    try:
        response = deepseek_client.chat.completions.create(
            model="deepseek-v4-flash",
            messages=[
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_prompt}
            ],
            temperature=0.7,
            max_tokens=500
        )

        text = response.choices[0].message.content
        return {"text": text}

    except Exception as e:
        print("LLM API error type:", type(e).__name__, flush=True)
        print("LLM API error message:", repr(e), flush=True)
        print("LLM API error cause type:", type(e.__cause__).__name__ if e.__cause__ else None, flush=True)
        print("LLM API error cause message:", repr(e.__cause__) if e.__cause__ else None, flush=True)

        return {
            "text": "LLM API error: " + type(e).__name__ + " - " + repr(e)
    }


@app.post("/transcribe")
async def transcribe_audio(file: UploadFile = File(...)):

    audio_bytes = await file.read()

    debug_path = os.path.join(os.path.dirname(__file__), "debug_answer.wav")

    # DEBUG: save the audio received from Unity
    with open(debug_path, "wb") as f:
        f.write(audio_bytes)

    print("Saved debug audio to:", debug_path)
    print("Received file:", file.filename)
    print("Content type:", file.content_type)
    print("Audio size:", len(audio_bytes), "bytes")

    if len(audio_bytes) < 1000:
        return {
            "text": "",
            "error": "Audio file too small or empty"
        }

    temp_audio_path = None

    try:
        suffix = os.path.splitext(file.filename)[1]

        if suffix == "":
            suffix = ".wav"

        with tempfile.NamedTemporaryFile(delete=False, suffix=suffix) as temp_audio:
            temp_audio.write(audio_bytes)
            temp_audio_path = temp_audio.name

        print("Temporary audio path:", temp_audio_path)
        print("Temporary audio size:", os.path.getsize(temp_audio_path), "bytes")

        with open(temp_audio_path, "rb") as audio_file:
            transcript = openai_client.audio.transcriptions.create(
                model="gpt-4o-mini-transcribe",
                file=audio_file
            )

        print("Transcript:", transcript.text)

        return {
            "text": transcript.text
        }

    except Exception as e:
        print("Transcription error type:", type(e).__name__, flush=True)
        print("Transcription error message:", repr(e), flush=True)
        print("Transcription error cause type:", type(e.__cause__).__name__ if e.__cause__ else None, flush=True)
        print("Transcription error cause message:", repr(e.__cause__) if e.__cause__ else None, flush=True)

        return {
            "text": "",
            "error": type(e).__name__ + " - " + repr(e)
        }

    finally:
        if temp_audio_path and os.path.exists(temp_audio_path):
            os.remove(temp_audio_path)



@app.post("/tts")
async def text_to_speech(request: TTSRequest):
    """
    Convert interviewer text into a WAV audio file.
    Unity will call this endpoint and play the returned audio.
    """

    text = request.text.strip()

    if not text:
        return {"error": "No text provided for TTS."}

    print("TTS text received:", text)

    try:
        with openai_client.audio.speech.with_streaming_response.create(
            model="gpt-4o-mini-tts",
            voice="onyx",
            input=text,
            instructions="Speak like a professional but friendly job interviewer. Clear, calm, and natural.",
            response_format="mp3",
        ) as response:
            response.stream_to_file(TTS_OUTPUT_PATH)

        print(f"Saved interviewer voice to: {TTS_OUTPUT_PATH}")

        return FileResponse(
            path=TTS_OUTPUT_PATH,
            media_type="audio/mpeg",
            filename="interviewer_voice.mp3"
        )

    except Exception as e:
        print("TTS error:", str(e))
        return {"error": str(e)}