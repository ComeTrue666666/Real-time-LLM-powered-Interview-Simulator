using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class TextToSpeechPlayer : MonoBehaviour
{
    [Header("Backend Settings")]
    public BackendConfig backendConfig;

    [Header("Audio")]
    public AudioSource interviewerAudioSource;

    [System.Serializable]
    private class TTSRequest
    {
        public string text;
    }

    public void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.LogWarning("TTS skipped because text is empty.");
            return;
        }

        StartCoroutine(SendTTSRequest(text));
    }

    private IEnumerator SendTTSRequest(string text)
    {
        if (backendConfig == null)
        {
            Debug.LogError("BackendConfig is not assigned on TextToSpeechPlayer.");
            yield break;
        }

        string url = backendConfig.TtsUrl;

        TTSRequest requestData = new TTSRequest
        {
            text = text
        };

        string jsonBody = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.timeout = 60;

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.MPEG);
        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log("Sending TTS request: " + text);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("TTS request failed: " + request.error);
            Debug.LogError("TTS response: " + request.downloadHandler.text);
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(request);

        if (clip == null)
        {
            Debug.LogError("TTS audio clip is null.");
            yield break;
        }

        Debug.Log(
            "TTS_CLIP_CHECK | " +
            "length=" + clip.length +
            " | samples=" + clip.samples +
            " | frequency=" + clip.frequency +
            " | channels=" + clip.channels
        );

        if (interviewerAudioSource == null)
        {
            Debug.LogError("Interviewer AudioSource is not assigned.");
            yield break;
        }

        Debug.Log(
            "TTS_AUDIOSOURCE_CHECK | " +
            "object=" + interviewerAudioSource.gameObject.name +
            " | enabled=" + interviewerAudioSource.enabled +
            " | volume=" + interviewerAudioSource.volume +
            " | mute=" + interviewerAudioSource.mute +
            " | spatialBlend=" + interviewerAudioSource.spatialBlend
        );

        interviewerAudioSource.Stop();
        interviewerAudioSource.clip = clip;
        interviewerAudioSource.volume = 1f;
        interviewerAudioSource.mute = false;
        interviewerAudioSource.spatialBlend = 0f;
        interviewerAudioSource.Play();

        Debug.Log(
            "TTS_PLAY_CHECK | " +
            "isPlaying=" + interviewerAudioSource.isPlaying +
            " | clipName=" + interviewerAudioSource.clip.name +
            " | clipLength=" + interviewerAudioSource.clip.length
        );

        yield return new WaitForSeconds(0.2f);

        Debug.Log(
            "TTS_PLAY_CHECK_AFTER_WAIT | " +
            "isPlaying=" + interviewerAudioSource.isPlaying +
            " | time=" + interviewerAudioSource.time
        );
        
    }
}