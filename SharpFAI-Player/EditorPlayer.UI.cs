using System.Numerics;
using ImGuiNET;

namespace SharpFAI_Player;

/// <summary>
/// EditorPlayer - UI 渲染部分
/// </summary>
public partial class EditorPlayer
{
    private void RenderEditorUi()
    {
        RenderMenuBar();
        RenderMainLayout();
        
        if (_showAboutWindow)
        {
            RenderAboutWindow();
        }
        
        // 渲染轨道按钮（仅在选中一个地板时）
        if (_selectedFloors.Count == 1)
        {
            RenderTrackButtons();
        }
        
        RenderStatusBar();
        
        Title = $"SharpFAI Editor - {(_level != null ? Path.GetFileName(_levelPath) : "未加载关卡")}";
    }

    private void RenderMainLayout()
    {
        const float menuBarHeight = 25f;
        const float statusBarHeight = 25f;
        const float tabButtonWidth = 40f;
        const float eventSetTabHeight = 35f;
        const float topPadding = 20f;
        const float bottomPadding = 20f;
        
        var workAreaHeight = ClientSize.Y - menuBarHeight - statusBarHeight;
        
        // 计算顶部面板高度：如果底部面板折叠，则顶部面板占据更多空间
        // Calculate top panel height: if bottom panel is collapsed, top panel takes more space
        float topPanelHeight;
        float eventSetTabStartY;
        
        if (_bottomPanelCollapsed)
        {
            // 折叠时：顶部面板占据几乎所有空间，标签栏紧贴底部
            // When collapsed: top panel takes almost all space, tabs stick to bottom
            topPanelHeight = workAreaHeight - eventSetTabHeight - topPadding - bottomPadding;
            eventSetTabStartY = ClientSize.Y - statusBarHeight - eventSetTabHeight;
        }
        else
        {
            // 展开时：正常布局
            // When expanded: normal layout
            topPanelHeight = workAreaHeight - eventSetTabHeight - _bottomPanelHeight - topPadding - bottomPadding;
            eventSetTabStartY = menuBarHeight + topPadding + topPanelHeight + bottomPadding;
        }
        
        var topPanelStartY = menuBarHeight + topPadding;
        
        // 左侧：设置面板 + 右侧子标签
        if (!_leftPanelCollapsed)
        {
            RenderSettingsPanelWithTabs(topPanelStartY, topPanelHeight, tabButtonWidth);
        }
        else
        {
            RenderCollapsedLeftPanel(topPanelStartY, topPanelHeight, tabButtonWidth);
        }
        
        // 右侧：左侧子标签 + 事件信息面板
        if (!_rightPanelCollapsed)
        {
            RenderEventInfoPanelWithTabs(topPanelStartY, topPanelHeight, tabButtonWidth);
        }
        else
        {
            RenderCollapsedRightPanel(topPanelStartY, topPanelHeight, tabButtonWidth);
        }
        
        // 底部：事件集子标签 + 事件集面板（标签始终显示）
        // Bottom: Event set tabs + panel (tabs always visible)
        RenderEventSetPanelWithTabs(eventSetTabStartY, eventSetTabHeight, _bottomPanelHeight);
    }

    private void RenderSettingsPanelWithTabs(float startY, float height, float tabWidth)
    {
        const float startX = 0f;
        
        // 设置面板
        ImGui.SetNextWindowPos(new Vector2(startX, startY), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(_leftPanelWidth, height), ImGuiCond.Always);
        ImGui.SetNextWindowSizeConstraints(new Vector2(200, height), new Vector2(ClientSize.X * 0.5f, height));
        
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.12f, 0.12f, 0.15f, 0.95f));
        
        var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse;
        
        if (ImGui.Begin("设置面板", flags))
        {
            ImGui.Text("设置面板 Settings Panel");
            ImGui.Separator();
            ImGui.Spacing();
            
            // 音乐文件设置 - 始终可用
            ImGui.Text("音乐:");
            ImGui.SameLine();
            
            string musicFileName = "未选择";
            
            if (!string.IsNullOrEmpty(_musicFilePath))
            {
                musicFileName = Path.GetFileName(_musicFilePath);
            }
            else if (_level != null && _level.HasSetting("songFilename"))
            {
                string? songFilename = _level.GetSetting<string>("songFilename");
                if (!string.IsNullOrEmpty(songFilename))
                {
                    musicFileName = songFilename;
                }
            }
            
            ImGui.PushItemWidth(-80); // 留出按钮空间
            ImGui.InputText("##MusicPath", ref musicFileName, 256, ImGuiInputTextFlags.ReadOnly);
            ImGui.PopItemWidth();
            
            ImGui.SameLine();
            if (ImGui.Button("选择文件", new Vector2(70, 0)))
            {
                var filePath = Native.NativeAPI.OpenFileDialog(new Native.NativeAPI.FileFilter
                {
                    Name = "音频文件",
                    Filter = new List<string> { "*.mp3", "*.wav", "*.ogg", "*.flac" },
                    IncludeAllFiles = true
                });
                
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    _musicFilePath = filePath;
                    _statusMessage = $"已选择音乐: {Path.GetFileName(filePath)}";
                }
            }
            
            ImGui.Spacing();
            
            // BPM 设置 - 始终可用
            ImGui.Text("BPM:");
            ImGui.SameLine();
            ImGui.PushItemWidth(-1);
            
            if (ImGui.InputFloat("##BPM", ref _bpm, 0.1f, 1.0f, "%.2f"))
            {
                if (_bpm < 1f) _bpm = 1f;
                if (_bpm > 999f) _bpm = 999f;
                _statusMessage = $"BPM 设置为 {_bpm:F2}";
            }
            
            ImGui.PopItemWidth();
            
            ImGui.Spacing();
            
            // 音高设置 - 始终可用，整数输入
            ImGui.Text("音高:");
            ImGui.SameLine();
            ImGui.PushItemWidth(-1);
            int pitchInt = (int)_pitch;
            if (ImGui.InputInt("##Pitch", ref pitchInt, 1, 10))
            {
                if (pitchInt < 50) pitchInt = 50;
                if (pitchInt > 200) pitchInt = 200;
                _pitch = pitchInt;
                _statusMessage = $"音高设置为 {pitchInt}%";
            }
            ImGui.PopItemWidth();
            
            // 在输入框旁边显示百分号提示
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip($"{pitchInt}%");
            }
            
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            
            // 关卡信息 - 仅在加载关卡后显示
            if (_level != null && _levelPath != null)
            {
                ImGui.Text("关卡信息:");
                ImGui.Text($"文件: {Path.GetFileName(_levelPath)}");
                
                if (_floors != null)
                {
                    ImGui.Text($"地板数量: {_floors.Count}");
                }
            }
            else
            {
                ImGui.TextWrapped("提示: 可以先设置音乐和参数，然后从菜单打开关卡文件。");
            }
            
            _leftPanelWidth = ImGui.GetWindowWidth();
        }
        
        ImGui.End();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);
        
        // 右侧紧贴的单个标签按钮
        RenderSingleVerticalTab(_leftPanelWidth, startY, tabWidth, height, 
            "⚙", "设置", ref _lastLeftTabClickTime, ref _leftPanelCollapsed);
    }

    private void RenderEventInfoPanelWithTabs(float startY, float height, float tabWidth)
    {
        var startX = ClientSize.X - _rightPanelWidth;
        
        // 左侧紧贴的单个标签按钮
        RenderSingleVerticalTab(startX - tabWidth, startY, tabWidth, height,
            "📝", "事件", ref _lastRightTabClickTime, ref _rightPanelCollapsed);
        
        // 事件信息面板
        ImGui.SetNextWindowPos(new Vector2(startX, startY), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(_rightPanelWidth, height), ImGuiCond.Always);
        ImGui.SetNextWindowSizeConstraints(new Vector2(200, height), new Vector2(ClientSize.X * 0.5f, height));
        
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.12f, 0.12f, 0.15f, 0.95f));
        
        var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse;
        
        if (ImGui.Begin("事件信息", flags))
        {
            ImGui.Text("事件信息 Event Info");
            ImGui.Separator();
            ImGui.Spacing();
            
            if (_selectedFloors.Count > 0)
            {
                var selectedFloor = _selectedFloors[0];
                var selectedIndex = _selectedFloorIndices[0];
                
                ImGui.Text($"地板 Floor #{selectedIndex} (共选中 {_selectedFloors.Count} 个)");
                ImGui.Spacing();
                
                ImGui.Text("📝 地板信息");
                ImGui.Text($"事件数量: {selectedFloor.events.Count}");
                ImGui.Text($"BPM: {selectedFloor.bpm:F2}");
                ImGui.Text($"角度: {selectedFloor.angle:F2}°");
                ImGui.Text($"位置: ({selectedFloor.position.X:F2}, {selectedFloor.position.Y:F2})");
                ImGui.Text($"旋转: {(selectedFloor.isCW ? "顺时针" : "逆时针")}");
                
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                
                if (selectedFloor.events.Count > 0)
                {
                    ImGui.Text("事件列表:");
                    if (ImGui.BeginChild("EventsChild", new Vector2(0, 0)))
                    {
                        foreach (var evt in selectedFloor.events)
                        {
                            ImGui.BulletText($"{evt.EventType} (Floor: {evt.Floor})");
                        }
                    }
                    ImGui.EndChild();
                }
            }
            else
            {
                ImGui.TextWrapped("未选中地板。请从地板列表中选择。");
            }
            
            _rightPanelWidth = ImGui.GetWindowWidth();
        }
        
        ImGui.End();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);
    }

    private void RenderEventSetPanelWithTabs(float startY, float tabHeight, float panelHeight)
    {
        const float startX = 0f;
        var width = ClientSize.X;
        
        // 顶部水平单个标签按钮 - 始终显示
        // Top horizontal single tab button - always visible
        ImGui.SetNextWindowPos(new Vector2(startX, startY), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(width, tabHeight), ImGuiCond.Always);
        
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.08f, 0.08f, 0.1f, 0.95f));
        
        if (ImGui.Begin("事件集标签", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.Text("事件集 ↓");
            ImGui.SameLine();
            
            if (ImGui.Button("🎯 事件集", new Vector2(80, 25)))
            {
                HandleSingleTabDoubleClick(ref _lastBottomTabClickTime, ref _bottomPanelCollapsed);
            }
            
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("事件集 (双击折叠)");
            }
        }
        
        ImGui.End();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);
        
        // 事件集面板 - 仅在未折叠时显示
        // Event set panel - only show when not collapsed
        if (!_bottomPanelCollapsed)
        {
            ImGui.SetNextWindowPos(new Vector2(startX, startY + tabHeight), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(width, _bottomPanelHeight), ImGuiCond.Always);
            ImGui.SetNextWindowSizeConstraints(new Vector2(width, 100), new Vector2(width, ClientSize.Y * 0.5f));
            
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.1f, 0.1f, 0.12f, 0.95f));
            
            var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse;
            
            if (ImGui.Begin("事件集面板", flags))
            {
                ImGui.Text("事件集面板 Event Set Panel");
                ImGui.Separator();
                ImGui.Spacing();
                
                if (_floors != null)
                {
                    ImGui.Text("显示事件集内容");
                    ImGui.Spacing();
                    
                    ImGui.Text($"总地板数: {_floors.Count}");
                    ImGui.Text($"当前选中: {(_selectedFloorIndices.Count > 0 ? string.Join(", ", _selectedFloorIndices.Select(i => i + 1)) : "无")}");
                }
                else
                {
                    ImGui.TextWrapped("请先加载关卡文件");
                }
                
                _bottomPanelHeight = ImGui.GetWindowHeight();
            }
            
            ImGui.End();
            ImGui.PopStyleColor();
            ImGui.PopStyleVar(2);
        }
    }

    private void RenderCollapsedLeftPanel(float startY, float height, float tabWidth)
    {
        RenderSingleVerticalTab(0, startY, tabWidth, height, 
            "⚙", "设置", ref _lastLeftTabClickTime, ref _leftPanelCollapsed);
    }

    private void RenderCollapsedRightPanel(float startY, float height, float tabWidth)
    {
        RenderSingleVerticalTab(ClientSize.X - tabWidth, startY, tabWidth, height,
            "📝", "事件", ref _lastRightTabClickTime, ref _rightPanelCollapsed);
    }

    private void RenderSingleVerticalTab(float x, float y, float width, float height, 
        string icon, string tooltipText, ref double lastClickTime, ref bool panelCollapsed)
    {
        ImGui.SetNextWindowPos(new Vector2(x, y));
        ImGui.SetNextWindowSize(new Vector2(width, height));
        
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(2, 5));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.08f, 0.08f, 0.1f, 0.95f));
        
        if (ImGui.Begin($"##SingleTab{tooltipText}", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar))
        {
            // 按钮居上，正方形尺寸
            // Button at top, square size
            float buttonSize = width - 4; // 正方形边长等于宽度
            
            if (ImGui.Button($"{icon}##{tooltipText}", new Vector2(buttonSize, buttonSize)))
            {
                HandleSingleTabDoubleClick(ref lastClickTime, ref panelCollapsed);
            }
            
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip($"{tooltipText} (双击折叠)");
            }
        }
        
        ImGui.End();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);
    }
    
    private void HandleSingleTabDoubleClick(ref double lastClickTime, ref bool panelCollapsed)
    {
        var currentTime = UpdateTime;
        
        if ((currentTime - lastClickTime) < DoubleClickTime)
        {
            // 双击，切换折叠状态
            // Double click, toggle collapsed state
            panelCollapsed = !panelCollapsed;
            lastClickTime = 0; // 重置时间避免三击 / Reset to avoid triple click
        }
        else
        {
            // 单击，只记录时间，不做任何操作
            // Single click, just record time, no action
            lastClickTime = currentTime;
        }
    }

    private void RenderMenuBar()
    {
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("文件 File"))
            {
                if (ImGui.MenuItem("打开关卡 Open Level", "Ctrl+O"))
                {
                    OpenLevelFile();
                }
                
                ImGui.Separator();
                
                bool hasLevel = _level != null;
                
                if (ImGui.MenuItem("保存 Save", "Ctrl+S", false, hasLevel))
                {
                    SaveLevel();
                }
                
                if (ImGui.MenuItem("另存为 Save As...", "Ctrl+Shift+S", false, hasLevel))
                {
                    SaveLevelAs();
                }
                
                ImGui.Separator();
                
                if (ImGui.MenuItem("退出 Exit", "ESC"))
                {
                    Close();
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("播放 Playback"))
            {
                if (ImGui.MenuItem(_isPlaying ? "暂停 Pause" : "播放 Play", "Space"))
                {
                    if (_isPlaying)
                        PausePlay();
                    else
                        StartPlay();
                }
                
                if (ImGui.MenuItem("停止 Stop"))
                {
                    StopPlay();
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("工具 Tools"))
            {
                if (ImGui.MenuItem("刷新 Refresh", "F5"))
                {
                    if (_levelPath != null)
                    {
                        LoadLevel(_levelPath);
                    }
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("帮助 Help"))
            {
                if (ImGui.MenuItem("关于编辑器 About Editor"))
                {
                    _showAboutWindow = true;
                }
                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }
    }

    private void RenderAboutWindow()
    {
        ImGui.SetNextWindowPos(new Vector2(ClientSize.X / 2f - 250, ClientSize.Y / 2f - 150), ImGuiCond.Appearing);
        ImGui.SetNextWindowSize(new Vector2(500, 300), ImGuiCond.Appearing);
        
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 10f);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.1f, 0.1f, 0.15f, 0.95f));
        
        if (ImGui.Begin("关于 About SharpFAI Editor", ref _showAboutWindow, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.3f, 0.8f, 1.0f, 1.0f));
            var titleSize = ImGui.CalcTextSize("SharpFAI Editor");
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - titleSize.X) / 2);
            ImGui.Text("SharpFAI Editor");
            ImGui.PopStyleColor();
            
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            
            ImGui.TextWrapped("A Dance of Fire and Ice (ADOFAI) 关卡编辑器");
            ImGui.TextWrapped("使用 C# 和 OpenTK + ImGui 开发");
            
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            
            ImGui.Text("功能 Features:");
            ImGui.BulletText("多面板布局");
            ImGui.BulletText("子标签切换（双击折叠）");
            ImGui.BulletText("事件集管理");
            ImGui.BulletText("播放控制");
            ImGui.BulletText("摄像机拖动和缩放");
            
            ImGui.Spacing();
            ImGui.Spacing();
            
            const float buttonWidth = 120f;
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - buttonWidth) / 2);
            if (ImGui.Button("关闭 Close", new Vector2(buttonWidth, 30)))
            {
                _showAboutWindow = false;
            }
        }
        
        ImGui.End();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar();
    }

    private void RenderStatusBar()
    {
        const float statusBarHeight = 25f;
        ImGui.SetNextWindowPos(new Vector2(0, ClientSize.Y - statusBarHeight));
        ImGui.SetNextWindowSize(new Vector2(ClientSize.X, statusBarHeight));
        
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 5));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.15f, 0.15f, 0.18f, 1.0f));
        
        if (ImGui.Begin("StatusBar", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.Text(_statusMessage);
            
            ImGui.SameLine(ClientSize.X - 450);
            if (_camera2D != null)
            {
                ImGui.Text($"缩放: {_camera2D.Zoom:F1}x");
                ImGui.SameLine();
            }
            
            ImGui.SameLine(ClientSize.X - 300);
            if (_isPlaying)
            {
                ImGui.Text($"▶ {_currentTime:F2}s");
                ImGui.SameLine();
            }
            
            ImGui.SameLine(ClientSize.X - 200);
            ImGui.Text($"FPS: {1.0 / UpdateTime:F0}");
            
            if (_isShiftPressed)
            {
                ImGui.SameLine(ClientSize.X - 120);
                ImGui.TextColored(new Vector4(1, 1, 0, 1), "⇧ Shift");
            }
        }
        
        ImGui.End();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);
    }
    
    private void RenderKeyboardHints()
    {
        const float hintSize = 80f;
        const float spacing = 10f;
        var centerX = ClientSize.X / 2f;
        var centerY = ClientSize.Y / 2f;
        
        ImGui.SetNextWindowPos(new Vector2(centerX - 200, centerY - 150), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(400, 300), ImGuiCond.Always);
        
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 10f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(20, 20));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.1f, 0.1f, 0.15f, 0.85f));
        
        if (ImGui.Begin("按键提示 Keyboard Hints", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 0, 1));
            ImGui.Text("⇧ Shift 模式");
            ImGui.PopStyleColor();
            
            ImGui.Separator();
            ImGui.Spacing();
            
            ImGui.Text("按键布局:");
            ImGui.Spacing();
            
            // 显示不同的按键布局（根据图片）
            ImGui.Text("第一组: Q W E / A S D / Z X C");
            ImGui.Text("第二组: T Y / H J / N M / V B");
            
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            
            ImGui.TextWrapped("提示: 按住 Shift 可以访问更多按键选项");
        }
        
        ImGui.End();
        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);
    }
}
