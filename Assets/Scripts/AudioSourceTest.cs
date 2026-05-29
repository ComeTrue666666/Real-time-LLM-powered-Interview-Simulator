using UnityEngine;

public class AudioSourceTest : MonoBehaviour
{
    public AudioSource audioSource;

    void Start()
    {
        if (audioSource == null)
        {
            Debug.LogError("AudioSourceTest: audioSource is not assigned.");
            return;
        }

        AudioClip testClip = AudioClip.Create(
            "TestBeep",
            44100,
            1,
            44100,
            false
        );

        float[] samples = new float[44100];

        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = Mathf.Sin(2 * Mathf.PI * 440 * i / 44100f) * 0.2f;
        }

        testClip.SetData(samples, 0);

        audioSource.clip = testClip;
        audioSource.Play();

        Debug.Log("AudioSourceTest: playing test beep.");
    }
}