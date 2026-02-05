using System.Numerics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpFAI.Framework;
using SharpFAI.Util;

namespace SharpFAI_Player;

/// <summary>
/// EditorPlayer - 编辑功能部分
/// 参考 ADOFAI-Editor/GLWindow.xaml.cs 的逻辑
/// </summary>
public partial class EditorPlayer
{
    #region Mouse State Fields
    private Vector2 _lastMousePos;
    private bool _isDragging;
    private bool _wasMouseMoving; // 检测是否是拖动还是点击
    private int _anchorFloorIndex = -1; // 范围选择的锚点索引（第一次选择的轨道）
    #endregion
    
    private void HandleInput()
    {
        if (_camera2D == null)
            return;
        
        var currentMousePos = new Vector2(MouseState.X, MouseState.Y);
        
        // 处理鼠标拖动（移动摄像机）
        HandleCameraDrag(currentMousePos);
        
        // 处理滚轮缩放
        HandleMouseWheelZoom();
        
        // 更新鼠标位置
        _lastMousePos = currentMousePos;
    }
    
    /// <summary>
    /// 处理摄像机拖动
    /// 参考 GLWindow.xaml.cs 的 OnMouseMove 方法
    /// </summary>
    private void HandleCameraDrag(Vector2 currentMousePos)
    {
        // 左键或中键或右键按下时拖动摄像机
        bool isAnyButtonDown = MouseState.IsButtonDown(MouseButton.Left) ||
                               MouseState.IsButtonDown(MouseButton.Middle) ||
                               MouseState.IsButtonDown(MouseButton.Right);
        
        if (isAnyButtonDown)
        {
            if (!_isDragging)
            {
                _isDragging = true;
                _lastMousePos = currentMousePos;
                _wasMouseMoving = false;
                return;
            }
            
            // 计算鼠标移动的像素差
            float deltaX = currentMousePos.X - _lastMousePos.X;
            float deltaY = currentMousePos.Y - _lastMousePos.Y;
            
            // 如果鼠标移动超过阈值，标记为拖动
            if (Math.Abs(deltaX) > 2 || Math.Abs(deltaY) > 2)
            {
                _wasMouseMoving = true;
            }
            
            // 将像素差转换为世界坐标差
            // 参考 GLWindow.xaml.cs 的计算方式
            float worldWidth = _camera2D.ViewportWidth * _camera2D.Zoom;
            float worldHeight = _camera2D.ViewportHeight * _camera2D.Zoom;
            float pixelToWorldX = worldWidth / _camera2D.ViewportWidth;
            float pixelToWorldY = worldHeight / _camera2D.ViewportHeight;
            
            // 移动摄像机（注意 Y 轴方向相反）
            _camera2D.Position -= new Vector2(deltaX * pixelToWorldX, -deltaY * pixelToWorldY);
        }
        else
        {
            // 鼠标释放时，如果没有移动，则处理点击
            if (_isDragging && !_wasMouseMoving)
            {
                // 检查鼠标是否在按钮窗口上，如果是则不处理地板点击
                if (!_isButtonWindowHovered)
                {
                    HandleFloorClick(currentMousePos);
                }
            }
            
            _isDragging = false;
            _wasMouseMoving = false;
        }
    }
    
    /// <summary>
    /// 处理鼠标滚轮缩放
    /// 参考 GLWindow.xaml.cs 的 OnMouseWheel 方法
    /// </summary>
    private void HandleMouseWheelZoom()
    {
        if (MouseState.ScrollDelta.Y != 0)
        {
            // 计算缩放因子
            float zoomFactor = MouseState.ScrollDelta.Y < 0 ? 1.25f : 0.8f;
            
            // 获取鼠标在屏幕上的位置
            Vector2 mouseScreenPos = new Vector2(MouseState.X, MouseState.Y);
            
            // 计算缩放前鼠标指向的世界坐标
            Vector2 mouseWorldPosBefore = ScreenToWorld(mouseScreenPos);
            
            // 应用缩放
            float newZoom = _camera2D.Zoom * zoomFactor;
            newZoom = Math.Clamp(newZoom, 0.5f, 20f);
            _camera2D.Zoom = newZoom;
            
            // 计算缩放后鼠标指向的世界坐标
            Vector2 mouseWorldPosAfter = ScreenToWorld(mouseScreenPos);
            
            // 调整摄像机位置，使鼠标指向的世界位置保持不变
            _camera2D.Position += mouseWorldPosBefore - mouseWorldPosAfter;
        }
    }
    
    /// <summary>
    /// 屏幕坐标转世界坐标
    /// 参考 GLWindow.xaml.cs 的 ScreenToWorldPosition 方法
    /// </summary>
    private Vector2 ScreenToWorld(Vector2 screenPos)
    {
        if (_camera2D == null)
            return screenPos;
        
        // 归一化屏幕坐标到 [-1, 1]
        float normalizedX = (screenPos.X / _camera2D.ViewportWidth) * 2.0f - 1.0f;
        float normalizedY = 1.0f - (screenPos.Y / _camera2D.ViewportHeight) * 2.0f;
        
        // 计算世界坐标的宽度和高度
        float worldWidth = _camera2D.ViewportWidth * _camera2D.Zoom;
        float worldHeight = _camera2D.ViewportHeight * _camera2D.Zoom;
        
        // 转换为世界坐标
        float worldX = normalizedX * worldWidth * 0.5f + _camera2D.Position.X;
        float worldY = normalizedY * worldHeight * 0.5f + _camera2D.Position.Y;
        
        return new Vector2(worldX, worldY);
    }
    
    /// <summary>
    /// 世界坐标转屏幕坐标
    /// </summary>
    private Vector2 WorldToScreen(Vector2 worldPos)
    {
        if (_camera2D == null)
            return worldPos;
        
        return _camera2D.WorldToScreen(worldPos);
    }
    
    /// <summary>
    /// 处理地板点击
    /// 参考 GLWindow.xaml.cs 的 OnMouseDown 方法
    /// </summary>
    private void HandleFloorClick(Vector2 screenPos)
    {
        if (_floors == null || _floors.Count == 0)
            return;
        
        // 转换为世界坐标
        Vector2 worldPos = ScreenToWorld(screenPos);
        
        // 查找点击的地板
        Floor? clickedFloor = GetFloorAtPosition(worldPos);
        
        if (clickedFloor != null)
        {
            int clickedIndex = _floors.IndexOf(clickedFloor);
            
            if (_isShiftPressed && _anchorFloorIndex >= 0)
            {
                // Shift + 点击：范围选择（保持锚点不变）
                HandleRangeSelection(clickedIndex);
            }
            else
            {
                // 普通点击：单选（设置新的锚点）
                ClearSelection();
                _anchorFloorIndex = clickedIndex;
                _selectedFloorIndices.Add(clickedIndex);
                _selectedFloors.Add(clickedFloor);
                UpdateFloorSelection(clickedIndex, true);
                _statusMessage = $"已选择地板 {clickedIndex}";
            }
        }
        else
        {
            // 点击空白处，清除选择（仅在非 Shift 模式）
            if (!_isShiftPressed)
            {
                ClearSelection();
                _anchorFloorIndex = -1;
                _statusMessage = "已清除选择";
            }
        }
    }
    
    /// <summary>
    /// 处理范围选择（Shift + 点击）
    /// 保持第一次选择的轨道作为锚点
    /// </summary>
    private void HandleRangeSelection(int clickedIndex)
    {
        if (_anchorFloorIndex < 0)
        {
            // 没有锚点，直接选中并设置为锚点
            _anchorFloorIndex = clickedIndex;
            _selectedFloorIndices.Add(clickedIndex);
            _selectedFloors.Add(_floors![clickedIndex]);
            UpdateFloorSelection(clickedIndex, true);
            _statusMessage = $"已选择地板 {clickedIndex}";
            return;
        }
        
        // 清除当前选择（但保持锚点）
        ClearSelection();
        
        // 从锚点到点击位置选择范围内的所有地板
        int startIndex = Math.Min(_anchorFloorIndex, clickedIndex);
        int endIndex = Math.Max(_anchorFloorIndex, clickedIndex);
        
        for (int i = startIndex; i <= endIndex; i++)
        {
            if (i >= 0 && i < _floors!.Count)
            {
                _selectedFloorIndices.Add(i);
                _selectedFloors.Add(_floors[i]);
                UpdateFloorSelection(i, true);
            }
        }
        
        _statusMessage = $"已选择 {_selectedFloors.Count} 个地板 (索引 {startIndex}-{endIndex})";
    }
    
    /// <summary>
    /// 更新地板的选中状态（设置绿色高亮）
    /// </summary>
    private void UpdateFloorSelection(int floorIndex, bool selected)
    {
        if (_playerFloors == null || floorIndex < 0 || floorIndex >= _playerFloors.Count)
            return;
        
        _playerFloors[floorIndex].SetSelected(selected);
    }
    
    /// <summary>
    /// 清除所有选择
    /// </summary>
    private void ClearSelection()
    {
        // 清除所有地板的选中状态
        foreach (var index in _selectedFloorIndices)
        {
            UpdateFloorSelection(index, false);
        }
        _selectedFloorIndices.Clear();
        _selectedFloors.Clear();
    }
    
    /// <summary>
    /// 获取指定位置的地板
    /// 参考 GLWindow.xaml.cs 的 GetTileAtPosition 方法
    /// </summary>
    private Floor? GetFloorAtPosition(Vector2 worldPos)
    {
        if (_floors == null)
            return null;
        
        // 从后往前遍历（后面的地板在上层）
        for (int i = _floors.Count - 1; i >= 0; i--)
        {
            var floor = _floors[i];
            
            // 检查点是否在地板范围内
            // 使用简单的距离检测
            float distance = Vector2.Distance(worldPos, floor.position);
            float threshold = Floor.width * 1.5f; // 增加一些容差
            
            if (distance < threshold)
            {
                return floor;
            }
        }
        
        return null;
    }
}
