using System.Drawing;
using System.Numerics;
using SharpFAI.Editor.Core.Framework.Graphics;
using SharpFAI.Editor.Core.Framework.Particles;
using SharpFAI.Framework;

namespace SharpFAI.Editor.Core.Models;

/// <summary>
/// Planet implementation with particle-based trail
/// 带粒子拖尾的行星实现
/// </summary>
public class Planet : IPlanet, IDisposable
{
    /// <summary>
    /// Planet position / 行星位置
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// Planet radius / 行星半径
    /// </summary>
    public float Radius { get; set; }

    /// <summary>
    /// Planet color / 行星颜色
    /// </summary>
    public Color Color { get; set; }

    /// <summary>
    /// Planet rotation angle / 行星旋转角度
    /// </summary>
    public float Rotation { get; set; }

    /// <summary>
    /// Tail (particle system) / 尾迹（粒子系统）
    /// </summary>
    public ITail Tail => null; // Not used in this implementation

    // Trail enabled flag
    private readonly bool _trailEnabled;
    
    // Particle system
    private ParticleSystem _particleSystem;
    
    // Particles per unit distance (similar to Unity Trail Renderer)
    private float _particlesPerUnit = 0.3f;
    
    // Last position for distance-based trail generation
    private Vector2 _lastPosition;
    private bool _firstUpdate = true;
    
    // Planet mesh
    private GLMesh _mesh;
    
    // Pause particles flag
    private bool _particlesPaused;

    /// <summary>
    /// Create a new planet / 创建新行�?    /// </summary>
    public Planet(Color color, bool trailEnabled = true)
    {
        Color = color;
        _trailEnabled = trailEnabled;
        Position = Vector2.Zero;
        Radius = 1.0f;
        Rotation = 0;

        if (_trailEnabled)
        {
            _particleSystem = new ParticleSystem();
        }

        _lastPosition = Vector2.Zero;
        
        // Create planet mesh - 转换 Color �?Vector4
        Vector4 colorVec4 = new Vector4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
        _mesh = GLMesh.CreateCircle(1.0f, 32, colorVec4);
    }

    /// <summary>
    /// Update planet state / 更新行星状�?    /// </summary>
    public void Update(float deltaTime)
    {
        if (_trailEnabled && _particleSystem != null)
        {
            // Update particle system
            _particleSystem.Update(deltaTime);

            // Generate distance-based trail particles
            if (!_particlesPaused)
            {
                GenerateDistanceBasedTrail();
            }
        }
    }

    /// <summary>
    /// Generate distance-based trail particles (similar to Unity Trail Renderer)
    /// Maintains continuous trail regardless of movement speed
    /// 基于距离生成拖尾粒子（类似Unity Trail Renderer�?    /// 无论移动速度多快都能保持连贯的拖尾效�?    /// </summary>
    private void GenerateDistanceBasedTrail()
    {
        if (_firstUpdate)
        {
            _lastPosition = Position;
            _firstUpdate = false;
            return;
        }

        // Calculate movement distance
        float distance = Vector2.Distance(Position, _lastPosition);

        if (distance > 0.1f) // Minimum movement threshold
        {
            // Calculate number of particles to generate based on distance
            int particlesToGenerate = Math.Min(Math.Max(1, (int)(distance * _particlesPerUnit)),500);

            // Distribute particles evenly along movement path
            for (int i = 0; i < particlesToGenerate; i++)
            {
                float t = i / (float)particlesToGenerate;

                // Linear interpolation between lastPosition and current position
                Vector2 particlePosition = Vector2.Lerp(_lastPosition, Position, t);

                // Add slight random offset for natural feel
                float offsetRadius = Radius * 0.2f;
                float angle = (float)(Random.Shared.NextDouble() * 2 * Math.PI);
                float offsetDistance = (float)(Random.Shared.NextDouble() * offsetRadius);

                particlePosition.X += MathF.Cos(angle) * offsetDistance;
                particlePosition.Y += MathF.Sin(angle) * offsetDistance;

                // Particle velocity - slight outward dispersion
                Vector2 particleVelocity = new Vector2(
                    MathF.Cos(angle) * Radius * 0.05f,
                    MathF.Sin(angle) * Radius * 0.05f
                );

                // Particle properties
                Color particleColor = Color;
                
                float particleSize = Radius * (0.8f + (float)Random.Shared.NextDouble() * 0.4f);
                float particleLife = 0.4f + (float)Random.Shared.NextDouble() * 0.4f;

                _particleSystem.EmitParticle(particlePosition, particleVelocity, particleColor, particleSize, particleLife);
            }

            // Update last position
            _lastPosition = Position;
        }
    }

    /// <summary>
    /// Render planet / 渲染行星
    /// </summary>
    public void Render(IShader shader)
    {
        Render(shader, null);
    }

    /// <summary>
    /// Render planet with camera / 使用相机渲染行星
    /// </summary>
    public void Render(IShader shader, ICamera camera)
    {
        // Render trail particles (behind planet)
        if (_trailEnabled && _particleSystem != null && camera != null)
        {
            float time = (DateTime.Now.Ticks / TimeSpan.TicksPerSecond);
            _particleSystem.Render(camera, time);
            
            // Restore main shader after particle rendering
            shader?.Use();
        }

        // Render main planet
        _mesh.Position = new Vector3(Position.X, Position.Y, 0);
        _mesh.Rotation = new Vector3(0, 0, Rotation);
        _mesh.Scale = new Vector3(Radius, Radius, 1);
        _mesh.Render(shader);
    }

    /// <summary>
    /// Move planet to target position / 移动行星到目标位�?    /// </summary>
    public void MoveTo(Vector2 target)
    {
        Position = target;
    }

    /// <summary>
    /// Set trail density (particles per unit distance) / 设置拖尾密度（每单位距离的粒子数�?    /// </summary>
    public void SetParticlesPerUnit(float density)
    {
        _particlesPerUnit = Math.Max(0, density);
    }

    /// <summary>
    /// Get trail density / 获取拖尾密度
    /// </summary>
    public float GetParticlesPerUnit()
    {
        return _particlesPerUnit;
    }

    /// <summary>
    /// Get active particle count / 获取活跃粒子数量
    /// </summary>
    public int GetActiveParticleCount()
    {
        return _particleSystem?.GetActiveParticleCount() ?? 0;
    }

    /// <summary>
    /// Clear all particles / 清除所有粒�?    /// </summary>
    public void ClearParticles()
    {
        _particleSystem?.Clear();
    }

    /// <summary>
    /// Pause/resume particle generation / 暂停/恢复粒子生成
    /// </summary>
    public void SetParticlesPaused(bool paused)
    {
        _particlesPaused = paused;
    }

    public void Dispose()
    {
        _mesh?.Dispose();
        _particleSystem?.Dispose();
    }
}


