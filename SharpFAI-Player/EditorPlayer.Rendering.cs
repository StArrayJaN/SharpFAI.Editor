using System.Numerics;
using ImGuiNET;
using SharpFAI.Framework;
using SharpFAI.Util;

namespace SharpFAI_Player;

/// <summary>
/// EditorPlayer - 渲染功能部分
/// </summary>
public partial class EditorPlayer
{
    private void RenderTracks()
    {
        if (!_initialized || _shader == null || _camera2D == null || _renderFloors == null)
            return;
        
        _shader.Use();
        _camera2D.Render(_shader);
        
        foreach (var floor in _renderFloors)
        {
            if (_camera2D.IsPointVisible(new Vector2(floor.floor.position.X, floor.floor.position.Y)))
            {
                floor.Render(_shader);
            }
        }
        
        // 仅在播放时渲染星球
        if (_isPlaying)
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
            return;
        
        var selectedFloor = _selectedFloors[0];
        var floorWorldPos = new Vector2(selectedFloor.position.X, selectedFloor.position.Y);
        var floorScreenPos = WorldToScreen(floorWorldPos);
        
        // 确保地板在屏幕可见范围内
        if (floorScreenPos.X < 0 || floorScreenPos.X > ClientSize.X || 
            floorScreenPos.Y < 0 || floorScreenPos.Y > ClientSize.Y)
            return;
        
        // 设置按钮窗口位置（在轨道上方）
        var buttonWindowPos = new Vector2(floorScreenPos.X - 130, floorScreenPos.Y - 80);
        ImGui.SetNextWindowPos(buttonWindowPos, ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(260, 70), ImGuiCond.Always);
        
        // 半透明背景样式（不是完全透明，这样才能捕获鼠标）
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.1f, 0.1f, 0.1f, 0.3f));
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.3f, 0.3f, 0.3f, 0.3f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 8f);
        
        // 移除 NoBackground 和 NoFocusOnAppearing，让窗口能捕获鼠标
        var flags = ImGuiWindowFlags.NoTitleBar | 
                    ImGuiWindowFlags.NoResize | 
                    ImGuiWindowFlags.NoMove | 
                    ImGuiWindowFlags.NoScrollbar |
                    ImGuiWindowFlags.NoScrollWithMouse |
                    ImGuiWindowFlags.NoCollapse |
                    ImGuiWindowFlags.NoSavedSettings;
        
        if (ImGui.Begin("##TrackButtons", flags))
        {
            // 按钮样式
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.25f, 0.25f, 0.3f, 0.9f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.35f, 0.35f, 0.4f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.45f, 0.45f, 0.5f, 1.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 5f);
            
            var buttonSize = new Vector2(58, 55);
            
            if (ImGui.Button("编辑\nEdit", buttonSize))
            {
                _statusMessage = "编辑地板";
            }
            
            ImGui.SameLine();
            if (ImGui.Button("删除\nDel", buttonSize))
            {
                _statusMessage = "删除地板";
            }
            
            ImGui.SameLine();
            if (ImGui.Button("复制\nCopy", buttonSize))
            {
                _statusMessage = "复制地板";
            }
            
            ImGui.SameLine();
            if (ImGui.Button("属性\nProp", buttonSize))
            {
                _statusMessage = "地板属性";
            }
            
            ImGui.PopStyleVar();
            ImGui.PopStyleColor(3);
        }
        
        ImGui.End();
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(2);
    }
    
    private void RenderShiftKeyHints()
    {
        if (_camera2D == null || _shader == null)
            return;
        
        // 在屏幕中央显示按键布局提示
        var centerX = ClientSize.X / 2f;
        var centerY = ClientSize.Y / 2f;
        
        ImGui.SetNextWindowPos(new Vector2(centerX - 250, centerY - 200), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(500, 400), ImGuiCond.Always);
        
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 15f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(20, 20));
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.05f, 0.05f, 0.08f, 0.85f));
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.3f, 0.5f, 0.8f, 0.5f));
        
        var flags = ImGuiWindowFlags.NoTitleBar | 
                    ImGuiWindowFlags.NoResize | 
                    ImGuiWindowFlags.NoMove | 
                    ImGuiWindowFlags.NoCollapse |
                    ImGuiWindowFlags.NoSavedSettings;
        
        if (ImGui.Begin("##ShiftKeyHints", flags))
        {
            // 标题
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 1f, 0.3f, 1f));
            var titleText = "⇧ SHIFT 模式 - 按键布局";
            var titleSize = ImGui.CalcTextSize(titleText);
            ImGui.SetCursorPosX((500 - titleSize.X) / 2);
            ImGui.Text(titleText);
            ImGui.PopStyleColor();
            
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            
            // 第一组按键 (QWEASDZXC)
            ImGui.Text("第一组按键布局:");
            ImGui.Spacing();
            
            ImGui.Indent(50);
            ImGui.Text("    W");
            ImGui.Text("Q       E");
            ImGui.Text("A   S   D");
            ImGui.Text("Z   X   C");
            ImGui.Unindent(50);
            
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            
            // 第二组按键布局 (按住 Shift)
            ImGui.Text("第二组按键布局 (按住 Shift):");
            ImGui.Spacing();
            
            ImGui.Indent(50);
            ImGui.Text("  T   Y");
            ImGui.Text("H       J");
            ImGui.Text("N   M");
            ImGui.Text("  V   B");
            ImGui.Unindent(50);
            
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            
            ImGui.TextWrapped("💡 提示: 这些按键可用于快速放置不同角度的轨道");
        }
        
        ImGui.End();
        ImGui.PopStyleColor(2);
        ImGui.PopStyleVar(2);
    }
}
