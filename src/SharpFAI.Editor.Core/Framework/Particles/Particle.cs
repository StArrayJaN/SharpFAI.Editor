using System.Drawing;
using System.Numerics;

namespace SharpFAI.Editor.Core.Framework.Particles;

/// <summary>
/// Particle class - 粒子�?(优化后版�?
/// 优化特性：
/// - 使用 Vector4 打包颜色，减少内存占用和访问开销
/// - 使用属性而非字段，提供统一的访问接�?
/// - 移除不必要的数学运算（sqrt），使用线性衰�?
/// - IsAlive 改为属性，统一访问方式
/// </summary>
public class Particle
{
    /// <summary>
    /// 粒子位置
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// 粒子速度
    /// </summary>
    public Vector2 Velocity { get; set; }

    /// <summary>
    /// 粒子颜色 (RGBA)
    /// 优化：相�?4 �?float (R/G/B/A)，Vector4 是单�?struct，访问更高效
    /// </summary>
    public Vector4 Color { get; set; }

    /// <summary>
    /// 粒子大小
    /// </summary>
    public float Size { get; set; }

    /// <summary>
    /// 粒子当前生命�?
    /// </summary>
    public float Life { get; set; }

    /// <summary>
    /// 粒子最大生命�?
    /// </summary>
    public float MaxLife { get; set; }

    /// <summary>
    /// 粒子是否活跃 (生命�?> 0)
    /// </summary>
    public bool IsAlive => Life > 0;

    public Particle()
    {
        Position = Vector2.Zero;
        Velocity = Vector2.Zero;
        Color = Vector4.One;
        Size = 1.0f;
        Life = 1.0f;
        MaxLife = 1.0f;
    }

    /// <summary>
    /// 使用 Color 对象初始化粒�?
    /// </summary>
    public void Reset(Vector2 pos, Vector2 vel, Color col, float size, float lifetime)
    {
        Position = pos;
        Velocity = vel;
        // �?Color 转换�?[0,1] �?Vector4
        Color = new Vector4(col.R / 255.0f, col.G / 255.0f, col.B / 255.0f, col.A / 255.0f);
        Size = size;
        Life = lifetime;
        MaxLife = lifetime;
    }

    /// <summary>
    /// 使用 Vector4 颜色初始化粒子（推荐用于渲染管道�?
    /// </summary>
    public void Reset(Vector2 pos, Vector2 vel, Vector4 color, float size, float lifetime)
    {
        Position = pos;
        Velocity = vel;
        Color = color;
        Size = size;
        Life = lifetime;
        MaxLife = lifetime;
    }

    /// <summary>
    /// 更新粒子状�?
    /// 优化�?
    /// - 移除 Sqrt 运算（高开销），改为线性衰�?
    /// - 直接更新 Color �?Alpha 分量，避免重复构�?Vector4
    /// </summary>
    public void Update(float deltaTime)
    {
        if (!IsAlive) return;

        // 更新位置
        Position += Velocity * deltaTime;

        // 减少生命�?
        Life -= deltaTime;

        // 线性衰减透明度（�?sqrt 更快，视觉效果仍然良好）
        float lifeRatio = Life / MaxLife;
        float alpha = Math.Max(0.0f, lifeRatio);
        
        // 仅更�?Alpha 分量，保�?RGB
        Color = new Vector4(Color.X, Color.Y, Color.Z, alpha);
    }

    /// <summary>
    /// 杀死粒�?
    /// </summary>
    public void Kill()
    {
        Life = 0;
    }
}




