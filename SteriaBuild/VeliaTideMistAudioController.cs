using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

/// <summary>Optional per-card cast/hit companion; follows the host's scaled combat clock.</summary>
public sealed class VeliaTideMistAudioController : IDisposable
{
    public const int SampleRate = 44100;
    public const int CastFrames = 14112;
    public const int HitFrames = 31752;
    public const int MaxWaveBytes = 512 * 1024;
    public const float CastDuration = 0.32f;
    public const float HitDuration = 0.72f;
    public const float CastGain = 0.55f;
    public const float HitGain = 0.75f;
    public const float CastRelease = 0.04f;

    private static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
    private readonly Func<float> _readEffectVolume;
    private GameObject _root;
    private AudioSource _castSource, _hitSource;
    private int _ordinal = -1, _hitOrdinal = -1;
    private bool _castStarted, _hitStarted, _castStopped, _hitStopped, _castReleasing;
    private bool _timePaused, _listenerWasPaused;
    private float _hitAge, _castReleaseStart, _lastScale = -1f;

    public float Elapsed { get; private set; }
    public float CastAge { get { return Elapsed; } }
    public int DiceOrdinal { get { return _ordinal; } }
    public int LastHitOrdinal { get { return _hitOrdinal; } }
    public bool HitTriggered { get { return _ordinal >= 0 && _hitOrdinal == _ordinal; } }
    public float HitAge { get { return _hitOrdinal >= 0 ? _hitAge : -1f; } }
    public bool CastFinished { get { return _castStopped || IsComplete; } }
    public bool HitFinished { get { return (_hitOrdinal >= 0 && _hitStopped) || IsComplete; } }
    public bool IsComplete { get; private set; }
    public bool IsDisposed { get; private set; }
    public int CastStartCount { get; private set; }
    public int HitStartCount { get; private set; }
    public int HitTriggerCount { get; private set; }
    public AudioSource CastSource { get { return _castSource; } }
    public AudioSource HitSource { get { return _hitSource; } }
    public GameObject Root { get { return _root; } }
    public float CastVolume { get { return _castSource != null ? _castSource.volume : 0f; } }
    public float HitVolume { get { return _hitSource != null ? _hitSource.volume : 0f; } }

    public static VeliaTideMistAudioController TryCreate(Vector3 position, Func<float> readEffectVolume = null, string audioDirectory = null)
    {
        try { return new VeliaTideMistAudioController(position, audioDirectory, readEffectVolume); }
        catch (Exception ex)
        {
            Debug.LogWarning("VeliaTideMist audio unavailable; visual continues: " + ex.Message);
            return null;
        }
    }

    public VeliaTideMistAudioController(Vector3 position, string audioDirectory = null, Func<float> readEffectVolume = null)
    {
        _readEffectVolume = readEffectVolume;
        try
        {
            string directory = audioDirectory ?? RuntimeAudioDirectory();
            AudioClip cast = TryLoadClip(Path.Combine(directory, "cast.wav"), CastFrames);
            AudioClip hit = TryLoadClip(Path.Combine(directory, "hit.wav"), HitFrames);
            if (cast == null && hit == null) return;
            _root = new GameObject("VeliaTideMistAudio");
            _root.transform.position = position;
            _castSource = CreateSource(cast);
            _hitSource = CreateSource(hit);
            UpdatePlayback(); // One cast for this whole card, not one per BeginDice.
        }
        catch { ReleaseSources(); throw; }
    }

    private static string RuntimeAudioDirectory()
    {
        string assemblyDirectory = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
        return Path.Combine(Directory.GetParent(assemblyDirectory).FullName, "Resource", "CustomAudio", "VeliaTideMist");
    }

    private AudioSource CreateSource(AudioClip clip)
    {
        AudioSource source = _root.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.ignoreListenerPause = false;
        source.volume = 0f;
        source.clip = clip;
        return source;
    }

    public bool BeginDice(int ordinal)
    {
        if (IsDisposed || IsComplete || ordinal < 0 || ordinal <= _ordinal) return false;
        _ordinal = ordinal;
        return true; // Do not reset the whole-card cast, clock or still-ending previous cue.
    }

    public bool TriggerHit(int ordinal)
    {
        if (IsDisposed || IsComplete || ordinal < 0 || ordinal != _ordinal || ordinal <= _hitOrdinal) return false;
        _hitOrdinal = ordinal;
        _hitAge = 0f;
        _hitStarted = _hitStopped = false;
        HitTriggerCount++;
        if (_hitSource != null) _hitSource.Stop(); // Reuse one source; never layer old/new copies.
        if (!_castReleasing)
        {
            _castReleasing = true;
            _castReleaseStart = Elapsed;
        }
        SafelyUpdatePlayback(); // Same callback frame, including empty success lists.
        return true;
    }

    public void Advance(float scaledDeltaTime)
    {
        if (IsDisposed || IsComplete) return;
        if (CurrentScale() > 0f && scaledDeltaTime > 0f && !float.IsNaN(scaledDeltaTime) && !float.IsInfinity(scaledDeltaTime))
        {
            Elapsed += scaledDeltaTime;
            if (_hitOrdinal >= 0) _hitAge += scaledDeltaTime;
        }
        // Expire cues even when missing/deferred; never replay expired sound on unmute/resume.
        if (!_castStopped && (Elapsed >= CastDuration || (_castReleasing && Elapsed - _castReleaseStart >= CastRelease)))
        {
            _castStopped = true;
            if (_castSource != null) _castSource.Stop();
        }
        if (!_hitStopped && _hitOrdinal >= 0 && _hitAge >= HitDuration)
        {
            _hitStopped = true;
            if (_hitSource != null) _hitSource.Stop();
        }
        SafelyUpdatePlayback(); // Advance(0) still refreshes options and pause/resume state.
    }

    private void SafelyUpdatePlayback()
    {
        try { UpdatePlayback(); }
        catch (Exception ex)
        {
            Debug.LogWarning("VeliaTideMist audio stopped safely: " + ex.Message);
            Dispose();
        }
    }

    private float EffectVolume()
    {
        float value;
        try { value = _readEffectVolume != null ? _readEffectVolume() : 1f; }
        catch { value = 1f; }
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
        float option = EffectVolume(); // The game callback already returns master * effects.
        float release = _castReleasing ? 1f - Mathf.Clamp01((Elapsed - _castReleaseStart) / CastRelease) : 1f;
        _castSource.volume = !_castStopped ? CastGain * release * option : 0f;
        _hitSource.volume = _hitOrdinal >= 0 && !_hitStopped ? HitGain * option : 0f;
        float scale = CurrentScale();
        float pitch = Mathf.Clamp(scale, 0f, 3f);
        _castSource.pitch = _hitSource.pitch = pitch;
        if (scale <= 0f)
        {
            if (!_timePaused)
            {
                if (_castStarted && !_castStopped) _castSource.Pause();
                if (_hitStarted && !_hitStopped) _hitSource.Pause();
            }
            _timePaused = true;
            return;
        }
        bool resync = _timePaused || _listenerWasPaused || scale != _lastScale;
        _lastScale = scale;
        if (_timePaused)
        {
            if (_castStarted && !_castStopped) _castSource.UnPause();
            if (_hitStarted && !_hitStopped) _hitSource.UnPause();
            _timePaused = false;
        }
        if (AudioListener.pause)
        {
            _listenerWasPaused = true;
            return;
        }
        _listenerWasPaused = false;
        if (!_castStopped && _castSource.clip != null)
        {
            bool first = !_castStarted;
            if (first) { _castSource.Play(); _castStarted = true; CastStartCount++; }
            Synchronize(_castSource, Elapsed, first || resync);
        }
        if (_hitOrdinal >= 0 && !_hitStopped && _hitSource.clip != null)
        {
            bool first = !_hitStarted;
            if (first) { _hitSource.Play(); _hitStarted = true; HitStartCount++; }
            Synchronize(_hitSource, _hitAge, first || resync);
        }
    }

    private static void Synchronize(AudioSource source, float age, bool force)
    {
        int wanted = (int)Math.Min(source.clip.samples - 1, Math.Floor((double)age * SampleRate));
        if (force || Math.Abs(source.timeSamples - wanted) > SampleRate / 10) source.timeSamples = wanted;
    }

    public void Finish()
    {
        if (IsComplete) return;
        IsComplete = true;
        ReleaseSources(); // No extra audio tail may hold the manager or survive this card.
    }
    public void Cancel() { Dispose(); }
    public void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        IsComplete = true;
        ReleaseSources();
    }

    private void ReleaseSources()
    {
        if (_castSource != null) { _castSource.volume = 0f; _castSource.Stop(); }
        if (_hitSource != null) { _hitSource.volume = 0f; _hitSource.Stop(); }
        if (_root != null) { _root.SetActive(false); UnityEngine.Object.Destroy(_root); }
        _castSource = null;
        _hitSource = null;
        _root = null;
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
            Debug.LogWarning("VeliaTideMist optional WAV unavailable: " + path + " (" + ex.Message + ")");
            return null;
        }
    }

    public static float[] ReadPcm16Wave(string path, int expectedFrames)
    {
        if (expectedFrames != CastFrames && expectedFrames != HitFrames) throw new ArgumentOutOfRangeException("expectedFrames");
        return SkillPcmWave.ReadStereo44100(path, expectedFrames);
    }
}
