# Real-time LLM-powered Interview Simulator
<p align="center">
  <video src="Assets/demo.mp4" controls autoplay loop muted></video>
</p>
A Unity VR interview simulator that lets a candidate practice a spoken technical interview with an LLM-powered interviewer. The Unity client presents the VR scene and interview UI, records the cand

idate's answer, sends audio to a Python backend for transcription, asks an LLM for the next interviewer question, and plays the interviewer response back through text-to-speech.

The current project is set up for Unity 6, Universal Render Pipeline, OpenXR, Meta OpenXR, and XR Interaction Toolkit.

## Features

- VR interview scene in `Assets/Scenes/MainScene.unity`
- Interview setup for job category, difficulty, and interviewer personality
- LLM-generated opening question, follow-up questions, and final feedback
- Voice answer recording from Unity microphone input
- Speech-to-text backend endpoint for candidate answers
- Text-to-speech backend endpoint for interviewer audio
- Optional text answer flow for testing without voice input
- Mock LLM mode in Unity for UI testing without backend calls

## Architecture

```text
Unity client
  Assets/Scripts/InterviewSessionManager.cs
  Assets/Scripts/LLMInterviewService.cs
  Assets/Scripts/VoiceAnswerRecorder.cs
  Assets/Scripts/TextToSpeechPlayer.cs
  Assets/Scripts/BackendConfig.cs

FastAPI backend
  Backend/server.py
  Backend/requirements.txt
  Backend/Dockerfile
```

Runtime flow:

1. Unity starts an interview from `MainScene`.
2. Unity calls `POST /interview` to get the first question.
3. Backend uses a DeepSeek-compatible chat API for interviewer text generation.
4. Unity calls `POST /tts` to turn interviewer text into audio.
5. Candidate records an answer in Unity.
6. Unity sends the WAV audio to `POST /transcribe`.
7. Backend uses OpenAI transcription and returns text.
8. Unity sends the transcript and interview history back to `POST /interview` for the next follow-up.

## Requirements

- Unity `6000.0.62f1`
- Unity modules:
  - Android Build Support, if building for Meta Quest or Android
  - OpenXR support
- Python `3.10+` or `3.11`
- API keys:
  - `OPENAI_API_KEY` for transcription and text-to-speech
  - `DEEPSEEK_API_KEY` for interview question generation
- A microphone for voice input
- Optional: Meta Quest or another OpenXR-compatible headset

## Repository Layout

```text
Assets/              Unity scenes, scripts, prefabs, models, textures, and other project assets
Packages/            Unity package manifest and lock file
ProjectSettings/     Unity project settings
Backend/             FastAPI backend for LLM, transcription, and text-to-speech
.gitignore           Excludes Unity caches, local secrets, generated audio, and build outputs
```

Important generated folders such as `Library/`, `Logs/`, `UserSettings/`, `Backend/venv/`, and Unity package archives are intentionally ignored. Unity will regenerate `Library/` when the project is opened.

## Backend Setup

From the repository root:

```powershell
cd Backend
python -m venv venv
.\venv\Scripts\Activate.ps1
python -m pip install --upgrade pip
python -m pip install -r requirements.txt
```

Create `Backend/.env`:

```env
OPENAI_API_KEY=your_openai_key_here
DEEPSEEK_API_KEY=your_deepseek_key_here
```

`Backend/.env` is ignored by Git and should not be committed.

Run the backend locally:

```powershell
python -m uvicorn server:app --reload --host 0.0.0.0 --port 8000
```

Verify that it is running:

```powershell
Invoke-RestMethod http://127.0.0.1:8000/
```

Expected response:

```json
{
  "status": "Backend is running",
  "service": "LLM Interview Simulator Backend"
}
```

Optional backend debug endpoints:

- `GET /debug/env`
- `GET /debug/internet`
- `GET /debug/openai-auth`
- `GET /debug/openai-raw-auth`

## Backend API

### `POST /interview`

Generates the first question, a follow-up question, or final feedback.

Example body:

```json
{
  "task": "first_question",
  "jobCategory": "AI",
  "difficulty": "Medium",
  "personality": "Normal",
  "interviewHistory": "",
  "latestUserAnswer": ""
}
```

Supported `task` values:

- `first_question`
- `follow_up`
- `final_feedback`

### `POST /transcribe`

Accepts a multipart audio upload named `file` and returns transcript text.

### `POST /tts`

Accepts interviewer text and returns MP3 audio.

Example body:

```json
{
  "text": "Thanks for joining me today. Could you tell me about yourself?"
}
```

## Unity Setup

1. Open Unity Hub.
2. Add this project folder.
3. Open it with Unity `6000.0.62f1`.
4. Wait for Unity to restore packages and regenerate `Library/`.
5. Open `Assets/Scenes/MainScene.unity`.
6. Find the GameObject that has the `BackendConfig` component.
7. Set `Backend Base Url`:
   - Unity Editor with local backend: `http://127.0.0.1:8000`
   - Quest/device on the same network: `http://<your-computer-lan-ip>:8000`
   - Deployed backend: use the Cloud Run URL already configured in `BackendConfig.cs`
8. Press Play.
9. Choose interview settings and start the interview.
10. Answer with the text input or use the record/stop voice buttons.

Note: `127.0.0.1` only points to the same machine. If the app runs on a Quest headset, use your computer's LAN IP address or a deployed backend URL instead.

## Running Without Backend Calls

For quick UI testing, select the GameObject with `LLMInterviewService` and enable `Use Mock LLM` in the Inspector. This bypasses the backend for interview text generation. Voice transcription and text-to-speech still require backend routes unless those UI actions are avoided.

## Android / Quest Build Notes

1. Install Unity Android Build Support.
2. In Unity, open Build Profiles or Build Settings.
3. Select Android as the target platform.
4. Confirm OpenXR and Meta OpenXR settings are enabled in Project Settings.
5. Make sure microphone permission is allowed on the device.
6. Use a reachable backend URL in `BackendConfig`.
7. Build and run on the headset.

The Android package identifier is configured as:

```text
com.CSE566_assignment4
```

## Docker Backend

The backend includes a Dockerfile:

```powershell
cd Backend
docker build -t interview-backend .
docker run --env-file .env -p 8080:8080 interview-backend
```

When running this container locally, set Unity's backend URL to:

```text
http://127.0.0.1:8080
```

For a physical headset, replace `127.0.0.1` with the host machine's LAN IP or deploy the container to a public service such as Cloud Run.

## GitHub Upload Notes

Commit the source project files:

- `Assets/`
- `Packages/`
- `ProjectSettings/`
- `Backend/server.py`
- `Backend/requirements.txt`
- `Backend/Dockerfile`
- `.gitignore`
- `README.md`


## Reproducing From a Fresh Clone

1. Clone the repository.
2. Install Unity `6000.0.62f1`.
3. Open the project folder in Unity Hub.
4. Let Unity restore packages and rebuild `Library/`.
5. Create `Backend/.env` with `OPENAI_API_KEY` and `DEEPSEEK_API_KEY`.
6. Create and activate the Python virtual environment.
7. Install backend dependencies with `python -m pip install -r Backend/requirements.txt`.
8. Start the backend with `python -m uvicorn server:app --reload --host 0.0.0.0 --port 8000` from inside `Backend/`.
9. Set `BackendConfig.backendBaseUrl` in Unity to the running backend URL.
10. Open `Assets/Scenes/MainScene.unity` and press Play.

## Troubleshooting

- Backend returns API errors: verify `Backend/.env` exists and both API keys are set.
- Unity cannot connect to backend: confirm the backend URL in `BackendConfig` and check firewall/network access.
- Quest cannot connect to local backend: use the computer's LAN IP, not `127.0.0.1`.
- Microphone input fails on Android: grant microphone permission on the device.
- No interviewer audio plays: check the `TextToSpeechPlayer` AudioSource assignment and backend `/tts` response.
- Unity imports slowly after clone: this is expected because `Library/` is regenerated locally.
