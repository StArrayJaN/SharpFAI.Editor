using System.Drawing;
using System.Numerics;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpFAI_Player.Framework;
using SharpFAI.Editor.Core.Framework.Graphics;
using SharpFAI.Editor.Core.Models;
using SharpFAI.Editor.Core.UI;
using SharpFAI.Editor.Core.Util;
using SharpFAI.Editor.Platform.Native;
using SharpFAI.Events;
using SharpFAI.Framework;
using SharpFAI.Serialization;
using SharpFAI.Util;

namespace SharpFAI_Player;
#pragma warning disable all
public class MainPlayer: GameWindow, IPlayer
{
    #region ImGui Fields
    protected ImGuiController? _imGuiController;
    protected bool _showControlPanel = false;
    protected bool _showLevelInfo = false;
    protected bool _showAboutWindow = false;
    protected System.Numerics.Vector4 _clearColor = new System.Numerics.Vector4(0.05f, 0.05f, 0.05f, 1.0f);
    #endregion
    
    [Note("对象变量")]
    protected Level? level;
    protected List<Floor> floors;
    protected Camera2D camera2D;
    protected Music music;
    protected Music hitSound;
    protected List<PlayerFloor> renderFloors;
    protected List<PlayerFloor> playerFloors;
    protected GLShader shader;
    protected Floor currentFloor;
    protected Planet lastPlanet;
    protected Planet currentPlanet;
    protected Planet redPlanet;
    protected Planet bluePlanet;
    protected List<double> noteTimes;

    [Note("基础类型变量")]
    protected bool isStarted;
    protected double angle;
    protected bool isCw;
    protected int currentIndex;
    protected bool initialized;
    protected string state;
    protected nint hwnd;
    protected double rotationSpeed;
    protected double currentTime;
    
    [Note("摄像机相关")]
    protected Vector2 cameraFromPos;
    protected Vector2 cameraToPos;
    protected float cameraTimer;
    protected float cameraSpeed = 2.0f;
    
    public MainPlayer(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, string? levelPath) 
        : base(gameWindowSettings, nativeWindowSettings)
    {
        if (!string.IsNullOrEmpty(levelPath))
        {
            level = new Level(levelPath);
        }
    }

    #region GL LifeCycle Methods 
    protected override void OnLoad()
    {
        base.OnLoad();
        
        // Initialize OpenGL
        GL.ClearColor(_clearColor.X, _clearColor.Y, _clearColor.Z, _clearColor.W);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // Initialize ImGui
        _imGuiController = new ImGuiController(ClientSize.X, ClientSize.Y);
        
        CreatePlayer();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        
        // Clear screen
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        // Render game
        RenderPlayer(args.Time);
        
        // Update and render ImGui (with safety check)
        if (_imGuiController != null)
        {
            try
            {
                _imGuiController.Update(this, (float)args.Time);
                RenderImGui();
                _imGuiController.Render();
            }
            catch (Exception ex)
            {
                // Silently catch ImGui rendering exceptions during shutdown
                Console.WriteLine($"ImGui render error (likely during shutdown): {ex}");
            }
        }
        
        SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        UpdatePlayer(args.Time);
    }

    protected override void OnUnload()
    {
        _imGuiController?.Dispose();
        DestroyPlayer();
        base.OnUnload();
    }
    
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        
        GL.Viewport(0, 0, e.Width, e.Height);
        _imGuiController?.WindowResized(e.Width, e.Height);
        
        if (camera2D != null)
        {
            camera2D.ViewportWidth = e.Width;
            camera2D.ViewportHeight = e.Height;
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
    #endregion
    
    public async void CreatePlayer()
    {
        // Check if level is loaded
        if (level == null)
        {
            state = "请从菜单打开关卡文件 (文件 -> 打开关卡)";
            initialized = false;
            return;
        }
        
        // Define local progress callback method
        // 定义本地进度回调方法
        void OnProgress(int progress, string message)
        {
            state = $"{progress}%:{message}";
        }
        
        try
        {
            if (OperatingSystem.IsWindows())
            {
                hwnd = NativeAPI.GetForegroundWindow();
            }
            
            // Add progress callback
            // 添加进度回调
            LevelUtils.progressCallback += OnProgress;
            
            // Dispose old shader if exists
            if (shader != null)
            {
                shader.Dispose();
                shader = null;
            }
            
            // Create or reuse camera
            if (camera2D == null)
            {
                camera2D = new (ClientSize.X, ClientSize.Y);
                camera2D.Zoom = 3;
            }
            
            // Dispose old planets if they exist
            redPlanet?.Dispose();
            bluePlanet?.Dispose();
            
            redPlanet = new Planet(Color.Red);
            bluePlanet = new Planet(Color.Blue);
            redPlanet.Radius = Floor.width;
            bluePlanet.Radius = Floor.width;
            lastPlanet = redPlanet;
            currentPlanet = bluePlanet;
            
            shader = GLShader.CreateDefault2D();
            shader.Compile();
            state = "初始化轨道";
            
            // Capture level reference to avoid race condition
            var currentLevel = level;
            
            noteTimes = await Task.Run(() => currentLevel.GetNoteTimes().Select(a=>a.Item1).ToList());
            noteTimes = noteTimes.Select(x => x - noteTimes[0]).ToList();
            floors = await Task.Run(() => currentLevel.CreateFloors(usePositionTrack:true));
            
            if (!shader.IsCompiled)
            {
                Console.WriteLine(shader.CompileLog);
            }
            state = "初始化音频";
            try
            {
                music = new (currentLevel.GetAudioPath());
                // Preload music to reduce playback latency / 预加载音乐以减少播放延迟
                music.Preload();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            await Task.Run(() => new AudioMerger().Export("kick.wav".ExportAssets(), noteTimes,
                Path.Combine(Path.GetDirectoryName(currentLevel.pathToLevel), "hitSound.wav")));
            hitSound = new (Path.Combine(Path.GetDirectoryName(currentLevel.pathToLevel), "hitSound.wav"));
            // Preload hitsound to reduce playback latency / 预加载命中音效以减少播放延迟
            hitSound.Preload();
            state = "生成轨道网格";

            playerFloors = await Task.Run(() => floors.Select(x =>
            {
                //GLTexture texture = null;
                Twirl twirl = null;
                SetSpeed setSpeed = null;
                foreach (var e in x.events)
                {
                    if (e.EventType == EventType.Twirl) twirl = e.ToEvent<Twirl>();
                    if (e.EventType == EventType.SetSpeed) setSpeed = e.ToEvent<SetSpeed>();
                }
                return new PlayerFloor(x);
            }).ToList());
            renderFloors = playerFloors.OrderBy(x => x.floor.renderOrder).ToList();
            currentFloor = floors[0];
            cameraFromPos = currentFloor.position;
            cameraToPos = currentFloor.position;
            camera2D.Position = currentFloor.position;
            cameraTimer = 0f;
            initialized = true;
            state = "初始化完成，按空格播放，按R重新播放";
            if (OperatingSystem.IsWindows())
            {
                NativeAPI.ShowWindow(hwnd, 3);
            }
            
            // Remove progress callback
            // 移除进度回调
            LevelUtils.progressCallback -= OnProgress;
        }
        catch (Exception ex)
        {
            state = $"初始化失败: {ex.Message}";
            Console.WriteLine($"CreatePlayer error: {ex}");
            initialized = false;
            
            // Remove progress callback even on error
            // 即使出错也要移除进度回调
            LevelUtils.progressCallback -= OnProgress;
        }
    }

    public void UpdatePlayer(double delta)
    {
        // Early return if not initialized
        if (!initialized || camera2D == null)
            return;
            
        // Manual camera control with mouse / 用鼠标手动控制摄像机
        if (MouseState.IsButtonDown(MouseButton.Left))
        {
            Vector2 deltaMove = new Vector2(-MouseState.Delta.X, MouseState.Delta.Y);
            camera2D.Position += deltaMove;
            cameraFromPos += deltaMove;
            cameraToPos += deltaMove;
        }
        
        // Playback controls / 播放控制
        if (KeyboardState.IsKeyPressed(Keys.Space))
        {
            if (!isStarted)
            {
                StartPlay();
            }
            else
            {
                PausePlay();
            }
        }
    
        if (KeyboardState.IsKeyPressed(Keys.R))
        {
            ResetPlayer();
            StartPlay();
        }
        
        // Pause/Resume with P key / 使用 P 键暂停/恢复
        if (KeyboardState.IsKeyPressed(Keys.P) && initialized)
        {
            if (isStarted)
            {
                PausePlay();
            }
            else
            {
                ResumePlay();
            }
        }

        if (isStarted)
        {
            currentTime += delta;
            
            // Check if playback has finished / 检查播放是否已结束
            double totalTime = music?.Duration ?? 0;
            if (totalTime > 0 && currentTime >= totalTime)
            {
                // Clamp time to total duration to prevent overflow / 限制时间不超过总时长以防止越界
                currentTime = totalTime;
                PausePlay();
            }
            
            while (currentIndex < noteTimes.Count -1 && currentTime >= noteTimes[currentIndex] / 1000)
            {
                currentIndex++;
                MoveToNextFloor(floors[currentIndex]);
            }
        }
        // Update rotation / 更新旋转
        rotationSpeed = (isCw ? -1 : 1) * (currentFloor.bpm / 60f) * 180 * delta;
        angle += rotationSpeed;
        if (angle >= 360) angle = 0;
        isCw = currentFloor.isCW;
        
        float bpm = (float)currentFloor.bpm;
        cameraSpeed = (60f / bpm) * 2f; // crotchet * 2
        
        cameraTimer += (float)delta;
        
        float distance = Vector2.Distance(cameraFromPos, cameraToPos);
        float speedMultiplier = 1.0f;
        if (distance > 5f)
        {
            float distanceFactor = FloatMath.Min(1.0f, (distance - 5f) / 5f);
            speedMultiplier = distanceFactor * 0.5f + 1f;
        }
    
        // Lerp camera position / 插值摄像机位置
        float t = cameraTimer / (cameraSpeed / speedMultiplier);
        if (t > 1.0f) t = 1.0f;
    
        camera2D.Position = Vector2.Lerp(cameraFromPos, cameraToPos, t);
    
        // 更新摄像机状态，包括震动效果
        camera2D.Update((float)delta);
    }

    public void RenderPlayer(double delta)
    {
        // Only render if initialized
        if (!initialized || shader == null || camera2D == null)
            return;
            
        shader.Use();
        camera2D.Render(shader);
        
        for (int i = 0; i < renderFloors.Count; i++)
        {
            var floor = renderFloors[i];
            if (camera2D.IsPointVisible(new (floor.floor.position.X,floor.floor.position.Y)))
            {
                floor.Render(shader);
            }
        }
        // Update and render planets / 更新并渲染星球
        currentPlanet.Update((float)delta);
        lastPlanet.Update((float)delta);
        
        currentPlanet.Position = currentFloor.position;
        
        Vector2 offset = new Vector2(
            FloatMath.Cos(angle, true) * Floor.length * 2,
            FloatMath.Sin(angle, true) * Floor.length * 2
        );
        lastPlanet.Position = offset + currentFloor.position;
    
        bluePlanet.Render(shader, camera2D);
        redPlanet.Render(shader, camera2D);
    }
    
    private void RenderImGui()
    {
        // Render foreground overlay text (BPM, track info, time) / 渲染前景覆盖文本（BPM、轨道信息、时间）
        RenderForegroundInfo();
        
        // Control panel with debug info / 控制面板（含调试信息）
        if (_showControlPanel)
        {
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, 10), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(320, 0), ImGuiCond.FirstUseEver);
            
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 10f);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 5f);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new System.Numerics.Vector4(0.1f, 0.1f, 0.15f, 0.95f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new System.Numerics.Vector4(0.2f, 0.3f, 0.5f, 1.0f));
            
            bool beginResult = ImGui.Begin("控制面板 Control Panel", ref _showControlPanel, ImGuiWindowFlags.NoCollapse);
            
            if (beginResult)
            {
                var drawList = ImGui.GetWindowDrawList();
                
                // Status indicator with icon / 带图标的状态指示器
                var cursorPos = ImGui.GetCursorScreenPos();
                
                // Draw status icon (circle)
                // 绘制状态图标（圆形）
                if (isStarted)
                {
                    // Playing - green circle
                    drawList.AddCircleFilled(
                        new System.Numerics.Vector2(cursorPos.X + 10, cursorPos.Y + 10),
                        6,
                        ImGui.GetColorU32(new System.Numerics.Vector4(0.2f, 1.0f, 0.3f, 1.0f))
                    );
                }
                else
                {
                    // Paused - orange circle
                    drawList.AddCircleFilled(
                        new System.Numerics.Vector2(cursorPos.X + 10, cursorPos.Y + 10),
                        6,
                        ImGui.GetColorU32(new System.Numerics.Vector4(1.0f, 0.7f, 0.2f, 1.0f))
                    );
                }
                
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 25);
                ImGui.PushStyleColor(ImGuiCol.Text, isStarted ? 
                    new System.Numerics.Vector4(0.2f, 1.0f, 0.3f, 1.0f) : 
                    new System.Numerics.Vector4(1.0f, 0.7f, 0.2f, 1.0f));
                ImGui.Text(isStarted ? "播放中 Playing" : "暂停 Paused");
                ImGui.PopStyleColor();
                
                ImGui.Separator();
                
                // Playback controls / 播放控制
                ImGui.Text("播放控制");
                ImGui.Spacing();
                
                if (!isStarted)
                {
                    if (ImGui.Button("开始播放 Start (Space)", new System.Numerics.Vector2(-1, 30)))
                    {
                        StartPlay();
                    }
                }
                
                if (ImGui.Button("重新开始 Restart (R)", new System.Numerics.Vector2(-1, 30)))
                {
                    ResetPlayer();
                    StartPlay();
                }
                
                ImGui.Separator();
                
                // Progress info with visual bar / 带可视化进度条的进度信息
                ImGui.Text("进度信息");
                ImGui.Spacing();
                
                if (floors != null && floors.Count > 0)
                {
                    float progress = (float)currentIndex / (floors.Count - 1);
                    
                    // Custom progress bar with gradient
                    // 带渐变的自定义进度条
                    var progressBarPos = ImGui.GetCursorScreenPos();
                    var progressBarSize = new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 20);
                    
                    // Background
                    drawList.AddRectFilled(
                        progressBarPos,
                        new System.Numerics.Vector2(progressBarPos.X + progressBarSize.X, progressBarPos.Y + progressBarSize.Y),
                        ImGui.GetColorU32(new System.Numerics.Vector4(0.2f, 0.2f, 0.2f, 1.0f)),
                        5.0f
                    );
                    
                    // Progress fill with gradient
                    if (progress > 0)
                    {
                        drawList.AddRectFilledMultiColor(
                            progressBarPos,
                            new System.Numerics.Vector2(progressBarPos.X + progressBarSize.X * progress, progressBarPos.Y + progressBarSize.Y),
                            ImGui.GetColorU32(new System.Numerics.Vector4(0.2f, 0.6f, 1.0f, 1.0f)), // Left - blue
                            ImGui.GetColorU32(new System.Numerics.Vector4(0.6f, 0.2f, 1.0f, 1.0f)), // Right - purple
                            ImGui.GetColorU32(new System.Numerics.Vector4(0.6f, 0.2f, 1.0f, 1.0f)), // Bottom right
                            ImGui.GetColorU32(new System.Numerics.Vector4(0.2f, 0.6f, 1.0f, 1.0f))  // Bottom left
                        );
                    }
                    
                    // Progress text
                    var progressText = $"{currentIndex}/{floors.Count - 1}";
                    var textSize = ImGui.CalcTextSize(progressText);
                    drawList.AddText(
                        new System.Numerics.Vector2(
                            progressBarPos.X + (progressBarSize.X - textSize.X) / 2,
                            progressBarPos.Y + (progressBarSize.Y - textSize.Y) / 2
                        ),
                        ImGui.GetColorU32(new System.Numerics.Vector4(1, 1, 1, 1)),
                        progressText
                    );
                    
                    ImGui.Dummy(progressBarSize);
                    ImGui.Spacing();
                    
                    ImGui.Text($"时间: {currentTime:F2}s");
                    ImGui.Text($"当前 BPM: {currentFloor?.bpm:F1}");
                    ImGui.Text($"旋转角度: {angle:F1}°");
                }
                
                ImGui.Separator();
                
                // Debug info / 调试信息
                ImGui.Text("调试信息");
                ImGui.Spacing();
                
                ImGui.Text($"FPS: {1.0 / UpdateTime:F1}");
                ImGui.Text($"帧时间: {UpdateTime * 1000:F2} ms");
                ImGui.Text($"窗口: {ClientSize.X} x {ClientSize.Y}");
                
                ImGui.Separator();
                
                // Gradient shader control / 渐变着色器控制
                ImGui.Text("文字渐变效果");
                
                bool gradientEnabled = _imGuiController?.IsGradientShaderEnabled() ?? false;
                if (ImGui.Checkbox("启用渐变 Enable Gradient", ref gradientEnabled))
                {
                    _imGuiController?.ToggleGradientShader();
                }
                
                if (gradientEnabled)
                {
                    int currentMode = _imGuiController?.GetGradientMode() ?? 0;
                    string[] modeNames = { "水平 Horizontal", "垂直 Vertical", "对角 Diagonal", "彩虹波浪 Rainbow" };
                    
                    if (ImGui.Combo("渐变模式", ref currentMode, modeNames, modeNames.Length))
                    {
                        _imGuiController?.SetGradientMode(currentMode);
                    }
                }
                
                ImGui.Separator();
                
                // Background color / 背景颜色
                System.Numerics.Vector3 bgColor = new System.Numerics.Vector3(_clearColor.X, _clearColor.Y, _clearColor.Z);
                if (ImGui.ColorEdit3("背景颜色 BG Color", ref bgColor))
                {
                    _clearColor = new System.Numerics.Vector4(bgColor.X, bgColor.Y, bgColor.Z, 1.0f);
                    GL.ClearColor(_clearColor.X, _clearColor.Y, _clearColor.Z, _clearColor.W);
                }
                
                ImGui.Separator();
                
                // Window toggles / 窗口切换
                ImGui.Text("窗口");
                ImGui.Spacing();
                
                // Only allow toggling level info if level is loaded / 仅在关卡已加载时允许切换关卡信息
                if (level != null && initialized)
                {
                    ImGui.Checkbox("关卡信息 Level Info", ref _showLevelInfo);
                }
                else
                {
                    ImGui.BeginDisabled();
                    bool disabledLevelInfo = false;
                    ImGui.Checkbox("关卡信息 Level Info (需要加载关卡)", ref disabledLevelInfo);
                    ImGui.EndDisabled();
                }
                
                ImGui.Separator();
                ImGui.TextWrapped($"状态: {state}");
            }
            
            ImGui.End();
            ImGui.PopStyleColor(2);
            ImGui.PopStyleVar(2);
        }
        
        // Level info window / 关卡信息窗口
        // Only show if level is loaded and initialized / 仅在关卡已加载且已初始化时显示
        if (_showLevelInfo && level != null && initialized)
        {
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(ClientSize.X / 2 - 200, ClientSize.Y - 150), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(400, 0), ImGuiCond.FirstUseEver);
            
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 10f);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new System.Numerics.Vector4(0.1f, 0.15f, 0.1f, 0.95f));
            
            bool beginResult = ImGui.Begin("关卡信息 Level Info", ref _showLevelInfo);
            
            if (beginResult)
            {
                ImGui.Text($"路径: {Path.GetFileName(level.pathToLevel)}");
                ImGui.Text($"音频: {Path.GetFileName(level.GetAudioPath())}");
                
                if (floors != null)
                {
                    ImGui.Text($"地板数量: {floors.Count}");
                    ImGui.Text($"音符数量: {noteTimes?.Count ?? 0}");
                }
                
                ImGui.Separator();
                ImGui.TextWrapped("提示: 按 Space 开始，按 R 重新开始，鼠标左键拖动视角");
            }
            
            ImGui.End();
            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
        }
        
        // About window / 关于窗口
        if (_showAboutWindow)
        {
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(ClientSize.X / 2 - 250, ClientSize.Y / 2 - 150), ImGuiCond.Appearing);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(500, 300), ImGuiCond.Appearing);
            
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 10f);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 5f);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new System.Numerics.Vector4(0.1f, 0.1f, 0.15f, 0.95f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new System.Numerics.Vector4(0.2f, 0.3f, 0.5f, 1.0f));
            
            bool beginResult = ImGui.Begin("关于 About SharpFAI Player", ref _showAboutWindow, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize);
            
            if (beginResult)
            {
                // Title / 标题
                ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.3f, 0.8f, 1.0f, 1.0f));
                var titleSize = ImGui.CalcTextSize("SharpFAI Player");
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - titleSize.X) / 2);
                ImGui.Text("SharpFAI Player");
                ImGui.PopStyleColor();
                
                ImGui.Spacing();
                
                // Version / 版本
                ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1.0f));
                var versionText = "Version 1.0.0";
                var versionSize = ImGui.CalcTextSize(versionText);
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - versionSize.X) / 2);
                ImGui.Text(versionText);
                ImGui.PopStyleColor();
                
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                
                // Description / 描述
                ImGui.TextWrapped("A Dance of Fire and Ice (ADOFAI) 关卡播放器");
                ImGui.TextWrapped("使用 C# 和 OpenTK 开发的跨平台播放器");
                
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                
                // Links section / 链接部分
                ImGui.Text("链接 Links:");
                ImGui.Spacing();
                
                // GitHub link / GitHub 链接
                ImGui.BulletText("GitHub:");
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.3f, 0.7f, 1.0f, 1.0f));
                if (ImGui.SmallButton("https://github.com/StArrayJaN/ADOFAI-Player"))
                {
                    OpenUrl("https://github.com/StArrayJaN/ADOFAI-Player");
                }
                ImGui.PopStyleColor();
                
                ImGui.Spacing();
                
                // Bilibili link / Bilibili 链接
                ImGui.BulletText("Bilibili:");
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1.0f, 0.5f, 0.7f, 1.0f));
                if (ImGui.SmallButton("https://space.bilibili.com/425111197"))
                {
                    OpenUrl("https://space.bilibili.com/425111197");
                }
                ImGui.PopStyleColor();
                
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                
                // Credits / 致谢
                ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1.0f));
                ImGui.TextWrapped("使用的库 Libraries: OpenTK, ImGui.NET, LibVLCSharp");
                ImGui.PopStyleColor();
                
                ImGui.Spacing();
                ImGui.Spacing();
                
                // Close button / 关闭按钮
                float buttonWidth = 120f;
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - buttonWidth) / 2);
                if (ImGui.Button("关闭 Close", new System.Numerics.Vector2(buttonWidth, 30)))
                {
                    _showAboutWindow = false;
                }
            }
            
            ImGui.End();
            ImGui.PopStyleColor(2);
            ImGui.PopStyleVar(2);
        }
        
        // Main menu bar / 主菜单栏
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("文件 File"))
            {
                if (ImGui.MenuItem("打开关卡 Open Level", "Ctrl+O"))
                {
                    OpenLevelFile();
                }
                
                ImGui.Separator();
                
                if (ImGui.MenuItem("退出 Exit", "ESC"))
                {
                    Close();
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("视图 View"))
            {
                ImGui.MenuItem("控制面板 Control Panel", null, ref _showControlPanel);
                ImGui.MenuItem("关卡信息 Level Info", null, ref _showLevelInfo);
                ImGui.EndMenu();
            }
            
            if (ImGui.BeginMenu("帮助 Help"))
            {
                if (ImGui.MenuItem("关于 About"))
                {
                    _showAboutWindow = true;
                }
                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }
    }
    
    private void RenderForegroundInfo()
    {
        // Only render if initialized / 仅在初始化后渲染
        if (!initialized || level == null || floors == null || floors.Count == 0)
            return;
        
        var drawList = ImGui.GetForegroundDrawList();
        
        // Position for info display (top-right corner, below menu bar) / 信息显示位置（右上角，菜单栏下方）
        float padding = 20f;
        float lineHeight = 25f;
        float progressBarHeight = 6f;
        float startX = ClientSize.X - 350f;
        float startY = padding + 30f; // Add 30px to avoid menu bar / 增加30像素避开菜单栏
        
        // Background panel / 背景面板
        // Layout: BPM line + Track line + Track progress bar + Time line + Time progress bar
        // Total height: 4 text lines + 2 progress bars (with 5px offset each) + padding
        Vector2 panelMin = new Vector2(startX - 15, startY - 10);
        Vector2 panelMax = new Vector2(ClientSize.X - padding + 10, startY + lineHeight * 4 + progressBarHeight * 2 + 10 + 10); // 4 lines + 2 bars + offsets + bottom padding
        
        // Draw semi-transparent background / 绘制半透明背景
        drawList.AddRectFilled(
            panelMin,
            panelMax,
            ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 0.7f)),
            8.0f
        );
        
        // Draw border / 绘制边框
        drawList.AddRect(
            panelMin,
            panelMax,
            ImGui.GetColorU32(new Vector4(0.3f, 0.5f, 0.8f, 0.8f)),
            8.0f,
            ImDrawFlags.None,
            2.0f
        );
        
        // Colors / 颜色
        uint labelColor = ImGui.GetColorU32(new Vector4(0.7f, 0.7f, 0.7f, 1.0f));
        uint valueColor = ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
        uint highlightColor = ImGui.GetColorU32(new Vector4(0.3f, 0.8f, 1.0f, 1.0f));
        
        float currentY = startY;
        
        // BPM display / BPM 显示
        string bpmLabel = "BPM: ";
        string bpmValue = $"{currentFloor?.bpm:F1}";
        
        drawList.AddText(new Vector2(startX, currentY), labelColor, bpmLabel);
        float bpmLabelWidth = ImGui.CalcTextSize(bpmLabel).X;
        drawList.AddText(new Vector2(startX + bpmLabelWidth, currentY), highlightColor, bpmValue);
        currentY += lineHeight;
        
        // Track count display / 轨道数显示
        string trackLabel = "Track: ";
        string trackValue = $"{currentIndex + 1} / {floors.Count}";
        
        drawList.AddText(new Vector2(startX, currentY), labelColor, trackLabel);
        float trackLabelWidth = ImGui.CalcTextSize(trackLabel).X;
        drawList.AddText(new Vector2(startX + trackLabelWidth, currentY), valueColor, trackValue);
        currentY += lineHeight;
        
        // Progress bar for tracks / 轨道进度条
        float progressBarWidth = 300f;
        float progress = floors.Count > 1 ? (float)currentIndex / (floors.Count - 1) : 0f;
        
        Vector2 progressBarMin = new Vector2(startX, currentY + 5);
        Vector2 progressBarMax = new Vector2(startX + progressBarWidth, currentY + 5 + progressBarHeight);
        
        // Background of progress bar / 进度条背景
        drawList.AddRectFilled(
            progressBarMin,
            progressBarMax,
            ImGui.GetColorU32(new Vector4(0.2f, 0.2f, 0.2f, 0.8f)),
            3.0f
        );
        
        // Progress fill / 进度填充
        if (progress > 0)
        {
            Vector2 progressFillMax = new Vector2(startX + progressBarWidth * progress, currentY + 5 + progressBarHeight);
            drawList.AddRectFilled(
                progressBarMin,
                progressFillMax,
                ImGui.GetColorU32(new Vector4(0.3f, 0.8f, 1.0f, 1.0f)),
                3.0f
            );
        }
        
        currentY += lineHeight;
        
        // Time display / 时间显示
        double totalTime = music?.Duration ?? 0;
        string timeLabel = "Time: ";
        string timeValue = $"{FormatTime(currentTime)} / {FormatTime(totalTime)}";
        
        drawList.AddText(new Vector2(startX, currentY), labelColor, timeLabel);
        float timeLabelWidth = ImGui.CalcTextSize(timeLabel).X;
        drawList.AddText(new Vector2(startX + timeLabelWidth, currentY), valueColor, timeValue);
        currentY += lineHeight;
        
        // Time progress bar / 时间进度条
        float timeProgress = totalTime > 0 ? (float)(currentTime / totalTime) : 0f;
        
        Vector2 timeBarMin = new Vector2(startX, currentY + 5);
        Vector2 timeBarMax = new Vector2(startX + progressBarWidth, currentY + 5 + progressBarHeight);
        
        // Background / 背景
        drawList.AddRectFilled(
            timeBarMin,
            timeBarMax,
            ImGui.GetColorU32(new Vector4(0.2f, 0.2f, 0.2f, 0.8f)),
            3.0f
        );
        
        // Progress fill with gradient / 带渐变的进度填充
        if (timeProgress > 0)
        {
            Vector2 timeFillMax = new Vector2(startX + progressBarWidth * timeProgress, currentY + 5 + progressBarHeight);
            drawList.AddRectFilledMultiColor(
                timeBarMin,
                timeFillMax,
                ImGui.GetColorU32(new Vector4(0.2f, 0.6f, 1.0f, 1.0f)), // Left - blue
                ImGui.GetColorU32(new Vector4(0.6f, 0.2f, 1.0f, 1.0f)), // Right - purple
                ImGui.GetColorU32(new Vector4(0.6f, 0.2f, 1.0f, 1.0f)), // Bottom right
                ImGui.GetColorU32(new Vector4(0.2f, 0.6f, 1.0f, 1.0f))  // Bottom left
            );
        }
    }
    
    private string FormatTime(double seconds)
    {
        int minutes = (int)(seconds / 60);
        int secs = (int)(seconds % 60);
        return $"{minutes}:{secs:D2}";
    }
    
    private void OpenUrl(string url)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            else if (OperatingSystem.IsLinux())
            {
                System.Diagnostics.Process.Start("xdg-open", url);
            }
            else if (OperatingSystem.IsMacOS())
            {
                System.Diagnostics.Process.Start("open", url);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open URL: {ex.Message}");
        }
    }

    public void MoveToNextFloor(Floor next)
    {
        if (currentIndex >= floors.Count) {
            return;
        }
        
        // Mark previous floor as hit (hide it) / 标记前一个地板为已命中（隐藏它）
        if (currentIndex > 0 && currentIndex < playerFloors.Count)
        {
            playerFloors[currentIndex - 1].isHit = true;
        }
        
        currentIndex = next.index;
        if (next.isMidspin) {
            // Hide the midspin floor itself / 隐藏中旋地板本身
            if (next.index < playerFloors.Count)
            {
                playerFloors[next.index].isHit = true;
            }
            currentIndex++;
        }
        currentFloor = floors[currentIndex];
        
        if (!currentFloor.lastFloor.isMidspin)
        {
            (currentPlanet, lastPlanet) = (lastPlanet, currentPlanet);
        }
        
        angle = (currentFloor.lastFloor.angle + 180).Fmod(360);
        
        cameraFromPos = camera2D.Position;
        cameraToPos = currentFloor.position;
        cameraTimer = 0f;
    }

    public void StartPlay()
    {
        if (!initialized || level == null)
            return;
            
        _ = StartPlayAsync();
    }

    private async Task StartPlayAsync()
    {
        if (level == null)
            return;
            
        int offset = level.GetSetting<int>("offset");
        
        // Start both audio tracks simultaneously with precise timing
        // 同时启动两个音轨，使用精确计时
        if (offset > 0)
        {
            // If there's an offset, start music first, then hitsound after delay
            // 如果有偏移，先启动音乐，然后延迟后启动命中音效
            music?.Play();
            await Task.Delay(offset - 1);
            hitSound?.Play();
        }
        else
        {
            // No offset, start both simultaneously
            // 无偏移，同时启动
            hitSound?.Play();
            music?.Play();
        }
        
        isStarted = true;
    }

    public void StopPlay()
    {
        isStarted = false;
        if (initialized)
        {
            ResetPlayer();
        }
    }

    public void PausePlay()
    {
        isStarted = false;
        music?.Pause();
        hitSound?.Pause();
    }

    public void ResumePlay()
    {
        isStarted = true;
        music?.Resume();
        hitSound?.Resume();
    }

    public void ResetPlayer()
    {
        if (!initialized || floors == null || floors.Count == 0)
            return;
            
        currentFloor = floors[0];
        angle = 0;
        isStarted = false;
        currentIndex = 0;
        currentTime = 0;
        music?.Stop();
        hitSound?.Stop();
        
        // Reset all floors to visible / 重置所有地板为可见
        if (playerFloors != null)
        {
            foreach (var floor in playerFloors)
            {
                floor.isHit = false;
            }
        }
    }

    public void DestroyPlayer()
    {
        isStarted = false;
        
        // Dispose audio resources
        music?.Dispose();
        music = null;
        hitSound?.Dispose();
        hitSound = null;
        
        // Dispose OpenGL resources
        if (playerFloors != null)
        {
            foreach (var floor in playerFloors)
            {
                floor?.Dispose();
            }
            playerFloors = null;
        }
        
        renderFloors = null;
        
        // Dispose planets
        redPlanet?.Dispose();
        bluePlanet?.Dispose();
        redPlanet = null;
        bluePlanet = null;
        lastPlanet = null;
        currentPlanet = null;
        
        // Dispose shader
        shader?.Dispose();
        shader = null;
        
        // Clear collections
        floors = null;
        noteTimes = null;
        
        initialized = false;
    }
    
    private void OpenLevelFile()
    {
        string filePath = NativeAPI.OpenFileDialog(new NativeAPI.FileFilter
        {
            Name = "关卡文件",
            Filter = new List<string> { "*.adofai" },
            IncludeAllFiles = true
        });
        
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            LoadLevel(filePath);
        }
    }
    
    private void LoadLevel(string levelPath)
    {
        try
        {
            // Stop current playback
            StopPlay();
            
            // Dispose audio resources
            music?.Dispose();
            music = null;
            hitSound?.Dispose();
            hitSound = null;
            
            // Clear OpenGL resources (must be done on the OpenGL thread)
            if (playerFloors != null)
            {
                foreach (var floor in playerFloors)
                {
                    floor?.Dispose();
                }
                playerFloors = null;
            }
            
            renderFloors = null;
            
            // Dispose planets
            redPlanet?.Dispose();
            bluePlanet?.Dispose();
            redPlanet = null;
            bluePlanet = null;
            lastPlanet = null;
            currentPlanet = null;
            
            // Don't dispose shader here - it will be reused or disposed in DestroyPlayer
            // shader?.Dispose();
            
            // Clear collections
            floors = null;
            noteTimes = null;
            
            // Load new level
            level = new Level(levelPath);
            
            // Reset state
            initialized = false;
            isStarted = false;
            currentIndex = 0;
            currentTime = 0;
            angle = 0;
            state = "正在加载新关卡...";
            
            // Recreate player
            CreatePlayer();
        }
        catch (Exception ex)
        {
            state = $"加载失败: {ex.Message}";
            Console.WriteLine($"Failed to load level: {ex}");
        }
    }
}