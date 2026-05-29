using System.Collections;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Networking;

public class VoiceAnswerRecorder : MonoBehaviour
{
    [Header("Backend Settings")]
    public BackendConfig backendConfig;

    [Header("Interview References")]
    public InterviewSessionManager interviewSessionManager;
    public TMP_Text voiceStatusText;

    [Header("Recording Settings")]
    public int maxRecordingSeconds = 30;
    public int sampleRate = 16000;

    private AudioClip recordedClip;
    private string microphoneDevice;
    private bool isRecording = false;

    private void Start()
    {
        RequestMicrophonePermission();
    }

    private void RequestMicrophonePermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#endif
    }

    public void StartRecording()
    {

        if (!HasMicrophonePermission())
        {
            SetStatus("Microphone permission is not granted.");
            Debug.LogError("Cannot record because microphone permission is not granted.");
            RequestMicrophonePermission();
            return;
        }

        if (interviewSessionManager != null && !interviewSessionManager.CanAcceptVoiceAnswer())
        {
            SetStatus("Interview is paused or not ready for an answer.");
            Debug.LogWarning("Recording blocked because interview is not accepting answers.");
            return;
        }

        if (isRecording)
        {
            Debug.LogWarning("Already recording.");
            return;
        }

        if (Microphone.devices.Length == 0)
        {
            SetStatus("No microphone found.");
            Debug.LogError("No microphone device found.");
            return;
        }

        Debug.Log("MIC DEBUG | Number of microphone devices = " + Microphone.devices.Length);

        for (int i = 0; i < Microphone.devices.Length; i++)
        {
            Debug.Log("MIC DEBUG | Device " + i + " = " + Microphone.devices[i]);
        }

        microphoneDevice = Microphone.devices[0];

        int minFreq;
        int maxFreq;
        Microphone.GetDeviceCaps(microphoneDevice, out minFreq, out maxFreq);

        Debug.Log(
            "MIC CAPS DEBUG | device=" + microphoneDevice +
            " | minFreq=" + minFreq +
            " | maxFreq=" + maxFreq
        );

        recordedClip = Microphone.Start(
            microphoneDevice,
            false,
            maxRecordingSeconds,
            sampleRate
        );

        isRecording = true;

        SetStatus("Recording... Speak now.");
        Debug.Log("Voice recording started. Device: " + microphoneDevice);
    }

    public void StopRecordingAndTranscribe()
    {
        if (!isRecording)
        {
            Debug.LogWarning("Not recording.");
            SetStatus("Not recording.");
            return;
        }

        int recordingPosition = Microphone.GetPosition(microphoneDevice);

        Microphone.End(microphoneDevice);
        isRecording = false;

        SetStatus("Recording stopped. Transcribing...");
        Debug.Log("Voice recording stopped. Samples recorded: " + recordingPosition);

        if (recordingPosition <= 0)
        {
            SetStatus("No voice captured. Try again.");
            Debug.LogWarning("No microphone samples captured.");
            return;
        }

        AudioClip trimmedClip = TrimAudioClip(recordedClip, recordingPosition);
        

        LogAudioLoudness(trimmedClip);

        byte[] wavData = WavUtility.FromAudioClip(trimmedClip);

        Debug.Log(
            "VOICE DEBUG | " +
            "recordingPosition=" + recordingPosition +
            " | clipFrequency=" + trimmedClip.frequency +
            " | channels=" + trimmedClip.channels +
            " | clipLength=" + trimmedClip.length +
            " | wavBytes=" + wavData.Length
        );

        StartCoroutine(SendAudioToBackend(wavData));

    }

    private IEnumerator SendAudioToBackend(byte[] wavData)
    {
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavData, "answer.wav", "audio/wav");

        if (backendConfig == null)
     {
            SetStatus("BackendConfig is not assigned.");
            Debug.LogError("BackendConfig is not assigned on VoiceAnswerRecorder.");
            yield break;
        }

        string url = backendConfig.TranscribeUrl;               

        UnityWebRequest request = UnityWebRequest.Post(url, form);
        request.timeout = 60;

        Debug.Log("Sending audio to backend: " + url);

        yield return request.SendWebRequest();

        Debug.Log("Transcription request finished.");
        Debug.Log("Result: " + request.result);
        Debug.Log("Response Code: " + request.responseCode);
        Debug.Log("Response Text: " + request.downloadHandler.text);

        if (request.result != UnityWebRequest.Result.Success)
        {
            SetStatus("Transcription failed. Check backend.");
            Debug.LogError("Transcription failed: " + request.error);
            yield break;
        }

        TranscriptionResponse response =
            JsonUtility.FromJson<TranscriptionResponse>(request.downloadHandler.text);

        if (response == null)
        {
            SetStatus("Invalid transcription response.");
            Debug.LogError("Transcription response could not be parsed.");
            yield break;
        }

        if (string.IsNullOrEmpty(response.text))
        {
            string errorMessage = string.IsNullOrEmpty(response.error)
                ? "No error message returned by backend."
                : response.error;

            SetStatus("No transcript returned: " + errorMessage);
            Debug.LogError("Empty transcription text. Backend error: " + errorMessage);
            Debug.LogError("Raw transcription response: " + request.downloadHandler.text);
            yield break;
        }

        SetStatus("Answer received. Sending to interviewer...");
        Debug.Log("Transcript hidden from UI: " + response.text);

        if (interviewSessionManager == null)
        {
            SetStatus("Interview manager is not assigned.");
            Debug.LogError("InterviewSessionManager is not assigned on VoiceAnswerRecorder.");
            yield break;
        }

        interviewSessionManager.SubmitVoiceAnswer(response.text);
    }

    private AudioClip TrimAudioClip(AudioClip originalClip, int samplesRecorded)
    {
        float[] samples = new float[samplesRecorded * originalClip.channels];

        originalClip.GetData(samples, 0);

        AudioClip trimmedClip = AudioClip.Create(
            "TrimmedRecording",
            samplesRecorded,
            originalClip.channels,
            originalClip.frequency,
            false
        );

        trimmedClip.SetData(samples, 0);

        return trimmedClip;
    }

    private void LogAudioLoudness(AudioClip clip)
    {
        int sampleCount = clip.samples * clip.channels;
        float[] samples = new float[sampleCount];

        clip.GetData(samples, 0);

        float maxAbs = 0f;
        double sumSquares = 0.0;

        for (int i = 0; i < samples.Length; i++)
        {
            float abs = Mathf.Abs(samples[i]);

            if (abs > maxAbs)
            {
                maxAbs = abs;
            }

            sumSquares += samples[i] * samples[i];
        }

        double rms = Mathf.Sqrt((float)(sumSquares / samples.Length));

        Debug.Log(
            "VOICE LOUDNESS DEBUG | " +
            "maxAbs=" + maxAbs +
            " | rms=" + rms +
            " | samples=" + sampleCount
        );
}

    private void SetStatus(string message)
    {
        if (voiceStatusText != null)
        {
            voiceStatusText.text = message;
        }

        Debug.Log("Voice Status: " + message);
    }

    [System.Serializable]
    private class TranscriptionResponse
    {
        public string text;
        public string error;
    }

    private bool HasMicrophonePermission()
    {
    #if UNITY_ANDROID && !UNITY_EDITOR
        bool hasPermission = Permission.HasUserAuthorizedPermission(Permission.Microphone);
        Debug.Log("MIC PERMISSION DEBUG | Microphone permission granted = " + hasPermission);
        return hasPermission;
    #else
        Debug.Log("MIC PERMISSION DEBUG | Editor or non-Android, assuming permission granted.");
        return true;
    #endif
    }
}