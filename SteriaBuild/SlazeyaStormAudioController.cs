using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

/// <summary>Optional instance audio companion. The accepted visual driver remains the combat clock.</summary>
public sealed class SlazeyaStormAudioController : IDisposable
{
    public const int SampleRate = 44100;
    public const float GatherGain = 0.30f;
    public const float BurstGain = 0.78f;
    public const float GatherRelease = 0.04f;
    public const int GatherFrames = 44100;
    public const int BurstFrames = 52920;
    public const int MaxWaveBytes = 512 * 1024;

    private static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
    private readonly Func<float> _readEffectVolume;
    private GameObject _root;
    private AudioSource _gatherSource, _burstSource;
    private bool _gatherStarted, _burstStarted, _gatherStopped, _timePaused, _listenerWasPaused;
    private float _burstAge, _gatherAtBurst, _lastScale = -1f;

    public float Elapsed { get; private set; }
    public bool BurstTriggered { get; private set; }
    public float BurstAge { get { return BurstTriggered ? _burstAge : -1f; } }
    public bool IsComplete { get; private set; }
    public bool IsDisposed { get; private set; }
    public int BurstStartCount { get; private set; }
    public AudioSource GatherSource { get { return _gatherSource; } }
    public AudioSource BurstSource { get { return _burstSource; } }
    public GameObject Root { get { return _root; } }
    public float GatherVolume { get { return _gatherSource != null ? _gatherSource.volume : 0f; } }
    public float BurstVolume { get { return _burstSource != null ? _burstSource.volume : 0f; } }

    public static SlazeyaStormAudioController TryCreate(Vector3 position, Func<float> readEffectVolume = null, string audioDirectory = null)
    {
        try { return new SlazeyaStormAudioController(position, audioDirectory, readEffectVolume); }
        catch (Exception ex)
        {
            Debug.LogWarning("SlazeyaStorm audio unavailable; visual continues: " + ex.Message);
            return null;
        }
    }

    public SlazeyaStormAudioController(Vector3 position, string audioDirectory = null, Func<float> readEffectVolume = null)
    {
        _readEffectVolume = readEffectVolume;
        try
        {
            string directory = audioDirectory ?? RuntimeAudioDirectory();
            AudioClip gather = TryLoadClip(Path.Combine(directory, "gather_loop.wav"), GatherFrames);
            AudioClip burst = TryLoadClip(Path.Combine(directory, "burst_tail.wav"), BurstFrames);
            if (gather == null && burst == null) return;
            _root = new GameObject("SlazeyaStormAudio");
            _root.transform.position = position;
            _gatherSource = CreateSource(gather, true);
            _burstSource = CreateSource(burst, false);
            UpdatePlayback();
        }
        catch
        {
            ReleaseSources();
            throw;
        }
    }

    private static string RuntimeAudioDirectory()
    {
        string assemblyDirectory = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
        string modRoot = Directory.GetParent(assemblyDirectory).FullName;
        return Path.Combine(modRoot, "Resource", "CustomAudio", "SlazeyaStorm");
    }

    private AudioSource CreateSource(AudioClip clip, bool loop)
    {
        AudioSource source = _root.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.ignoreListenerPause = false;
        source.volume = 0f;
        source.clip = clip;
        return source;
    }

    public bool TriggerBurst()
    {
        if (IsDisposed || IsComplete || BurstTriggered) return false;
        BurstTriggered = true;
        _burstAge = 0f;
        _gatherAtBurst = GatherEnvelope(Elapsed);
        SafelyUpdatePlayback(); // Same callback frame; mute does not reset or defer the clock.
        return true;
    }

    public void Advance(float scaledDeltaTime)
    {
        if (IsDisposed || IsComplete) return;
        float scale = CurrentScale();
        if (scale > 0f && scaledDeltaTime > 0f && !float.IsNaN(scaledDeltaTime) && !float.IsInfinity(scaledDeltaTime))
        {
            Elapsed += scaledDeltaTime;
            if (BurstTriggered) _burstAge += scaledDeltaTime;
        }
        if (BurstTriggered && _burstAge >= SlazeyaStormVisualController.TailDuration)
        {
            IsComplete = true;
            ReleaseSources();
            return;
        }
        // Runs even at delta=0: volume sliders/mute must respond during a pause.
        SafelyUpdatePlayback();
    }

    public static float GatherEnvelope(float elapsed)
    {
        float t = Mathf.Clamp01(elapsed / SlazeyaStormVisualController.GatherDuration);
        return GatherGain * t * t * (3f - 2f * t);
    }

    private void SafelyUpdatePlayback()
    {
        try { UpdatePlayback(); }
        catch (Exception ex)
        {
            Debug.LogWarning("SlazeyaStorm audio stopped safely: " + ex.Message);
            Dispose();
        }
    }

    private float EffectVolume()
    {
        float value;
        try { value = _readEffectVolume != null ? _readEffectVolume() : 1f; }
        catch { value = 1f; } // A standalone Unity fixture need not have game options.
        return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Clamp01(value);
    }

    private static float CurrentScale()
    {
        float scale = Time.timeScale;
        return scale > 0f && !float.IsNaN(scale) && !float.IsInfinity(scale) ? scale : 0f;
    }

    private void UpdatePlayback()
    {
        if (_root == null) return;
        float option = EffectVolume(); // Already master times effects; multiply exactly once.
        float gather = BurstTriggered ? _gatherAtBurst * (1f - Mathf.Clamp01(_burstAge / GatherRelease)) : GatherEnvelope(Elapsed);
        _gatherSource.volume = gather * option;
        _burstSource.volume = BurstTriggered ? BurstGain * option : 0f;
        if (BurstTriggered && _burstAge >= GatherRelease && !_gatherStopped)
        {
            _gatherStopped = true;
            _gatherSource.Stop();
        }
        float scale = CurrentScale();
        float pitch = Mathf.Clamp(scale, 0f, 3f); // Keep positive slow motion exact; drift correction covers Unity's upper pitch limit.
        _gatherSource.pitch = pitch;
        _burstSource.pitch = pitch;
        if (scale <= 0f)
        {
            if (!_timePaused)
            {
                if (_gatherStarted && !_gatherStopped) _gatherSource.Pause();
                if (_burstStarted) _burstSource.Pause();
            }
            _timePaused = true;
            return;
        }
        bool resync = _timePaused || _listenerWasPaused || scale != _lastScale;
        _lastScale = scale;
        if (_timePaused)
        {
            if (_gatherStarted && !_gatherStopped) _gatherSource.UnPause();
            if (_burstStarted) _burstSource.UnPause();
            _timePaused = false;
        }
        if (AudioListener.pause)
        {
            _listenerWasPaused = true;
            return; // Native listener pause remains authoritative; no global audio setting is changed.
        }
        _listenerWasPaused = false;
        if (!_gatherStopped && _gatherSource.clip != null)
        {
            bool first = !_gatherStarted;
            if (first) { _gatherSource.Play(); _gatherStarted = true; }
            Synchronize(_gatherSource, Elapsed, true, first || resync);
        }
        if (BurstTriggered && _burstSource.clip != null)
        {
            bool first = !_burstStarted;
            if (first) { _burstSource.Play(); _burstStarted = true; BurstStartCount++; }
            Synchronize(_burstSource, _burstAge, false, first || resync);
        }
    }

    private static void Synchronize(AudioSource source, float time, bool loop, bool force)
    {
        int frames = source.clip.samples;
        double elapsedFrames = Math.Floor((double)time * SampleRate);
        int wanted = loop ? (int)(elapsedFrames % frames) : (int)Math.Min(frames - 1, elapsedFrames);
        int drift = Math.Abs(source.timeSamples - wanted);
        if (loop) drift = Math.Min(drift, frames - drift);
        if (force || drift > SampleRate / 10) source.timeSamples = wanted;
    }

    private void ReleaseSources()
    {
        if (_gatherSource != null) { _gatherSource.volume = 0f; _gatherSource.Stop(); }
        if (_burstSource != null) { _burstSource.volume = 0f; _burstSource.Stop(); }
        if (_root != null)
        {
            _root.SetActive(false);
            UnityEngine.Object.Destroy(_root);
        }
        _gatherSource = null;
        _burstSource = null;
        _root = null;
    }

    public void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        IsComplete = true;
        ReleaseSources(); // Successful cached clips are shared and intentionally retained.
    }

    private static AudioClip TryLoadClip(string path, int expectedFrames)
    {
        AudioClip clip = null;
        try
        {
            string key = Path.GetFullPath(path);
            if (Clips.TryGetValue(key, out clip) && clip != null) return clip;
            float[] samples = ReadPcm16Wave(path, expectedFrames);
            clip = AudioClip.Create(Path.GetFileNameWithoutExtension(path), expectedFrames, 2, SampleRate, false);
            if (clip == null || !clip.SetData(samples, 0)) throw new InvalidDataException("AudioClip could not accept PCM samples");
            Clips[key] = clip;
            return clip;
        }
        catch (Exception ex)
        {
            if (clip != null) UnityEngine.Object.Destroy(clip);
            Debug.LogWarning("SlazeyaStorm optional WAV unavailable: " + path + " (" + ex.Message + ")");
            return null;
        }
    }

    /// <summary>Strict reusable parser for this task's exact PCM16 stereo 44100-Hz RIFF files.</summary>
    public static float[] ReadPcm16Wave(string path, int expectedFrames)
    {
        if (expectedFrames != GatherFrames && expectedFrames != BurstFrames) throw new ArgumentOutOfRangeException("expectedFrames");
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var reader = new BinaryReader(stream))
        {
            if (stream.Length < 44 || stream.Length > MaxWaveBytes) throw new InvalidDataException("WAV file size outside fixed task bounds");
            if (reader.ReadUInt32() != 0x46464952) throw new InvalidDataException("Expected RIFF");
            if ((long)reader.ReadUInt32() + 8 != stream.Length) throw new InvalidDataException("RIFF length mismatch");
            if (reader.ReadUInt32() != 0x45564157) throw new InvalidDataException("Expected WAVE");
            bool haveFormat = false;
            long dataOffset = -1;
            uint dataLength = 0;
            while (stream.Position < stream.Length)
            {
                if (stream.Length - stream.Position < 8) throw new InvalidDataException("Truncated WAV chunk header");
                uint id = reader.ReadUInt32(), size = reader.ReadUInt32();
                long start = stream.Position, next = start + size + (size & 1u);
                if (next > stream.Length) throw new InvalidDataException("WAV chunk exceeds file");
                if (id == 0x20746D66) // fmt
                {
                    if (haveFormat || (size != 16 && size != 18)) throw new InvalidDataException("Duplicate or unsupported PCM format chunk");
                    ushort format = reader.ReadUInt16(), channels = reader.ReadUInt16();
                    uint rate = reader.ReadUInt32(), byteRate = reader.ReadUInt32();
                    ushort blockAlign = reader.ReadUInt16(), bits = reader.ReadUInt16();
                    if (format != 1 || channels != 2 || rate != 44100 || byteRate != 176400 || blockAlign != 4 || bits != 16)
                        throw new InvalidDataException("Expected PCM16 stereo 44100 Hz with consistent byte rate/alignment");
                    if (size == 18 && reader.ReadUInt16() != 0) throw new InvalidDataException("Unexpected PCM extension");
                    haveFormat = true;
                }
                else if (id == 0x61746164) // data
                {
                    if (dataOffset >= 0) throw new InvalidDataException("Duplicate WAV data chunk");
                    dataOffset = start;
                    dataLength = size;
                }
                stream.Position = next;
            }
            if (!haveFormat || dataOffset < 0 || dataLength != expectedFrames * 4) throw new InvalidDataException("Missing PCM data or incorrect frozen duration");
            stream.Position = dataOffset;
            var result = new float[expectedFrames * 2];
            for (int i = 0; i < result.Length; i++) result[i] = reader.ReadInt16() / 32768f;
            return result;
        }
    }
}
