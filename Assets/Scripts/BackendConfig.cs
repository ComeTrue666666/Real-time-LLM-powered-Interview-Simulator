using UnityEngine;

public class BackendConfig : MonoBehaviour
{
    [Header("Cloud Backend")]
    public string backendBaseUrl = "https://interview-backend-488533653640.us-east1.run.app";

    public string InterviewUrl
    {
        get { return backendBaseUrl.TrimEnd('/') + "/interview"; }
    }

    public string TranscribeUrl
    {
        get { return backendBaseUrl.TrimEnd('/') + "/transcribe"; }
    }

    public string TtsUrl
    {
        get { return backendBaseUrl.TrimEnd('/') + "/tts"; }
    }
}