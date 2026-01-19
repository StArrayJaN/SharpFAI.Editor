using System.Drawing;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpFAI.Framework;
using SharpFAI.Serialization;
using SharpFAI.Util;
using SharpFAI_Player.Framework;
using SharpFAI_Player.Native;

namespace SharpFAI_Player;

/// <summary>
/// 关卡编辑器 - 独立实现的简单编辑器
/// Level Editor - Standalone simple editor implementation
/// </summary>
public partial class EditorPlayer : GameWindow
{
    #region ImGui Fields
    private ImGuiController? _imGuiController;
    private readonly Vector4 _clearColor = new(0.05f, 0.05f, 0.05f, 1.0f);
    #endregion
    
    #region Editor UI State
    private bool _showAboutWindow;
    
    // 面板折叠状态
    private bool _leftPanelCollapsed;
    private bool _rightPanelCollapsed;
    private bool _bottomPanelCollapsed;
    
    // 双击检测（简化为单个标签）
    private double _lastLeftTabClickTime;
    private double _lastRightTabClickTime;
    private double _lastBottomTabClickTime;
    private const double DoubleClickTime = 0.3;
    #endregion
    
    #region Level Data
    private Level? _level;
    private string? _levelPath;
    private List<Floor>? _floors;
    private List<int> _selectedFloorIndices = new();
    private List<Floor> _selectedFloors = new();
    
    // 渲染相关
    private List<PlayerFloor>? _playerFloors;
    private List<PlayerFloor>? _renderFloors;
    private GLShader? _shader;
    private Camera2D? _camera2D;
    private bool _initialized;
    private bool _needsGLInitialization; // 标记需要在主线程初始化 OpenGL 对象
    #endregion
    
    #region Editor State
    private string _statusMessage = "就绪 Ready";
    
    // 可调整的面板宽度
    private float _leftPanelWidth = 350f;
    private float _rightPanelWidth = 350f;
    private float _bottomPanelHeight = 200f;
    #endregion
    
    #region Playback State
    private bool _isPlaying;
    private bool _showPlanets; // 控制星球显示但不影响时间进度
    private double _currentTime;
    private double _playbackSpeed = 1.0;
    private int _currentIndex;
    private Floor? _currentFloor;
    
    // 音频
    private Music? _music;
    private Music? _hitSound;
    private List<double>? _noteTimes;
    
    // 音频设置
    private string _musicFilePath = "";
    private float _bpm = 120f;
    private float _pitch = 100f;
    
    // 星球
    private Planet? _redPlanet;
    private Planet? _bluePlanet;
    private Planet? _currentPlanet;
    private Planet? _lastPlanet;
    
    // 旋转
    private double _angle;
    private bool _isCw;
    private double _rotationSpeed;
    
    // 摄像机跟随
    private Vector2 _cameraFromPos;
    private Vector2 _cameraToPos;
    private float _cameraTimer;
    private float _cameraSpeed = 2.0f;
    #endregion
    
    #region Input State
    private bool _isShiftPressed;
    #endregion

    public EditorPlayer(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, string? levelPath)
        : base(gameWindowSettings, nativeWindowSettings)
    {
        _levelPath = levelPath;
        
        // 如果没有提供关卡路径，创建一个新的默认关卡
        // If no level path is provided, create a new default level
        if (string.IsNullOrEmpty(levelPath))
        {
            _level = Level.CreateNewLevel();
            _statusMessage = "已创建新关卡 New level created";
        }
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        
        GL.ClearColor(_clearColor.X, _clearColor.Y, _clearColor.Z, _clearColor.W);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _imGuiController = new ImGuiController(ClientSize.X, ClientSize.Y);
        
        // 初始化摄像机
        _camera2D = new Camera2D(ClientSize.X, ClientSize.Y);
        _camera2D.Zoom = 3f;
        
        // 初始化着色器
        _shader = GLShader.CreateDefault2D();
        _shader.Compile();
        
        if (!string.IsNullOrEmpty(_levelPath))
        {
            LoadLevel(_levelPath);
        }
        else if (_level != null)
        {
            // 如果有默认关卡，初始化它
            // If there's a default level, initialize it
            LoadLevel(null);
        }
        else
        {
            _statusMessage = "请打开关卡文件或使用默认关卡 Please open a level file or use default level";
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        
        // 如果需要初始化 OpenGL 对象，在渲染循环中执行（确保在主线程）
        if (_needsGLInitialization)
        {
            InitializeGLObjects();
        }
        
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        // 渲染轨道
        RenderTracks();
        
        if (_imGuiController != null)
        {
            try
            {
                // ImGui.Update 已经在 OnUpdateFrame 中调用
                // ImGui.Update is already called in OnUpdateFrame
                RenderEditorUi();
                _imGuiController.Render();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ImGui render error: {ex}");
            }
        }
        
        SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        
        // 先更新 ImGui 以获取最新的 IO 状态
        // Update ImGui first to get the latest IO state
        if (_imGuiController != null)
        {
            _imGuiController.Update(this, (float)args.Time);
        }
        
        // ImGui 是否捕获输入（现在是最新状态）
        var io = ImGuiNET.ImGui.GetIO();
        var imguiWantsMouse = io.WantCaptureMouse;
        var imguiWantsKeyboard = io.WantCaptureKeyboard;
        var imguiWantsTextInput = io.WantTextInput;
        
        // ESC 键停止播放（仅在 ImGui 不需要键盘输入时处理）
        if (KeyboardState.IsKeyPressed(Keys.Escape) && !imguiWantsKeyboard && !imguiWantsTextInput)
        {
            if (_isPlaying)
            {
                StopPlay();
            }
        }
        
        // Shift 状态
        _isShiftPressed = KeyboardState.IsKeyDown(Keys.LeftShift) || KeyboardState.IsKeyDown(Keys.RightShift);
        
        // 输入处理（仅在 ImGui 不需要任何输入时）
        if (!imguiWantsMouse && !imguiWantsKeyboard && !imguiWantsTextInput)
        {
            HandleInput();
        }
        else if (imguiWantsTextInput || imguiWantsKeyboard)
        {
            // ImGui 正在处理文本输入，清除游戏输入状态
            _isDragging = false;
            _wasMouseMoving = false;
        }
        
        // 更新摄像机
        _camera2D?.Update((float)args.Time);
        
        // 更新星球（拖尾效果）
        _currentPlanet?.Update((float)args.Time);
        _lastPlanet?.Update((float)args.Time);
        
        // 更新播放
        UpdatePlayer(args.Time);
    }

    protected override void OnUnload()
    {
        // 停止播放
        _isPlaying = false;
        
        // 释放音频资源
        _music?.Dispose();
        _music = null;
        _hitSound?.Dispose();
        _hitSound = null;
        
        // 释放轨道资源
        if (_playerFloors != null)
        {
            foreach (var floor in _playerFloors)
            {
                floor?.Dispose();
            }
            _playerFloors = null;
        }
        _renderFloors = null;
        
        // 释放星球
        _redPlanet?.Dispose();
        _bluePlanet?.Dispose();
        _redPlanet = null;
        _bluePlanet = null;
        _lastPlanet = null;
        _currentPlanet = null;
        
        // 释放着色器
        _shader?.Dispose();
        _shader = null;
        
        _imGuiController?.Dispose();
        base.OnUnload();
    }
    
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        _imGuiController?.WindowResized(e.Width, e.Height);
        
        if (_camera2D != null)
        {
            _camera2D.ViewportWidth = e.Width;
            _camera2D.ViewportHeight = e.Height;
        }
    }
    
    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        _imGuiController?.PressChar((char)e.Unicode);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        _imGuiController?.MouseScroll(e.Offset);
    }

    #region Editor Methods
    
    private void SelectFloor(int index, bool addToSelection = false)
    {
        if (_floors == null || index < 0 || index >= _floors.Count)
            return;
        
        if (!addToSelection)
        {
            _selectedFloorIndices.Clear();
            _selectedFloors.Clear();
        }
        
        if (_selectedFloorIndices.Contains(index))
        {
            _selectedFloorIndices.Remove(index);
            _selectedFloors.RemoveAll(f => f == _floors[index]);
        }
        else
        {
            _selectedFloorIndices.Add(index);
            _selectedFloors.Add(_floors[index]);
        }
        
        _statusMessage = _selectedFloorIndices.Count > 0 
            ? $"已选择 {_selectedFloorIndices.Count} 个地板" 
            : "未选择地板";
    }
    
    private void ClearSelection()
    {
        _selectedFloorIndices.Clear();
        _selectedFloors.Clear();
        _statusMessage = "已清除选择";
    }

    private async void LoadLevel(string? levelPath)
    {
        try
        {
            _statusMessage = "正在加载关卡... Loading level...";
            _initialized = false;
            _needsGLInitialization = false;
            
            // 停止播放
            StopPlay();
            
            // 释放旧的音频资源
            _music?.Dispose();
            _music = null;
            _hitSound?.Dispose();
            _hitSound = null;
            
            // 释放旧的轨道资源
            if (_playerFloors != null)
            {
                foreach (var floor in _playerFloors)
                {
                    floor?.Dispose();
                }
                _playerFloors = null;
            }
            _renderFloors = null;
            
            // 释放星球
            _redPlanet?.Dispose();
            _bluePlanet?.Dispose();
            _redPlanet = null;
            _bluePlanet = null;
            _lastPlanet = null;
            _currentPlanet = null;
            
            // 如果提供了路径，加载新关卡；否则使用现有的 _level
            // If path is provided, load new level; otherwise use existing _level
            if (!string.IsNullOrEmpty(levelPath))
            {
                _level = new Level(levelPath);
                _levelPath = levelPath;
            }
            else if (_level == null)
            {
                // 如果没有关卡，创建一个新的
                // If no level exists, create a new one
                _level = Level.CreateNewLevel();
                _statusMessage = "已创建新关卡 New level created";
            }
            
            // 从关卡读取设置到编辑器字段
            // Read settings from level to editor fields
            if (_level.HasSetting("bpm"))
            {
                _bpm = (float)_level.GetSetting<double>("bpm");
            }
            if (_level.HasSetting("pitch"))
            {
                _pitch = (float)_level.GetSetting<double>("pitch");
            }
            
            _statusMessage = "正在计算音符时间... Calculating note times...";
            var noteTimes = await Task.Run(() => _level.GetNoteTimes());
            _noteTimes = noteTimes.Select(x => x - noteTimes[0]).ToList();
            
            _statusMessage = "正在生成地板... Generating floors...";
            var floors = await Task.Run(() => _level.CreateFloors(usePositionTrack: true));
            _floors = floors;
            
            _statusMessage = "正在初始化音频... Initializing audio...";
            try
            {
                // 尝试获取音频路径
                string? audioPath = null;
                
                if (!string.IsNullOrEmpty(_musicFilePath))
                {
                    audioPath = _musicFilePath;
                }
                else if (_level.HasSetting("songFilename") && !string.IsNullOrEmpty(_level.GetSetting<string>("songFilename")))
                {
                    try
                    {
                        audioPath = _level.GetAudioPath();
                    }
                    catch
                    {
                        // 音频文件不存在，继续不加载音乐
                        Console.WriteLine("Audio file not found in level settings");
                    }
                }
                
                if (!string.IsNullOrEmpty(audioPath) && File.Exists(audioPath))
                {
                    _music = new Music(audioPath);
                    _music.Preload();
                    _statusMessage = "音频加载成功 Audio loaded";
                }
                else
                {
                    _statusMessage = "未找到音频文件，将以无音乐模式运行 No audio file found, running without music";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to load music: {e}");
                _statusMessage = "音频加载失败，将以无音乐模式运行 Failed to load audio, running without music";
            }
            
            // 生成命中音效
            try
            {
                string hitSoundPath = Path.Combine(
                    string.IsNullOrEmpty(_levelPath) ? Path.GetTempPath() : Path.GetDirectoryName(_levelPath) ?? "",
                    "hitSound.wav");
                    
                await Task.Run(() => new AudioMerger().Export("kick.wav".ExportAssets(), _noteTimes, hitSoundPath));
                _hitSound = new Music(hitSoundPath);
                _hitSound.Preload();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to create hitsound: {e}");
            }
            
            _selectedFloorIndices.Clear();
            _selectedFloors.Clear();
            
            // 初始化播放状态
            _currentIndex = 0;
            _currentFloor = _floors[0];
            _angle = 0;
            _isCw = _currentFloor.isCW;
            
            // 设置摄像机位置到第一个地板
            if (_floors.Count > 0 && _camera2D != null)
            {
                _camera2D.Position = _floors[0].position;
                _cameraFromPos = _floors[0].position;
                _cameraToPos = _floors[0].position;
                _cameraTimer = 0f;
            }
            
            // 标记需要在渲染循环中初始化 OpenGL 对象
            _needsGLInitialization = true;
            _statusMessage = $"加载完成！共 {_floors.Count} 个地板，正在初始化渲染...";
        }
        catch (Exception ex)
        {
            _statusMessage = $"加载失败 Load failed: {ex.Message}";
            Console.WriteLine($"Failed to load level: {ex}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            _initialized = false;
        }
    }
    
    private void InitializeGLObjects()
    {
        if (!_needsGLInitialization || _floors == null)
            return;
        
        try
        {
            // 初始化星球（在主线程，必须最先创建）
            _redPlanet = new Planet(Color.Red);
            _bluePlanet = new Planet(Color.Blue);
            _redPlanet.Radius = Floor.width;
            _bluePlanet.Radius = Floor.width;
            _lastPlanet = _redPlanet;
            _currentPlanet = _bluePlanet;
            
            _statusMessage = "正在生成轨道网格... Generating track meshes...";
            
            // 生成轨道网格（在主线程）
            _playerFloors = _floors.Select(x => new PlayerFloor(x)).ToList();
            _renderFloors = _playerFloors.OrderBy(x => x.floor.renderOrder).ToList();
            
            _needsGLInitialization = false;
            _initialized = true;
            _statusMessage = $"加载完成！共 {_floors.Count} 个地板，按空格播放 Press Space to play";
        }
        catch (Exception ex)
        {
            _statusMessage = $"OpenGL 初始化失败: {ex.Message}";
            Console.WriteLine($"Failed to initialize GL objects: {ex}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            _needsGLInitialization = false;
            _initialized = false;
        }
    }

    private void OpenLevelFile()
    {
        var filePath = NativeAPI.OpenFileDialog(new NativeAPI.FileFilter
        {
            Name = "关卡文件",
            Filter = ["*.adofai"],
            IncludeAllFiles = true
        });
        
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            LoadLevel(filePath);
        }
    }
    
    private void SaveLevel()
    {
        if (_level == null)
        {
            _statusMessage = "没有可保存的关卡";
            return;
        }
        
        // 如果没有路径，调用另存为
        if (string.IsNullOrEmpty(_levelPath))
        {
            SaveLevelAs();
            return;
        }
        
        try
        {
            _level.Save(_levelPath);
            _statusMessage = $"已保存: {Path.GetFileName(_levelPath)}";
        }
        catch (Exception ex)
        {
            _statusMessage = $"保存失败: {ex.Message}";
            Console.WriteLine($"Save error: {ex}");
        }
    }
    
    private void SaveLevelAs()
    {
        if (_level == null)
        {
            _statusMessage = "没有可保存的关卡";
            return;
        }
        
        // 生成默认文件名
        string defaultFileName = "level.adofai";
        if (!string.IsNullOrEmpty(_levelPath))
        {
            defaultFileName = Path.GetFileName(_levelPath);
        }
        
        var filePath = NativeAPI.SaveFileDialog(new NativeAPI.FileFilter
        {
            Name = "关卡文件",
            Filter = ["*.adofai"],
            IncludeAllFiles = false
        }, defaultFileName);
        
        if (!string.IsNullOrEmpty(filePath))
        {
            try
            {
                // 确保文件有正确的扩展名
                if (!filePath.EndsWith(".adofai", StringComparison.OrdinalIgnoreCase))
                {
                    filePath += ".adofai";
                }
                
                _level.Save(filePath);
                _levelPath = filePath;
                _statusMessage = $"已另存为: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                _statusMessage = $"另存为失败: {ex.Message}";
                Console.WriteLine($"Save as error: {ex}");
            }
        }
    }
    
    #endregion
}
