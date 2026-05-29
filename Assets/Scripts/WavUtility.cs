using System;
using System.IO;
using UnityEngine;

public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            int sampleCount = clip.samples * clip.channels;
            float[] samples = new float[sampleCount];
            clip.GetData(samples, 0);

            byte[] pcmData = ConvertFloatTo16BitPCM(samples);

            int hz = clip.frequency;
            int channels = clip.channels;
            int byteRate = hz * channels * 2;

            WriteString(stream, "RIFF");
            WriteInt(stream, 36 + pcmData.Length);
            WriteString(stream, "WAVE");

            WriteString(stream, "fmt ");
            WriteInt(stream, 16);
            WriteShort(stream, 1);
            WriteShort(stream, (short)channels);
            WriteInt(stream, hz);
            WriteInt(stream, byteRate);
            WriteShort(stream, (short)(channels * 2));
            WriteShort(stream, 16);

            WriteString(stream, "data");
            WriteInt(stream, pcmData.Length);
            stream.Write(pcmData, 0, pcmData.Length);

            return stream.ToArray();
        }
    }

    private static byte[] ConvertFloatTo16BitPCM(float[] samples)
    {
        byte[] pcmData = new byte[samples.Length * 2];

        int index = 0;

        foreach (float sample in samples)
        {
            float clampedSample = Mathf.Clamp(sample, -1f, 1f);
            short intSample = (short)(clampedSample * short.MaxValue);

            byte[] bytes = BitConverter.GetBytes(intSample);

            pcmData[index++] = bytes[0];
            pcmData[index++] = bytes[1];
        }

        return pcmData;
    }

    private static void WriteString(Stream stream, string value)
    {
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static void WriteInt(Stream stream, int value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static void WriteShort(Stream stream, short value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }
}