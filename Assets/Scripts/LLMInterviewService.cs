using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class LLMInterviewService : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool useMockLLM = false;

    [Header("Backend Settings")]
    public BackendConfig backendConfig;

    [System.Serializable]
    public class InterviewRequest
    {
        public string task;
        public string jobCategory;
        public string difficulty;
        public string personality;
        public string interviewHistory;
        public string latestUserAnswer;
    }

    [System.Serializable]
    public class InterviewResponse
    {
        public string text;
    }

    public IEnumerator GenerateFirstQuestion(
        string jobCategory,
        string difficulty,
        string personality,
        System.Action<string> onQuestionReady
    )
    {
        if (useMockLLM)
        {
            yield return new WaitForSeconds(1f);

            string mockQuestion =
                "Let's begin. You are interviewing for a " + jobCategory +
                " role. Can you introduce yourself and explain why you are interested in this position?";

            onQuestionReady?.Invoke(mockQuestion);
            yield break;
        }

        InterviewRequest request = new InterviewRequest
        {
            task = "first_question",
            jobCategory = jobCategory,
            difficulty = difficulty,
            personality = personality,
            interviewHistory = "",
            latestUserAnswer = ""
        };

        yield return SendRequestToBackend(request, onQuestionReady);
    }

    public IEnumerator GenerateFollowUpQuestion(
        string jobCategory,
        string difficulty,
        string personality,
        string interviewHistory,
        string latestUserAnswer,
        System.Action<string> onQuestionReady
    )
    {
        if (useMockLLM)
        {
            yield return new WaitForSeconds(1f);

            string mockQuestion =
                "Good. Now can you explain one technical concept related to this role in more detail?";

            onQuestionReady?.Invoke(mockQuestion);
            yield break;
        }

        InterviewRequest request = new InterviewRequest
        {
            task = "follow_up",
            jobCategory = jobCategory,
            difficulty = difficulty,
            personality = personality,
            interviewHistory = interviewHistory,
            latestUserAnswer = latestUserAnswer
        };

        yield return SendRequestToBackend(request, onQuestionReady);
    }

    public IEnumerator GenerateFinalFeedback(
        string jobCategory,
        string difficulty,
        string personality,
        string interviewHistory,
        System.Action<string> onFeedbackReady
    )
    {
        if (useMockLLM)
        {
            yield return new WaitForSeconds(1f);

            string mockFeedback =
                "Interview finished.\n\n" +
                "Final Feedback:\n" +
                "Score: 7/10\n\n" +
                "Strengths:\n" +
                "- You completed the full interview.\n" +
                "- Your answers were understandable.\n\n" +
                "Areas to Improve:\n" +
                "- Add more specific examples.\n" +
                "- Explain technical ideas step by step.\n\n" +
                "Advice:\n" +
                "- Practice using the STAR method.";

            onFeedbackReady?.Invoke(mockFeedback);
            yield break;
        }

        InterviewRequest request = new InterviewRequest
        {
            task = "final_feedback",
            jobCategory = jobCategory,
            difficulty = difficulty,
            personality = personality,
            interviewHistory = interviewHistory,
            latestUserAnswer = ""
        };

        yield return SendRequestToBackend(request, onFeedbackReady);
    }

    private IEnumerator SendRequestToBackend(
        InterviewRequest requestData,
        System.Action<string> onResultReady
    )
    {
        if (backendConfig == null)
        {
            Debug.LogError("BackendConfig is not assigned on LLMInterviewService.");

            onResultReady?.Invoke(
                "Error: BackendConfig is not assigned in Unity."
            );

            yield break;
        }

        string jsonBody = JsonUtility.ToJson(requestData);

        string url = backendConfig.InterviewUrl;

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.timeout = 60;

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log("LLM DEBUG: Sending request to backend.");
        Debug.Log("LLM DEBUG: Backend URL = " + url);
        Debug.Log("LLM DEBUG: Request JSON = " + jsonBody);

        yield return request.SendWebRequest();

        Debug.Log("LLM DEBUG: Request finished.");
        Debug.Log("LLM DEBUG: Result = " + request.result);
        Debug.Log("LLM DEBUG: Response Code = " + request.responseCode);
        Debug.Log("LLM DEBUG: Response Text = " + request.downloadHandler.text);

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Backend request failed: " + request.error);

            onResultReady?.Invoke(
                "Error: Could not connect to the LLM backend.\n\n" +
                "Check that the Python backend is running."
            );

            yield break;
        }

        string responseText = request.downloadHandler.text;

        Debug.Log("Raw backend response: " + responseText);

        InterviewResponse response = JsonUtility.FromJson<InterviewResponse>(responseText);

        if (response == null || string.IsNullOrEmpty(response.text))
        {
            onResultReady?.Invoke("Error: Empty response from LLM backend.");
        }
        else
        {
            onResultReady?.Invoke(response.text);
        }
    }
}