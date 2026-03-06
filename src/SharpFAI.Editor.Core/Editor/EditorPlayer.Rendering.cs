using System.Numerics;
using ImGuiNET;
using SharpFAI.Framework;
using SharpFAI.Util;

namespace SharpFAI.Editor.Core.Editor;

/// <summary>
/// EditorPlayer - 渲染功能部分
/// </summary>
public partial class EditorPlayer
{
    private void RenderTracks()
    {
        if (!_initialized || _shader == null || _camera2D == null || _playerFloors == null)
            return;
        
        _shader.Use();
        _camera2D.Render(_shader);
        
        // 更新渲染顺序缓存（仅在需要时�?
        if (_needRenderOrderUpdate || _cachedRenderOrder.Length != _playerFloors.Count)
        {
            _cachedRenderOrder = Enumerable.Range(0, _playerFloors.Count)
                .OrderBy(i => _playerFloors[i].floor.renderOrder)
                .ToArray();
            _needRenderOrderUpdate = false;
        }
        
        // 根据缓存的顺序渲染（renderOrder 越小越先渲染，在底层�?
        foreach (int i in _cachedRenderOrder)
        {
            var floor = _playerFloors[i];
            if (_camera2D.IsPointVisible(new Vector2(floor.floor.position.X, floor.floor.position.Y)))
            {
                floor.Render(_shader);
            }
        }
        
        if (_showPlanets)
        {
            RenderPlanets();
        }
    }
    
    private void RenderPlanets()
    {
        if (_shader == null || _camera2D == null || _currentFloor == null)
            return;
        
        if (_currentPlanet == null || _lastPlanet == null)
            return;
        
        // 更新星球位置
        _currentPlanet.Position = _currentFloor.position;
        
        Vector2 offset = new Vector2(
            FloatMath.Cos((float)_angle, true) * Floor.length * 2,
            FloatMath.Sin((float)_angle, true) * Floor.length * 2
        );
        _lastPlanet.Position = offset + _currentFloor.position;
        
        // 渲染星球
        _bluePlanet?.Render(_shader, _camera2D);
        _redPlanet?.Render(_shader, _camera2D);
    }
    
    private void RenderTrackButtons()
    {
        // 仅在选中一个地板时显示按钮
        if (_selectedFloors.Count != 1 || _camera2D == null)
        {
            _isButtonWindowHovered = false;
            return;
        }
        
        var selectedFloor = _selectedFloors[0];
        var floorWorldPos = new Vector2(selectedFloor.position.X, selectedFloor.position.Y);
        var floorScreenPos = WorldToScreen(floorWorldPos);
        
        // 确保地板在屏幕可见范围内
        if (floorScreenPos.X < 0 || floorScreenPos.X > ClientSize.X || 
            floorScreenPos.Y < 0 || floorScreenPos.Y > ClientSize.Y)
        {
            _isButtonWindowHovered = false;
            return;
        }
        
        // 设置按钮窗口位置（在轨道上方�?
        var buttonWindowPos = new Vector2(floorScreenPos.X - 175/2f, floorScreenPos.Y + 1 - 175/2f);
        ImGui.SetNextWindowPos(buttonWindowPos, ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(175, 175), ImGuiCond.Always);
        
        // 关键：使�?NoInputs 让窗口完全不捕获输入，然后手动处理按�?
        var flags = ImGuiWindowFlags.NoTitleBar | 
                    ImGuiWindowFlags.NoResize | 
                    ImGuiWindowFlags.NoMove | 
                    ImGuiWindowFlags.NoScrollbar |
                    ImGuiWindowFlags.NoScrollWithMouse |
                    ImGuiWindowFlags.NoBackground |
                    ImGuiWindowFlags.NoNav |
                    ImGuiWindowFlags.NoMouseInputs | // 窗口不捕获鼠标输�?
                    ImGuiWindowFlags.NoCollapse;
        
        if (ImGui.Begin("##TrackButtons", flags))
        {
            var io = ImGui.GetIO();
            var mousePos = ImGui.GetMousePos();
            var windowPos = ImGui.GetWindowPos();
            var windowSize = ImGui.GetWindowSize();
            
            // 检测鼠标是否在窗口区域�?
            bool mouseInWindow = mousePos.X >= windowPos.X && mousePos.X <= windowPos.X + windowSize.X &&
                                 mousePos.Y >= windowPos.Y && mousePos.Y <= windowPos.Y + windowSize.Y;
            
            // 按钮样式
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.25f, 0.25f, 0.3f, 0.9f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.35f, 0.35f, 0.4f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.45f, 0.45f, 0.5f, 1.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 5f);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(5f, 5f));

            var buttonSize = new Vector2(50, 50);
            var spacing = 5f;
            var cursorStart = ImGui.GetCursorScreenPos();
            
            bool anyButtonHovered = false;
            
            // 手动检测按钮点�?
            bool CheckButton(string label, Vector2 pos)
            {
                var buttonMin = pos;
                var buttonMax = new Vector2(pos.X + buttonSize.X, pos.Y + buttonSize.Y);
                bool hovered = mousePos.X >= buttonMin.X && mousePos.X <= buttonMax.X &&
                               mousePos.Y >= buttonMin.Y && mousePos.Y <= buttonMax.Y;
                
                if (hovered) anyButtonHovered = true;
                
                ImGui.SetCursorScreenPos(pos);
                
                bool clicked = false;
                if (hovered)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.35f, 0.35f, 0.4f, 1.0f));
                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        clicked = true;
                    }
                    ImGui.Button(label, buttonSize);
                    ImGui.PopStyleColor();
                }
                else
                {
                    ImGui.Button(label, buttonSize);
                }
                
                return clicked;
            }
            
            /// QWE
            /// A D
            /// ZXC
            float row1Y = cursorStart.Y;
            float row2Y = row1Y + buttonSize.Y + spacing;
            float row3Y = row2Y + buttonSize.Y + spacing;
            
            float col1X = cursorStart.X;
            float col2X = col1X + buttonSize.X + spacing;
            float col3X = col2X + buttonSize.X + spacing;
            
            if (CheckButton("Q", new Vector2(col1X, row1Y))) { }
            if (CheckButton("W", new Vector2(col2X, row1Y))) { }
            if (CheckButton("E", new Vector2(col3X, row1Y))) { }
            
            if (CheckButton("A", new Vector2(col1X, row2Y))) { }
            if (CheckButton("D", new Vector2(col3X, row2Y))) { }
            
            if (CheckButton("Z", new Vector2(col1X, row3Y))) { }
            if (CheckButton("X", new Vector2(col2X, row3Y))) { }
            if (CheckButton("C", new Vector2(col3X, row3Y))) { }
            
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor(3);
            
            // 设置标志：鼠标在窗口内或在按钮上
            _isButtonWindowHovered = mouseInWindow || anyButtonHovered;
        }
        
        ImGui.End();
    }
}

