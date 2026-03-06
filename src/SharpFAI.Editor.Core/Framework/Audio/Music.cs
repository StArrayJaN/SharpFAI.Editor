using LibVLCSharp.Shared;
using SharpFAI.Framework;

namespace SharpFAI_Player.Framework;

/// <summary>
/// Music implementation using LibVLC with full format support
/// 使用LibVLC的音乐实现，完整支持各种格式
/// </summary>
public class Music : IMusic, IDisposable
{
    private static LibVLC _libVLC;
    private static bool _libVLCInitialized = false;
    private static readonly object _initLock = new object();

    private MediaPlayer _mediaPlayer;
    private Media _media;
    private bool _disposed;
    private string _currentFile;
    private bool _isPreloaded;
    private float _volume = 1.0f;
    private bool _isLooping = false;

    /// <summary>
    /// Initialize LibVLC (called once) / 初始化LibVLC（仅调用一次）
    /// </summary>
    private static void InitializeLibVLC()
    {
        if (_libVLCInitialized)
            return;

        lock (_initLock)
        {
            if (_libVLCInitialized)
                return;

            try
            {
                Core.Initialize();
                _libVLC = new LibVLC();
                _libVLCInitialized = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize LibVLC: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Get current playback position in seconds / 获取当前播放位置（秒）
    /// </summary>
    public double Position
    {
        get
        {
            if (_mediaPlayer == null || _media == null)
                return 0;

            return _mediaPlayer.Time / 1000.0; // Convert ms to seconds
        }
    }

    /// <summary>
    /// Get total duration in seconds / 获取总时长（秒）
    /// </summary>
    public double Duration
    {
        get
        {
            if (_media == null)
                return 0;

            return _media.Duration / 1000.0; // Convert ms to seconds
        }
    }

    /// <summary>
    /// Music volume (0.0 - 1.0) / 音乐音量（0.0 - 1.0）
    /// </summary>
    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0.0f, 1.0f);
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Volume = (int)(_volume * 100); // LibVLC uses 0-100
            }
        }
    }

    /// <summary>
    /// Music pitch multiplier / 音乐音调倍数
    /// </summary>
    public float Pitch { get; set; } = 1.0f;

    /// <summary>
    /// Whether the music is playing / 音乐是否正在播放
    /// </summary>
    public bool IsPlaying
    {
        get => _mediaPlayer?.IsPlaying ?? false;
    }

    /// <summary>
    /// Whether the music is paused / 音乐是否暂停
    /// </summary>
    public bool IsPaused
    {
        get => _mediaPlayer != null && !_mediaPlayer.IsPlaying && _mediaPlayer.Time > 0;
    }

    /// <summary>
    /// Whether the music is looping / 音乐是否循环
    /// </summary>
    public bool IsLooping
    {
        get => _isLooping;
        set => _isLooping = value;
    }

    /// <summary>
    /// Create a new Music instance / 创建新的音乐实例
    /// </summary>
    public Music()
    {
        InitializeLibVLC();
        _mediaPlayer = new MediaPlayer(_libVLC);
        _mediaPlayer.EndReached += OnEndReached;
    }

    /// <summary>
    /// Create a new Music instance with file / 使用文件创建新的音乐实例
    /// </summary>
    public Music(string path) : this()
    {
        Load(path);
    }

    /// <summary>
    /// Load audio file / 加载音频文件
    /// </summary>
    public void Load(string path)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Music));

        if (!File.Exists(path))
            throw new FileNotFoundException($"Audio file not found: {path}");

        try
        {
            // Dispose existing media
            _media?.Dispose();

            _currentFile = path;
            _media = new Media(_libVLC, path);
            _mediaPlayer.Media = _media;
            _isPreloaded = false;
        }
        catch (Exception ex)
        {
            _media?.Dispose();
            _media = null;
            throw new InvalidOperationException($"Failed to load audio file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Preload audio to reduce playback latency / 预加载音频以减少播放延迟
    /// </summary>
    public void Preload()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Music));

        if (_media == null)
            throw new InvalidOperationException("No audio file loaded. Call Load() first.");

        if (_isPreloaded)
            return;

        try
        {
            // Parse media synchronously to ensure metadata is loaded
            // 同步解析媒体以确保元数据已加载
            var parseTask = _media.Parse(MediaParseOptions.ParseNetwork);
            parseTask.Wait(TimeSpan.FromSeconds(5)); // Wait up to 5 seconds
            
            // Start playback briefly to initialize audio pipeline
            // 短暂开始播放以初始化音频管道
            _mediaPlayer.Play();
            
            // Wait a bit for the audio pipeline to initialize
            // 等待一小段时间让音频管道初始化
            System.Threading.Thread.Sleep(50);
            
            // Pause instead of stop to keep the pipeline warm
            // 使用暂停而不是停止，以保持管道预热
            _mediaPlayer.Pause();
            
            // Reset to beginning
            // 重置到开头
            _mediaPlayer.Time = 0;
            
            _isPreloaded = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Preload warning: {ex.Message}");
            // Non-critical error, continue anyway
        }
    }

    /// <summary>
    /// Play the music / 播放音乐
    /// </summary>
    public void Play()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Music));

        if (_media == null)
            throw new InvalidOperationException("No audio file loaded. Call Load() first.");

        // If not preloaded, do a quick preload
        if (!_isPreloaded)
        {
            Preload();
        }

        // If already paused (from preload), just resume
        // 如果已经暂停（来自预加载），只需恢复播放
        if (_isPreloaded && _mediaPlayer.State == VLCState.Paused)
        {
            _mediaPlayer.Play();
        }
        else
        {
            // Otherwise start fresh
            _mediaPlayer.Play();
        }
    }

    /// <summary>
    /// Pause the music / 暂停音乐
    /// </summary>
    public void Pause()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Music));

        _mediaPlayer?.Pause();
    }

    /// <summary>
    /// Stop the music / 停止音乐
    /// </summary>
    public void Stop()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Music));

        _mediaPlayer?.Stop();
        _isPreloaded = false; // Reset preload state when stopped
    }

    /// <summary>
    /// Resume the music / 恢复音乐
    /// </summary>
    public void Resume()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Music));

        if (_mediaPlayer != null && !_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Play();
        }
    }

    /// <summary>
    /// Seek to a specific position / 跳转到指定位置
    /// </summary>
    public void Seek(double position)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Music));

        if (_mediaPlayer == null || _media == null)
            return;

        double clampedPosition = Math.Clamp(position, 0, Duration);
        _mediaPlayer.Time = (long)(clampedPosition * 1000); // Convert seconds to ms
    }

    /// <summary>
    /// Update music state / 更新音乐状态
    /// </summary>
    public void Update()
    {
        // LibVLC handles playback asynchronously
        // This method can be used for future extensions
    }

    /// <summary>
    /// Handle end reached event / 处理播放结束事件
    /// </summary>
    private void OnEndReached(object sender, EventArgs e)
    {
        if (_isLooping && !_disposed)
        {
            _mediaPlayer.Stop();
            _mediaPlayer.Play();
        }
    }

    /// <summary>
    /// Dispose music resources / 释放音乐资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        if (_mediaPlayer != null)
        {
            _mediaPlayer.EndReached -= OnEndReached;
            _mediaPlayer.Stop();
            _mediaPlayer.Dispose();
            _mediaPlayer = null;
        }

        _media?.Dispose();
        _media = null;

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~Music()
    {
        Dispose();
    }
}
