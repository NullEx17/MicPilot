using NAudio.Wave;

namespace MicPilot.Audio;

internal static class AudioLevelMeter
{
    public static float CalculatePeak(byte[] buffer, int bytesRecorded, WaveFormat format)
    {
        if (bytesRecorded <= 0)
        {
            return 0f;
        }

        if (format.Encoding == WaveFormatEncoding.IeeeFloat)
        {
            var sampleCount = bytesRecorded / sizeof(float);
            var max = 0f;

            for (var i = 0; i < sampleCount; i++)
            {
                var sample = Math.Abs(BitConverter.ToSingle(buffer, i * sizeof(float)));
                if (sample > max)
                {
                    max = sample;
                }
            }

            return Math.Clamp(max, 0f, 1f);
        }

        if (format.BitsPerSample == 16)
        {
            var sampleCount = bytesRecorded / sizeof(short);
            var max = 0f;

            for (var i = 0; i < sampleCount; i++)
            {
                var sample = Math.Abs(BitConverter.ToInt16(buffer, i * sizeof(short)) / 32768f);
                if (sample > max)
                {
                    max = sample;
                }
            }

            return Math.Clamp(max, 0f, 1f);
        }

        return 0f;
    }
}
