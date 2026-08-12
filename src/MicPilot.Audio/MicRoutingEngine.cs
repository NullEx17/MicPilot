using MicPilot.Core.Models;
using MicPilot.Diagnostics;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace MicPilot.Audio;

public sealed class MicRoutingEngine : IDisposable
{
    private const int OutputLatencyMs = 20;

    private readonly object _sync = new();

    private WasapiCapture? _capture;
    private WasapiOut? _output;
    private BufferedWaveProvider? _buffer;
    private MMDeviceEnumerator? _enumerator;

    private bool _routeMuted;
    private bool _disposed;

    public bool IsRunning { get; private set; }

    public double EstimatedLatencyMs { get; private set; }

    public event Action<float>? InputLevelChanged;

    public event Action<string>? ErrorOccurred;

    public void Start(string captureDeviceId, string renderDeviceId)
    {
        lock (_sync)
        {
            StopInternal();

            try
            {
                _enumerator = new MMDeviceEnumerator();

                using var captureDevice = _enumerator.GetDevice(captureDeviceId);
                using var renderDevice = _enumerator.GetDevice(renderDeviceId);

                _capture = new WasapiCapture(captureDevice);
                _buffer = new BufferedWaveProvider(_capture.WaveFormat)
                {
                    BufferDuration = TimeSpan.FromSeconds(2),
                    DiscardOnBufferOverflow = true,
                    ReadFully = true
                };

                _output = new WasapiOut(renderDevice, AudioClientShareMode.Shared, true, OutputLatencyMs);
                _output.Init(_buffer);

                _capture.DataAvailable += OnCaptureDataAvailable;
                _capture.RecordingStopped += OnRecordingStopped;

                _output.Play();
                _capture.StartRecording();

                EstimatedLatencyMs = EstimateLatencyMs(_capture.WaveFormat);
                IsRunning = true;

                Log.Info(
                    $"Audio engine started. Capture='{captureDevice.FriendlyName}', " +
                    $"Render='{renderDevice.FriendlyName}', Latency~{EstimatedLatencyMs:F0}ms");
            }
            catch (Exception ex)
            {
                StopInternal();
                Log.Error("MicPilot couldn't access your microphone.", ex);
                ErrorOccurred?.Invoke("MicPilot couldn't access your microphone.");
                throw;
            }
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            StopInternal();
        }
    }

    public void SetRouteMuted(bool muted)
    {
        _routeMuted = muted;
        Log.Info(muted ? "Game route muted (silence)" : "Game route unmuted");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
    }

    private void OnCaptureDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_buffer is null)
        {
            return;
        }

        var peak = AudioLevelMeter.CalculatePeak(e.Buffer, e.BytesRecorded, _buffer.WaveFormat);
        InputLevelChanged?.Invoke(peak);

        if (_routeMuted)
        {
            var silence = new byte[e.BytesRecorded];
            _buffer.AddSamples(silence, 0, silence.Length);
            return;
        }

        _buffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null)
        {
            Log.Error("Audio capture stopped unexpectedly.", e.Exception);
            ErrorOccurred?.Invoke("Microphone disconnected or unavailable.");
        }

        IsRunning = false;
    }

    private void StopInternal()
    {
        if (_capture is not null)
        {
            _capture.DataAvailable -= OnCaptureDataAvailable;
            _capture.RecordingStopped -= OnRecordingStopped;

            try
            {
                _capture.StopRecording();
            }
            catch
            {
                // Best effort shutdown.
            }

            _capture.Dispose();
            _capture = null;
        }

        if (_output is not null)
        {
            try
            {
                _output.Stop();
            }
            catch
            {
                // Best effort shutdown.
            }

            _output.Dispose();
            _output = null;
        }

        _buffer = null;
        _enumerator?.Dispose();
        _enumerator = null;
        IsRunning = false;
        EstimatedLatencyMs = 0;
    }

    private static double EstimateLatencyMs(WaveFormat format)
    {
        // Shared-mode WASAPI adds roughly one buffer period per direction.
        // This is an estimate, not a measured round-trip latency.
        var periodMs = format.SampleRate > 0 ? 1000.0 * 480 / format.SampleRate : 10;
        return OutputLatencyMs + periodMs;
    }
}
