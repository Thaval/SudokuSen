namespace MySudoku.Services;

/// <summary>
/// Autoload: Manages all game audio (SFX and Music)
/// </summary>
public partial class AudioService : Node
{
    public static AudioService? Instance { get; private set; }

    [Signal]
    public delegate void MusicVolumeChangedEventHandler(float volume);

    [Signal]
    public delegate void SfxVolumeChangedEventHandler(float volume);

    // Audio buses
    private const string MUSIC_BUS = "Music";
    private const string SFX_BUS = "SFX";

    // Audio players
    private AudioStreamPlayer _menuMusicPlayer = null!;
    private AudioStreamPlayer _gameMusicPlayer = null!;
    private AudioStreamPlayer _sfxPlayer = null!;

    // SFX streams (loaded once)
    private AudioStream? _clickSound;
    private AudioStream? _cellSelectSound;
    private AudioStream? _numberPlaceSound;
    private AudioStream? _numberRemoveSound;
    private AudioStream? _notePlaceSound;
    private AudioStream? _noteRemoveSound;
    private AudioStream? _errorSound;
    private AudioStream? _successSound;
    private AudioStream? _winSound;

    // Music streams
    private AudioStream? _menuMusic;
    private AudioStream? _gameMusic;

    // State
    private bool _soundEnabled = true;
    private bool _musicEnabled = true;
    private float _sfxVolume = 0.8f;
    private float _musicVolume = 0.5f;
    private bool _isInGame = false;

    public bool SoundEnabled
    {
        get => _soundEnabled;
        set
        {
            _soundEnabled = value;
            UpdateSfxBusVolume();
        }
    }

    public bool MusicEnabled
    {
        get => _musicEnabled;
        set
        {
            _musicEnabled = value;
            UpdateMusicPlayback();
        }
    }

    public float SfxVolume
    {
        get => _sfxVolume;
        set
        {
            _sfxVolume = Mathf.Clamp(value, 0f, 1f);
            UpdateSfxBusVolume();
            EmitSignal(SignalName.SfxVolumeChanged, _sfxVolume);
        }
    }

    public float MusicVolume
    {
        get => _musicVolume;
        set
        {
            _musicVolume = Mathf.Clamp(value, 0f, 1f);
            UpdateMusicBusVolume();
            EmitSignal(SignalName.MusicVolumeChanged, _musicVolume);
        }
    }

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public override void _Ready()
    {
        // Create audio players
        _menuMusicPlayer = new AudioStreamPlayer { Bus = MUSIC_BUS };
        _gameMusicPlayer = new AudioStreamPlayer { Bus = MUSIC_BUS };
        _sfxPlayer = new AudioStreamPlayer { Bus = SFX_BUS };

        AddChild(_menuMusicPlayer);
        AddChild(_gameMusicPlayer);
        AddChild(_sfxPlayer);

        // Load audio resources
        LoadAudioResources();

        // Load settings
        var saveService = GetNode<SaveService>("/root/SaveService");
        ApplySettings(saveService.Settings);

        // Setup audio buses if they don't exist
        EnsureAudioBuses();
    }

    private void EnsureAudioBuses()
    {
        // Check if buses exist, if not we'll use Master
        int musicBusIdx = AudioServer.GetBusIndex(MUSIC_BUS);
        int sfxBusIdx = AudioServer.GetBusIndex(SFX_BUS);

        if (musicBusIdx == -1)
        {
            // Music bus doesn't exist, use Master
            _menuMusicPlayer.Bus = "Master";
            _gameMusicPlayer.Bus = "Master";
        }

        if (sfxBusIdx == -1)
        {
            // SFX bus doesn't exist, use Master
            _sfxPlayer.Bus = "Master";
        }

        UpdateMusicBusVolume();
        UpdateSfxBusVolume();
    }

    private void LoadAudioResources()
    {
        // SFX - using simple procedural sounds if files don't exist
        _clickSound = TryLoadAudio("res://Audio/click.wav", "res://Audio/click.ogg");
        _cellSelectSound = TryLoadAudio("res://Audio/cell_select.wav", "res://Audio/cell_select.ogg");
        _numberPlaceSound = TryLoadAudio("res://Audio/number_place.wav", "res://Audio/number_place.ogg");
        _numberRemoveSound = TryLoadAudio("res://Audio/number_remove.wav", "res://Audio/number_remove.ogg");
        _notePlaceSound = TryLoadAudio("res://Audio/note_place.wav", "res://Audio/note_place.ogg");
        _noteRemoveSound = TryLoadAudio("res://Audio/note_remove.wav", "res://Audio/note_remove.ogg");
        _errorSound = TryLoadAudio("res://Audio/error.wav", "res://Audio/error.ogg");
        _successSound = TryLoadAudio("res://Audio/success.wav", "res://Audio/success.ogg");
        _winSound = TryLoadAudio("res://Audio/win.wav", "res://Audio/win.ogg");

        // Music
        _menuMusic = TryLoadAudio("res://Audio/menu_music.ogg", "res://Audio/menu_music.mp3");
        _gameMusic = TryLoadAudio("res://Audio/game_music.ogg", "res://Audio/game_music.mp3");

        // Setup music players
        if (_menuMusic != null)
        {
            _menuMusicPlayer.Stream = _menuMusic;
            _menuMusicPlayer.Finished += () => { if (_musicEnabled && !_isInGame) _menuMusicPlayer.Play(); };
        }

        if (_gameMusic != null)
        {
            _gameMusicPlayer.Stream = _gameMusic;
            _gameMusicPlayer.Finished += () => { if (_musicEnabled && _isInGame) _gameMusicPlayer.Play(); };
        }
    }

    private AudioStream? TryLoadAudio(params string[] paths)
    {
        foreach (var path in paths)
        {
            if (ResourceLoader.Exists(path))
            {
                return ResourceLoader.Load<AudioStream>(path);
            }
        }
        return null;
    }

    public void ApplySettings(SettingsData settings)
    {
        _soundEnabled = settings.SoundEnabled;
        _musicEnabled = settings.MusicEnabled;
        _sfxVolume = settings.Volume / 100f;
        _musicVolume = settings.MusicVolume / 100f;

        UpdateSfxBusVolume();
        UpdateMusicBusVolume();
        UpdateMusicPlayback();
    }

    private void UpdateSfxBusVolume()
    {
        float db = _soundEnabled ? Mathf.LinearToDb(_sfxVolume) : -80f;

        int busIdx = AudioServer.GetBusIndex(SFX_BUS);
        if (busIdx >= 0)
        {
            AudioServer.SetBusVolumeDb(busIdx, db);
        }
    }

    private void UpdateMusicBusVolume()
    {
        float db = _musicEnabled ? Mathf.LinearToDb(_musicVolume) : -80f;

        int busIdx = AudioServer.GetBusIndex(MUSIC_BUS);
        if (busIdx >= 0)
        {
            AudioServer.SetBusVolumeDb(busIdx, db);
        }

        // Also set individual player volumes as fallback
        _menuMusicPlayer.VolumeDb = db;
        _gameMusicPlayer.VolumeDb = db;
    }

    private void UpdateMusicPlayback()
    {
        if (_musicEnabled)
        {
            if (_isInGame)
            {
                if (!_gameMusicPlayer.Playing && _gameMusic != null)
                    _gameMusicPlayer.Play();
                _menuMusicPlayer.Stop();
            }
            else
            {
                if (!_menuMusicPlayer.Playing && _menuMusic != null)
                    _menuMusicPlayer.Play();
                _gameMusicPlayer.Stop();
            }
        }
        else
        {
            _menuMusicPlayer.Stop();
            _gameMusicPlayer.Stop();
        }
    }

    // --- Public SFX Methods ---

    public void PlayClick()
    {
        PlaySfx(_clickSound);
    }

    public void PlayCellSelect()
    {
        PlaySfx(_cellSelectSound ?? _clickSound);
    }

    public void PlayNumberPlace()
    {
        PlaySfx(_numberPlaceSound ?? _clickSound);
    }

    public void PlayNumberRemove()
    {
        PlaySfx(_numberRemoveSound ?? _clickSound);
    }

    public void PlayNotePlaceOrRemove(bool isPlacing)
    {
        if (isPlacing)
            PlaySfx(_notePlaceSound ?? _clickSound);
        else
            PlaySfx(_noteRemoveSound ?? _clickSound);
    }

    public void PlayError()
    {
        PlaySfx(_errorSound);
    }

    public void PlaySuccess()
    {
        PlaySfx(_successSound ?? _clickSound);
    }

    public void PlayWin()
    {
        PlaySfx(_winSound ?? _successSound);
    }

    private void PlaySfx(AudioStream? stream)
    {
        if (!_soundEnabled || stream == null) return;

        _sfxPlayer.Stream = stream;
        _sfxPlayer.VolumeDb = Mathf.LinearToDb(_sfxVolume);
        _sfxPlayer.Play();
    }

    // --- Music Control ---

    public void SetInGame(bool inGame)
    {
        if (_isInGame == inGame) return;

        _isInGame = inGame;

        // Fade transition would be nice, but for simplicity just switch
        if (_musicEnabled)
        {
            UpdateMusicPlayback();
        }
    }

    public void StartMenuMusic()
    {
        SetInGame(false);
    }

    public void StartGameMusic()
    {
        SetInGame(true);
    }

    public void StopAllMusic()
    {
        _menuMusicPlayer.Stop();
        _gameMusicPlayer.Stop();
    }
}
