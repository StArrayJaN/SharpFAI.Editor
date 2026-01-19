using System.Numerics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpFAI.Framework;
using SharpFAI.Util;

namespace SharpFAI_Player;

/// <summary>
/// EditorPlayer - 播放功能部分
/// 移植自 MainPlayer
/// </summary>
public partial class EditorPlayer : IPlayer
{
    public void CreatePlayer()
    {
        // CreatePlayer 逻辑已在 LoadLevel 中实现
        // CreatePlayer logic is already implemented in LoadLevel
    }

    public void UpdatePlayer(double delta)
    {
        // 早期返回如果未初始化
        // Early return if not initialized
        if (!_initialized || _camera2D == null)
            return;
        
        // 播放控制
        // Playback controls
        if (KeyboardState.IsKeyPressed(Keys.Space))
        {
            if (!_isPlaying)
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
        
        // 使用 P 键暂停/恢复
        // Pause/Resume with P key
        if (KeyboardState.IsKeyPressed(Keys.P) && _initialized)
        {
            if (_isPlaying)
            {
                PausePlay();
            }
            else
            {
                ResumePlay();
            }
        }

        if (_isPlaying)
        {
            _currentTime += delta;
            
            // 检查播放是否已结束
            // Check if playback has finished
            double totalTime = _music?.Duration ?? 0;
            
            // 如果没有音乐，使用音符时间作为总时长
            // If no music, use note times as total duration
            if (totalTime <= 0 && _noteTimes != null && _noteTimes.Count > 0)
            {
                totalTime = _noteTimes[_noteTimes.Count - 1] / 1000.0 + 5.0; // 最后一个音符 + 5秒缓冲
            }
            
            if (totalTime > 0 && _currentTime >= totalTime)
            {
                // 限制时间不超过总时长以防止越界
                // Clamp time to total duration to prevent overflow
                _currentTime = totalTime;
                PausePlay();
            }
            
            if (_noteTimes != null && _floors != null)
            {
                while (_currentIndex < _noteTimes.Count - 1 && _currentTime >= _noteTimes[_currentIndex] / 1000)
                {
                    _currentIndex++;
                    MoveToNextFloor(_floors[_currentIndex]);
                }
            }
        }
        
        // 更新旋转
        // Update rotation
        if (_currentFloor != null)
        {
            _rotationSpeed = (_isCw ? -1 : 1) * (_currentFloor.bpm / 60f) * 180 * delta;
            _angle += _rotationSpeed;
            if (_angle >= 360) _angle = 0;
            _isCw = _currentFloor.isCW;
            
            float bpm = (float)_currentFloor.bpm;
            _cameraSpeed = (60f / bpm) * 2f; // crotchet * 2
        }
        
        // 只在播放时更新摄像机跟随
        // Only update camera follow when playing
        if (_isPlaying)
        {
            _cameraTimer += (float)delta;
            
            float distance = Vector2.Distance(_cameraFromPos, _cameraToPos);
            float speedMultiplier = 1.0f;
            if (distance > 5f)
            {
                float distanceFactor = FloatMath.Min(1.0f, (distance - 5f) / 5f);
                speedMultiplier = distanceFactor * 0.5f + 1f;
            }
        
            // 插值摄像机位置
            // Lerp camera position
            float t = _cameraTimer / (_cameraSpeed / speedMultiplier);
            if (t > 1.0f) t = 1.0f;
        
            _camera2D.Position = Vector2.Lerp(_cameraFromPos, _cameraToPos, t);
        }
    }

    public void RenderPlayer(double delta)
    {
        // 仅在初始化后渲染
        // Only render if initialized
        if (!_initialized || _shader == null || _camera2D == null)
            return;
        
        // 只有在显示星球时才渲染
        // Only render when planets should be shown
        if (!_showPlanets)
            return;
        
        // 更新并渲染星球
        // Update and render planets
        _currentPlanet?.Update((float)delta);
        _lastPlanet?.Update((float)delta);
        
        if (_currentFloor != null && _currentPlanet != null && _lastPlanet != null)
        {
            _currentPlanet.Position = _currentFloor.position;
            
            Vector2 offset = new Vector2(
                FloatMath.Cos(_angle, true) * Floor.length * 2,
                FloatMath.Sin(_angle, true) * Floor.length * 2
            );
            _lastPlanet.Position = offset + _currentFloor.position;
        }
    
        _bluePlanet?.Render(_shader, _camera2D);
        _redPlanet?.Render(_shader, _camera2D);
    }

    public void MoveToNextFloor(Floor next)
    {
        if (_floors == null || _playerFloors == null || _currentIndex >= _floors.Count)
        {
            return;
        }
        
        // 标记前一个地板为已命中（隐藏它）
        // Mark previous floor as hit (hide it)
        if (_currentIndex > 0 && _currentIndex < _playerFloors.Count)
        {
            _playerFloors[_currentIndex - 1].isHit = true;
        }
        
        _currentIndex = next.index;
        if (next.isMidspin)
        {
            // 隐藏中旋地板本身
            // Hide the midspin floor itself
            if (next.index < _playerFloors.Count)
            {
                _playerFloors[next.index].isHit = true;
            }
            _currentIndex++;
        }
        
        if (_currentIndex < _floors.Count)
        {
            _currentFloor = _floors[_currentIndex];
        }
        
        if (_currentFloor != null && !_currentFloor.lastFloor.isMidspin)
        {
            (_currentPlanet, _lastPlanet) = (_lastPlanet, _currentPlanet);
        }
        
        if (_currentFloor != null && _camera2D != null)
        {
            _angle = (_currentFloor.lastFloor.angle + 180).Fmod(360);
            
            _cameraFromPos = _camera2D.Position;
            _cameraToPos = _currentFloor.position;
            _cameraTimer = 0f;
        }
    }

    public void StartPlay()
    {
        if (!_initialized || _level == null)
            return;
            
        _ = StartPlayAsync();
    }

    private async Task StartPlayAsync()
    {
        if (_level == null)
            return;
        
        // 先显示星球但不开始时间进度
        // First show planets but don't start time progression
        _showPlanets = true;
        
        // 等待200毫秒让星球显示
        // Wait 200ms to show planets
        await Task.Delay(200);
        
        // 如果有音乐，启动音频
        // If there's music, start audio
        if (_music != null || _hitSound != null)
        {
            int offset = _level.GetSetting<int>("offset");
            
            // 同时启动两个音轨，使用精确计时
            // Start both audio tracks simultaneously with precise timing
            if (offset > 0)
            {
                // 如果有偏移，先启动音乐，然后延迟后启动命中音效
                // If there's an offset, start music first, then hitsound after delay
                _music?.Play();
                await Task.Delay(offset - 1);
                _hitSound?.Play();
            }
            else
            {
                // 无偏移，同时启动
                // No offset, start both simultaneously
                _hitSound?.Play();
                _music?.Play();
            }
        }
        
        // 现在才开始时间进度
        // Now start time progression
        _isPlaying = true;
    }

    public void StopPlay()
    {
        _isPlaying = false;
        _showPlanets = false;
        
        // 清除鼠标拖动状态
        _isDragging = false;
        _wasMouseMoving = false;
        
        if (_initialized)
        {
            ResetPlayer();
        }
    }

    public void PausePlay()
    {
        _isPlaying = false;
        _music?.Pause();
        _hitSound?.Pause();
    }

    public void ResumePlay()
    {
        _isPlaying = true;
        _music?.Resume();
        _hitSound?.Resume();
    }

    public void ResetPlayer()
    {
        if (!_initialized || _floors == null || _floors.Count == 0)
            return;
            
        _currentFloor = _floors[0];
        _angle = 0;
        _isPlaying = false;
        _showPlanets = false;
        _currentIndex = 0;
        _currentTime = 0;
        _music?.Stop();
        _hitSound?.Stop();
        
        // 重置所有地板为可见
        // Reset all floors to visible
        if (_playerFloors != null)
        {
            foreach (var floor in _playerFloors)
            {
                floor.isHit = false;
            }
        }
        
        // 重置摄像机位置
        // Reset camera position
        if (_camera2D != null && _floors.Count > 0)
        {
            _camera2D.Position = _floors[0].position;
            _cameraFromPos = _floors[0].position;
            _cameraToPos = _floors[0].position;
            _cameraTimer = 0f;
        }
    }

    public void DestroyPlayer()
    {
        _isPlaying = false;
        
        // 释放音频资源
        // Dispose audio resources
        _music?.Dispose();
        _music = null;
        _hitSound?.Dispose();
        _hitSound = null;
        
        // 释放 OpenGL 资源
        // Dispose OpenGL resources
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
        // Dispose planets
        _redPlanet?.Dispose();
        _bluePlanet?.Dispose();
        _redPlanet = null;
        _bluePlanet = null;
        _lastPlanet = null;
        _currentPlanet = null;
        
        // 释放着色器
        // Dispose shader
        _shader?.Dispose();
        _shader = null;
        
        // 清空集合
        // Clear collections
        _floors = null;
        _noteTimes = null;
        
        _initialized = false;
    }
}
