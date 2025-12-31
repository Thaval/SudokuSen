namespace SudokuSen.Services;

/// <summary>
/// Autoload: Manages all game audio (SFX and Music)
/// Generates procedural sounds if no audio files are provided.
/// </summary>
public partial class AudioService : Node
{
    public static AudioService? Instance { get; private set; }

    [Signal]
    public delegate void MusicVolumeChangedEventHandler(float volume);

    [Signal]
    public delegate void SfxVolumeChangedEventHandler(float volume);

    // SFX player pool - multiple players allow overlapping sounds and prevent interruption
    private const int SFX_POOL_SIZE = 4;
    private readonly List<AudioStreamPlayer> _sfxPool = new();
    private int _sfxPoolIndex = 0;

    // Music player (single, looping)
    private AudioStreamPlayer _musicPlayer = null!;

    // Silent "keep-alive" player - keeps audio driver active so short SFX don't get swallowed
    private AudioStreamPlayer _keepAlivePlayer = null!;

    // Cached service reference
    private SaveService _saveService = null!;

    private bool _readyForPlayback = false;
    private ulong _sfxAttemptId = 0;
    private ulong _musicDecisionId = 0;

    // Generated SFX streams
    private AudioStream _clickSound = null!;
    private AudioStream _cellSelectSound = null!;
    private AudioStream _numberPlaceSound = null!;
    private AudioStream _numberRemoveSound = null!;
    private AudioStream _notePlaceSound = null!;
    private AudioStream _noteRemoveSound = null!;
    private AudioStream _errorSound = null!;
    private AudioStream _successSound = null!;
    private AudioStream _winSound = null!;

    // Music streams - multiple tracks available
    private readonly List<AudioStream?> _musicTracks = new();
    private int _menuMusicTrack = 1;  // 0 = off, 1-N = track index
    private int _gameMusicTrack = 2;
    private bool _inGame = false;

    // State
    private bool _soundEnabled = true;
    private bool _musicEnabled = true;
    private float _sfxVolume = 0.8f;
    private float _musicVolume = 0.5f;

    // Track names for UI
    public static readonly string[] MusicTrackNames = { "Aus", "Maybe Tomorrow", "Test Wobble", "ASDF Mix" };

    private const int SAMPLE_RATE = 44100;

    public bool SoundEnabled
    {
        get => _soundEnabled;
        set
        {
            _soundEnabled = value;
        }
    }

    public bool MusicEnabled
    {
        get => _musicEnabled;
        set
        {
            _musicEnabled = value;
            if (!_musicEnabled)
                _musicPlayer?.Stop();
            else
                SetInGame(_inGame);
        }
    }

    public float SfxVolume
    {
        get => _sfxVolume;
        set
        {
            _sfxVolume = Mathf.Clamp(value, 0f, 1f);
            EmitSignal(SignalName.SfxVolumeChanged, _sfxVolume);
        }
    }

    public int MenuMusicTrack
    {
        get => _menuMusicTrack;
        set
        {
            _menuMusicTrack = value;
            if (!_inGame) SetInGame(false);
        }
    }

    public int GameMusicTrack
    {
        get => _gameMusicTrack;
        set
        {
            _gameMusicTrack = value;
            if (_inGame) SetInGame(true);
        }
    }

    public float MusicVolume
    {
        get => _musicVolume;
        set
        {
            _musicVolume = Mathf.Clamp(value, 0f, 1f);
            if (_musicPlayer != null)
                _musicPlayer.VolumeDb = Mathf.LinearToDb(_musicVolume);
            EmitSignal(SignalName.MusicVolumeChanged, _musicVolume);
        }
    }

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (_saveService != null)
            _saveService.SettingsChanged -= OnSettingsChanged;

        if (Instance == this)
            Instance = null;
    }

    public override void _Ready()
    {
        // Cache service reference
        _saveService = GetNode<SaveService>("/root/SaveService");
        _saveService.EnsureLoaded();

        // Create SFX player pool - multiple players prevent sounds from being cut off
        string sfxBus = AudioServer.GetBusIndex("SFX") >= 0 ? "SFX" : "Master";
        for (int i = 0; i < SFX_POOL_SIZE; i++)
        {
            var player = new AudioStreamPlayer();
            player.Bus = sfxBus;
            player.Name = $"SfxPlayer_{i}";
            AddChild(player);
            _sfxPool.Add(player);

            int playerIndex = i;
            player.Finished += () =>
            {
                GD.Print($"[Audio][SFX] Finished | player={playerIndex}, bus={player.Bus}");
            };
        }

        // Create music player
        _musicPlayer = new AudioStreamPlayer();
        _musicPlayer.Bus = AudioServer.GetBusIndex("Music") >= 0 ? "Music" : "Master";
        AddChild(_musicPlayer);

        _musicPlayer.Finished += () =>
        {
            GD.Print($"[Audio][Music] Finished | bus={_musicPlayer.Bus}, playing={_musicPlayer.Playing}");
        };

        // Create keep-alive player - plays silent loop to keep audio driver active
        // This prevents the audio driver from going idle and swallowing short SFX
        _keepAlivePlayer = new AudioStreamPlayer();
        _keepAlivePlayer.Bus = sfxBus;
        _keepAlivePlayer.Name = "KeepAlivePlayer";
        _keepAlivePlayer.VolumeDb = -80f; // Inaudible
        AddChild(_keepAlivePlayer);

        LoadAssetsOrGenerateFallback();

        // Start the keep-alive silent loop
        StartKeepAlive();

        _readyForPlayback = true;

        // Log detailed audio bus configuration
        LogAudioBusConfiguration();

        string sfxBusName = _sfxPool.Count > 0 ? _sfxPool[0].Bus : "none";
        int sfxBusIdx = AudioServer.GetBusIndex(sfxBusName);
        GD.Print($"[Audio] Ready | sfxBus={sfxBusName}(idx={sfxBusIdx}), sfxPoolSize={_sfxPool.Count}, musicBus={_musicPlayer.Bus}(idx={AudioServer.GetBusIndex(_musicPlayer.Bus)}), keepAlive=active");

        // Load settings (use cached reference)
        _saveService.SettingsChanged += OnSettingsChanged;
        ApplySettings(_saveService.Settings);
    }

    private void LogAudioBusConfiguration()
    {
        int busCount = AudioServer.BusCount;
        GD.Print($"[Audio] Bus configuration: {busCount} buses");
        for (int i = 0; i < busCount; i++)
        {
            string busName = AudioServer.GetBusName(i);
            float volDb = AudioServer.GetBusVolumeDb(i);
            bool muted = AudioServer.IsBusMute(i);
            bool solo = AudioServer.IsBusSolo(i);
            string send = AudioServer.GetBusSend(i);
            GD.Print($"[Audio]   Bus[{i}] '{busName}': volDb={volDb:0.00}, muted={muted}, solo={solo}, send='{send}'");
        }
    }

    private void OnSettingsChanged()
    {
        ApplySettings(_saveService.Settings);
    }

    private void LoadAssetsOrGenerateFallback()
    {
        _clickSound = TryLoadStream("res://Audio/click.wav") ?? GenerateClick(800, 0.05f);
        _cellSelectSound = TryLoadStream("res://Audio/cell_select.wav") ?? GenerateClick(600, 0.04f);
        _numberPlaceSound = TryLoadStream("res://Audio/number_place.wav") ?? GenerateTone(523.25f, 0.1f, true); // C5
        _numberRemoveSound = TryLoadStream("res://Audio/number_remove.wav") ?? GenerateTone(392.0f, 0.08f, false); // G4 descending
        _notePlaceSound = TryLoadStream("res://Audio/note_place.wav") ?? GenerateClick(1000, 0.03f);
        _noteRemoveSound = TryLoadStream("res://Audio/note_remove.wav") ?? GenerateClick(500, 0.03f);
        _errorSound = TryLoadStream("res://Audio/error.wav") ?? GenerateError();
        _successSound = TryLoadStream("res://Audio/success.wav") ?? GenerateSuccess();
        _winSound = TryLoadStream("res://Audio/win.wav") ?? GenerateWinFanfare();

        // Load music tracks - index 0 = null (off), then actual tracks
        _musicTracks.Clear();
        _musicTracks.Add(null); // Index 0 = Aus/Off
        _musicTracks.Add(TryLoadStream("res://Audio/maybetommorrowisdifferentmixoriginalkey.ogg")); // Maybe Tomorrow
        _musicTracks.Add(TryLoadStream("res://Audio/test_woble_mix.ogg")); // Test Wobble
        _musicTracks.Add(TryLoadStream("res://Audio/asdf_5_128_mix.ogg")); // ASDF Mix

        GD.Print($"[Audio] Loaded SFX: click={_clickSound != null}, cell_select={_cellSelectSound != null}, number_place={_numberPlaceSound != null}");
        GD.Print($"[Audio] Loaded Music tracks: {_musicTracks.Count - 1} tracks available");
    }

    private static int MigrateLegacyTrackIndex(int legacy)
    {
        // Old indices:
        // 0=Off, 1=Ambient Pad, 2=Maybe Tomorrow, 3=Test Wobble, 4=ASDF Mix
        // New indices:
        // 0=Off, 1=Maybe Tomorrow, 2=Test Wobble, 3=ASDF Mix
        return legacy switch
        {
            0 => 0,
            1 => 0, // Ambient Pad removed
            2 => 1,
            3 => 2,
            4 => 3,
            _ => 0
        };
    }

    private int NormalizeTrackIndex(int trackIndex)
    {
        if (trackIndex <= 0) return 0;
        if (_musicTracks.Count <= 1) return 0;

        // Clamp to valid range. If settings contain an out-of-range index, keep music enabled but choose the
        // closest valid track index instead of turning music off.
        return Mathf.Clamp(trackIndex, 0, _musicTracks.Count - 1);
    }

    private static AudioStream? TryLoadStream(string path)
    {
        if (!ResourceLoader.Exists(path)) return null;
        try
        {
            return GD.Load<AudioStream>(path);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[Audio] Failed to load {path}: {e.Message}");
            return null;
        }
    }

    private void GenerateSounds()
    {
        // Generate simple but pleasant procedural sounds
        _clickSound = GenerateClick(800, 0.05f);
        _cellSelectSound = GenerateClick(600, 0.04f);
        _numberPlaceSound = GenerateTone(523.25f, 0.1f, true); // C5
        _numberRemoveSound = GenerateTone(392.0f, 0.08f, false); // G4 descending
        _notePlaceSound = GenerateClick(1000, 0.03f);
        _noteRemoveSound = GenerateClick(500, 0.03f);
        _errorSound = GenerateError();
        _successSound = GenerateSuccess();
        _winSound = GenerateWinFanfare();
    }

    /// <summary>
    /// Generates a short click/tick sound
    /// </summary>
    private AudioStreamWav GenerateClick(float frequency, float duration)
    {
        int samples = (int)(SAMPLE_RATE * duration);
        var data = new byte[samples * 2]; // 16-bit mono

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float envelope = 1.0f - (float)i / samples; // Linear decay
            envelope *= envelope; // Exponential decay

            // Simple sine wave with harmonics for a click
            float sample = Mathf.Sin(2 * Mathf.Pi * frequency * t) * 0.6f;
            sample += Mathf.Sin(2 * Mathf.Pi * frequency * 2 * t) * 0.3f;
            sample += Mathf.Sin(2 * Mathf.Pi * frequency * 3 * t) * 0.1f;
            sample *= envelope * 0.5f;

            short value = (short)(sample * 32767);
            data[i * 2] = (byte)(value & 0xFF);
            data[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }

        return CreateWavStream(data);
    }

    /// <summary>
    /// Generates a pleasant tone (for number placement)
    /// </summary>
    private AudioStreamWav GenerateTone(float frequency, float duration, bool ascending)
    {
        int samples = (int)(SAMPLE_RATE * duration);
        var data = new byte[samples * 2];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = (float)i / samples;

            // Pitch bend for ascending/descending feel
            float freqMod = ascending ? 1.0f + progress * 0.2f : 1.0f - progress * 0.1f;
            float currentFreq = frequency * freqMod;

            // ADSR envelope
            float envelope;
            if (progress < 0.1f)
                envelope = progress / 0.1f; // Attack
            else if (progress < 0.3f)
                envelope = 1.0f - (progress - 0.1f) * 0.3f; // Decay
            else
                envelope = 0.7f * (1.0f - (progress - 0.3f) / 0.7f); // Release

            float sample = Mathf.Sin(2 * Mathf.Pi * currentFreq * t) * 0.5f;
            sample += Mathf.Sin(2 * Mathf.Pi * currentFreq * 2 * t) * 0.2f;
            sample *= envelope * 0.4f;

            short value = (short)(sample * 32767);
            data[i * 2] = (byte)(value & 0xFF);
            data[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }

        return CreateWavStream(data);
    }

    /// <summary>
    /// Generates an error/buzz sound
    /// </summary>
    private AudioStreamWav GenerateError()
    {
        float duration = 0.2f;
        int samples = (int)(SAMPLE_RATE * duration);
        var data = new byte[samples * 2];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = (float)i / samples;
            float envelope = 1.0f - progress;

            // Dissonant frequencies for error feel
            float sample = Mathf.Sin(2 * Mathf.Pi * 200 * t) * 0.4f;
            sample += Mathf.Sin(2 * Mathf.Pi * 250 * t) * 0.3f;
            sample += Mathf.Sin(2 * Mathf.Pi * 180 * t) * 0.2f;
            sample *= envelope * 0.35f;

            short value = (short)(sample * 32767);
            data[i * 2] = (byte)(value & 0xFF);
            data[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }

        return CreateWavStream(data);
    }

    /// <summary>
    /// Generates a pleasant success chime
    /// </summary>
    private AudioStreamWav GenerateSuccess()
    {
        float duration = 0.25f;
        int samples = (int)(SAMPLE_RATE * duration);
        var data = new byte[samples * 2];

        // Two-note ascending chime: C5 -> E5
        float freq1 = 523.25f; // C5
        float freq2 = 659.25f; // E5

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = (float)i / samples;

            // Switch notes halfway
            float freq = progress < 0.4f ? freq1 : freq2;
            float noteProgress = progress < 0.4f ? progress / 0.4f : (progress - 0.4f) / 0.6f;

            float envelope;
            if (noteProgress < 0.1f)
                envelope = noteProgress / 0.1f;
            else
                envelope = 1.0f - (noteProgress - 0.1f) / 0.9f;

            float sample = Mathf.Sin(2 * Mathf.Pi * freq * t) * 0.5f;
            sample += Mathf.Sin(2 * Mathf.Pi * freq * 2 * t) * 0.2f;
            sample *= envelope * 0.35f;

            short value = (short)(sample * 32767);
            data[i * 2] = (byte)(value & 0xFF);
            data[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }

        return CreateWavStream(data);
    }

    /// <summary>
    /// Generates a win fanfare (C-E-G arpeggio)
    /// </summary>
    private AudioStreamWav GenerateWinFanfare()
    {
        float duration = 0.6f;
        int samples = (int)(SAMPLE_RATE * duration);
        var data = new byte[samples * 2];

        // C major arpeggio: C5 -> E5 -> G5
        float[] freqs = { 523.25f, 659.25f, 783.99f }; // C5, E5, G5

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float progress = (float)i / samples;

            // Determine which note we're on
            int noteIdx;
            float noteStart;
            if (progress < 0.25f) { noteIdx = 0; noteStart = 0; }
            else if (progress < 0.5f) { noteIdx = 1; noteStart = 0.25f; }
            else { noteIdx = 2; noteStart = 0.5f; }

            float freq = freqs[noteIdx];
            float noteProgress = (progress - noteStart) / (noteIdx == 2 ? 0.5f : 0.25f);

            float envelope;
            if (noteProgress < 0.05f)
                envelope = noteProgress / 0.05f;
            else
                envelope = 1.0f - (noteProgress - 0.05f) / 0.95f;

            // Last note holds longer
            if (noteIdx == 2)
                envelope = Mathf.Max(envelope, 0.3f * (1.0f - noteProgress));

            float sample = Mathf.Sin(2 * Mathf.Pi * freq * t) * 0.5f;
            sample += Mathf.Sin(2 * Mathf.Pi * freq * 2 * t) * 0.25f;
            sample += Mathf.Sin(2 * Mathf.Pi * freq * 0.5f * t) * 0.15f; // Sub-octave
            sample *= envelope * 0.35f;

            short value = (short)(sample * 32767);
            data[i * 2] = (byte)(value & 0xFF);
            data[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }

        return CreateWavStream(data);
    }

    private AudioStreamWav CreateWavStream(byte[] data)
    {
        var stream = new AudioStreamWav();
        stream.Format = AudioStreamWav.FormatEnum.Format16Bits;
        stream.MixRate = SAMPLE_RATE;
        stream.Stereo = false;
        stream.Data = data;
        return stream;
    }

    public void ApplySettings(SettingsData settings)
    {
        _soundEnabled = settings.SoundEnabled;
        _musicEnabled = settings.MusicEnabled;
        _sfxVolume = settings.Volume / 100f;
        _musicVolume = settings.MusicVolume / 100f;

        // Normalize track indices to avoid invalid values causing unexpected behavior.
        // NOTE: We intentionally do not attempt to auto-migrate older index schemes here because without an
        // explicit settings version it is ambiguous and can break valid current selections.
        _menuMusicTrack = NormalizeTrackIndex(settings.MenuMusicTrack);
        _gameMusicTrack = NormalizeTrackIndex(settings.GameMusicTrack);

        // Persist only if the stored values are invalid/out-of-range.
        if (settings.MenuMusicTrack != _menuMusicTrack || settings.GameMusicTrack != _gameMusicTrack)
        {
            GD.Print($"[Audio] Normalized music track indices: menu {settings.MenuMusicTrack}->{_menuMusicTrack}, game {settings.GameMusicTrack}->{_gameMusicTrack}");
            settings.MenuMusicTrack = _menuMusicTrack;
            settings.GameMusicTrack = _gameMusicTrack;
            try
            {
                _saveService.SaveSettings();
            }
            catch (Exception e)
            {
                GD.PrintErr($"[Audio] Failed to persist normalized music track settings: {e.Message}");
            }
        }

        GD.Print($"[Audio] ApplySettings: sfxEnabled={_soundEnabled}, sfxVol={_sfxVolume:0.00}, musicEnabled={_musicEnabled}, musicVol={_musicVolume:0.00}, menuTrack={_menuMusicTrack}, gameTrack={_gameMusicTrack}");

        if (_musicPlayer != null)
            _musicPlayer.VolumeDb = Mathf.LinearToDb(_musicVolume);

        if (!_musicEnabled)
        {
            _musicPlayer?.Stop();
            GD.Print($"[Audio][Music] Stop (disabled) | playing={_musicPlayer?.Playing}");
        }
        else
        {
            // Resume music in the current context
            SetInGame(_inGame);
        }
    }

    // --- Public SFX Methods ---

    public void PlayClick()
    {
        PlaySfx("click", _clickSound);
    }

    public void PlayCellSelect()
    {
        PlaySfx("cell_select", _cellSelectSound);
    }

    public void PlayNumberPlace()
    {
        PlaySfx("number_place", _numberPlaceSound);
    }

    public void PlayNumberRemove()
    {
        PlaySfx("number_remove", _numberRemoveSound);
    }

    public void PlayNotePlaceOrRemove(bool isPlacing)
    {
        PlaySfx(isPlacing ? "note_place" : "note_remove", isPlacing ? _notePlaceSound : _noteRemoveSound);
    }

    public void PlayError()
    {
        PlaySfx("error", _errorSound);
    }

    public void PlaySuccess()
    {
        PlaySfx("success", _successSound);
    }

    public void PlayWin()
    {
        PlaySfx("win", _winSound);
    }

    private void PlaySfx(string name, AudioStream? stream)
    {
        _sfxAttemptId++;
        ulong id = _sfxAttemptId;

        if (!_readyForPlayback)
        {
            GD.Print($"[Audio][SFX#{id}] Skip {name}: not ready | enabled={_soundEnabled}");
            return;
        }

        if (!_soundEnabled)
        {
            GD.Print($"[Audio][SFX#{id}] Skip {name}: SFX disabled | vol={_sfxVolume:0.00}");
            return;
        }

        if (stream == null)
        {
            GD.Print($"[Audio][SFX#{id}] Skip {name}: stream is null");
            return;
        }

        if (_sfxPool.Count == 0)
        {
            GD.Print($"[Audio][SFX#{id}] Skip {name}: no players in pool");
            return;
        }

        // Get next player from pool (round-robin)
        var player = _sfxPool[_sfxPoolIndex];
        int playerIdx = _sfxPoolIndex;
        _sfxPoolIndex = (_sfxPoolIndex + 1) % _sfxPool.Count;

        if (!player.IsInsideTree())
        {
            GD.Print($"[Audio][SFX#{id}] Skip {name}: player[{playerIdx}] not inside tree");
            return;
        }

        int busIdx = AudioServer.GetBusIndex(player.Bus);
        if (busIdx < 0)
        {
            GD.Print($"[Audio][SFX#{id}] Skip {name}: invalid bus '{player.Bus}'");
            return;
        }

        // Check bus state - is it muted or has zero volume?
        bool busMuted = AudioServer.IsBusMute(busIdx);
        float busVolDb = AudioServer.GetBusVolumeDb(busIdx);
        bool busEffectivelyMuted = busMuted || busVolDb <= -60f;

        string streamPath = stream.ResourcePath;
        string streamInfo = string.IsNullOrWhiteSpace(streamPath) ? stream.GetClass() : streamPath;
        double streamLength = stream.GetLength();

        // Stop any currently playing sound on this player and play new sound
        if (player.Playing)
        {
            player.Stop();
        }

        player.Stream = stream;
        player.VolumeDb = Mathf.LinearToDb(_sfxVolume);
        player.Play();

        // Detailed diagnostics
        GD.Print($"[Audio][SFX#{id}] Play {name} | player={playerIdx}, playing={player.Playing}, volDb={player.VolumeDb:0.00}, volLin={_sfxVolume:0.00}");
        GD.Print($"[Audio][SFX#{id}]   bus={player.Bus}(idx={busIdx}), busVolDb={busVolDb:0.00}, busMuted={busMuted}, effectivelyMuted={busEffectivelyMuted}");
        GD.Print($"[Audio][SFX#{id}]   stream={streamInfo}, length={streamLength:0.000}s");

        if (busEffectivelyMuted)
        {
            GD.PrintErr($"[Audio][SFX#{id}] WARNING: Bus '{player.Bus}' is effectively muted!");
        }
    }

    // --- Music Control ---

    public void SetInGame(bool inGame)
    {
        _inGame = inGame;

        _musicDecisionId++;
        ulong id = _musicDecisionId;

        if (!_readyForPlayback)
        {
            GD.Print($"[Audio][Music#{id}] Skip SetInGame({inGame}): not ready | enabled={_musicEnabled}");
            return;
        }

        if (!_musicEnabled)
        {
            _musicPlayer?.Stop();
            GD.Print($"[Audio][Music#{id}] Stop (disabled) | inGame={_inGame}");
            return;
        }

        int trackIndex = _inGame ? _gameMusicTrack : _menuMusicTrack;

        // Track 0 = off
        if (trackIndex <= 0 || trackIndex >= _musicTracks.Count)
        {
            _musicPlayer?.Stop();
            GD.Print($"[Audio][Music#{id}] Stop (track off/out of range) | context={(inGame ? "game" : "menu")}, trackIndex={trackIndex}, tracks={_musicTracks.Count}");
            return;
        }

        var desired = _musicTracks[trackIndex];
        if (desired == null)
        {
            _musicPlayer?.Stop();
            GD.Print($"[Audio][Music#{id}] Stop (missing stream) | trackIndex={trackIndex}");
            return;
        }

        if (_musicPlayer.Stream == desired && _musicPlayer.Playing)
        {
            GD.Print($"[Audio][Music#{id}] No-op (already playing) | context={(inGame ? "game" : "menu")}, trackIndex={trackIndex}");
            return;
        }

        _musicPlayer.Stream = desired;
        _musicPlayer.VolumeDb = Mathf.LinearToDb(_musicVolume);

        // Configure looping based on stream type
        if (desired is AudioStreamWav wav)
        {
            wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
            wav.LoopBegin = 0;
            int bytesPerSample = wav.Stereo ? 4 : 2;
            int sampleCount = bytesPerSample > 0 ? (wav.Data.Length / bytesPerSample) : 0;
            if (sampleCount > 0)
                wav.LoopEnd = sampleCount;
        }
        else if (desired is AudioStreamOggVorbis ogg)
        {
            // Ensure OGG files loop endlessly
            ogg.Loop = true;
        }

        _musicPlayer.Play();
        string trackName = trackIndex < MusicTrackNames.Length ? MusicTrackNames[trackIndex] : $"Track {trackIndex}";

        int busIdx = AudioServer.GetBusIndex(_musicPlayer.Bus);
        string streamPath = desired.ResourcePath;
        string streamInfo = string.IsNullOrWhiteSpace(streamPath) ? desired.GetClass() : streamPath;

        GD.Print($"[Audio][Music#{id}] Play | ok={_musicPlayer.Playing}, context={(inGame ? "game" : "menu")}, trackIndex={trackIndex}, name={trackName}, bus={_musicPlayer.Bus}(idx={busIdx}), volDb={_musicPlayer.VolumeDb:0.00}, stream={streamInfo}");
    }

    public void StartMenuMusic()
    {
        GD.Print("[Audio][Music] StartMenuMusic()");
        SetInGame(false);
    }

    public void StartGameMusic()
    {
        GD.Print("[Audio][Music] StartGameMusic()");
        SetInGame(true);
    }

    public void StopAllMusic()
    {
        _musicPlayer?.Stop();
        GD.Print($"[Audio][Music] StopAllMusic() | playing={_musicPlayer?.Playing}");
    }

    /// <summary>
    /// Creates and starts a silent looping audio stream to keep the audio driver active.
    /// This prevents the driver from going idle and swallowing the first few milliseconds
    /// of short sound effects like clicks.
    /// </summary>
    private void StartKeepAlive()
    {
        // Create a 1-second silent loop at 44100 Hz
        const int sampleRate = 44100;
        const int durationSeconds = 1;
        int sampleCount = sampleRate * durationSeconds;
        var data = new byte[sampleCount * 2]; // 16-bit mono, all zeros = silence

        var silentLoop = new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = sampleRate,
            Stereo = false,
            LoopMode = AudioStreamWav.LoopModeEnum.Forward,
            LoopBegin = 0,
            LoopEnd = sampleCount,
            Data = data
        };

        _keepAlivePlayer.Stream = silentLoop;
        _keepAlivePlayer.Play();

        GD.Print("[Audio] KeepAlive: started silent loop to keep audio driver active");
    }
}
