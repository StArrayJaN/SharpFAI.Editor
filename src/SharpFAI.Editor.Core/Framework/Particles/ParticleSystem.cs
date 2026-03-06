using System.Drawing;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;
using SharpFAI.Editor.Core.Framework.Graphics;
using SharpFAI.Framework;

namespace SharpFAI.Editor.Core.Framework.Particles;

/// <summary>
/// High-performance particle system using quad mesh batch rendering
/// 使用四边形网格批量渲染的高性能粒子系统
/// </summary>
public class ParticleSystem : IDisposable
{
    private const int MAX_PARTICLES = 1000;
    private const int VERTICES_PER_PARTICLE = 4; // Quad
    private const int INDICES_PER_PARTICLE = 6;  // 2 triangles
    private const int VERTEX_SIZE = 9; // x, y, u, v, size, r, g, b, a

    private Particle[] _particles = null!;
    private int _vao;
    private int _vbo;
    private int _ebo;
    private float[] _vertices = null!;
    private ushort[] _indices = null!;
    private GLShader _shader = null!;
    private bool _disposed;
    private int _nextEmitIndex;
    private int _activeCount;

    public ParticleSystem()
    {
        _particles = new Particle[MAX_PARTICLES];
        for (int i = 0; i < MAX_PARTICLES; i++)
            _particles[i] = new Particle();
        InitializeGL();
        CreateShader();
    }

    private void InitializeGL()
    {
        // Create VAO
        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);

        // Create VBO
        _vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);

        // Allocate buffer
        _vertices = new float[MAX_PARTICLES * VERTICES_PER_PARTICLE * VERTEX_SIZE];
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicDraw);

        // Position attribute (location 0)
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, VERTEX_SIZE * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        // Texture coordinate attribute (location 1)
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, VERTEX_SIZE * sizeof(float), 2 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        // Size attribute (location 2)
        GL.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, VERTEX_SIZE * sizeof(float), 4 * sizeof(float));
        GL.EnableVertexAttribArray(2);

        // Color attribute (location 3)
        GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, VERTEX_SIZE * sizeof(float), 5 * sizeof(float));
        GL.EnableVertexAttribArray(3);

        // Create EBO
        _ebo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);

        // Create index buffer (each particle is a quad)
        _indices = new ushort[MAX_PARTICLES * INDICES_PER_PARTICLE];
        for (int i = 0; i < MAX_PARTICLES; i++)
        {
            int baseVertex = i * VERTICES_PER_PARTICLE;
            int baseIndex = i * INDICES_PER_PARTICLE;

            // First triangle
            _indices[baseIndex] = (ushort)baseVertex;
            _indices[baseIndex + 1] = (ushort)(baseVertex + 1);
            _indices[baseIndex + 2] = (ushort)(baseVertex + 2);

            // Second triangle
            _indices[baseIndex + 3] = (ushort)(baseVertex + 2);
            _indices[baseIndex + 4] = (ushort)(baseVertex + 3);
            _indices[baseIndex + 5] = (ushort)baseVertex;
        }

        GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(ushort), _indices, BufferUsageHint.StaticDraw);

        GL.BindVertexArray(0);
    }

    private void CreateShader()
    {
        string vertexShader = @"
#version 330 core
layout (location = 0) in vec2 a_position;
layout (location = 1) in vec2 a_texCoord;
layout (location = 2) in float a_size;
layout (location = 3) in vec4 a_color;

uniform mat4 uView;
uniform mat4 uProjection;
uniform float u_time;

out vec2 v_texCoord;
out vec4 v_color;

void main()
{
    gl_Position = uProjection * uView * vec4(a_position, 0.0, 1.0);
    v_texCoord = a_texCoord;
    v_color = a_color;
}
";

        string fragmentShader = @"
#version 330 core
in vec2 v_texCoord;
in vec4 v_color;

out vec4 FragColor;

void main()
{
    // Create circular particle
    vec2 coord = v_texCoord * 2.0 - 1.0; // Convert to [-1, 1]
    float dist = length(coord);
    
    if (dist > 1.0)
        discard;
    
    // Soft edge
    float alpha = 1.0 - smoothstep(0.7, 1.0, dist);
    
    FragColor = vec4(v_color.rgb, v_color.a * alpha);
}
";

        _shader = new GLShader(vertexShader, fragmentShader);
        _shader.Compile();

        if (!_shader.IsCompiled)
        {
            Console.WriteLine("Particle shader compilation failed:");
            Console.WriteLine(_shader.CompileLog);
        }
    }

    /// <summary>
    /// Emit a particle / 发射一个粒�?
    /// </summary>
    public void EmitParticle(Vector2 position, Vector2 velocity, Color color, float size, float life)
    {
        // 转换 Color �?Vector4
        Vector4 colorVec4 = new Vector4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
        EmitParticle(position, velocity, colorVec4, size, life);
    }

    /// <summary>
    /// Emit a particle with Vector4 color (optimized) / 发射粒子，使�?Vector4 颜色（优化版�?
    /// 优化：使�?Vector4 颜色避免转换开销，使用属性访�?IsAlive
    /// </summary>
    public void EmitParticle(Vector2 position, Vector2 velocity, Vector4 color, float size, float life)
    {
        // Use ring buffer style pool to avoid allocations
        int start = _nextEmitIndex;
        int found = -1;
        for (int i = 0; i < MAX_PARTICLES; i++)
        {
            int idx = (start + i) % MAX_PARTICLES;
            if (!_particles[idx].IsAlive)
            {
                found = idx;
                break;
            }
        }

        // If no dead particle found, overwrite nextEmitIndex
        if (found == -1)
            found = _nextEmitIndex;

        bool wasAlive = _particles[found].IsAlive;
        // 使用优化�?Vector4 版本初始�?
        _particles[found].Reset(position, velocity, color, size, life);
        if (!wasAlive)
            _activeCount++;

        _nextEmitIndex = (found + 1) % MAX_PARTICLES;
    }

    /// <summary>
    /// Update all particles / 更新所有粒�?
    /// </summary>
    public void Update(float deltaTime)
    {
        for (int i = 0; i < MAX_PARTICLES; i++)
        {
            var particle = _particles[i];
            if (particle.IsAlive)
            {
                particle.Update(deltaTime);
                if (!particle.IsAlive)
                {
                    // particle died this frame
                    _activeCount = Math.Max(0, _activeCount - 1);
                }
            }
        }
    }

    /// <summary>
    /// Render all active particles / 渲染所有活跃粒�?
    /// 优化：使用统一�?Vector4 颜色，避免多次字段访�?
    /// </summary>
    public void Render(ICamera camera, float time)
    {
        if (_disposed) return;

        // Count active particles
        int activeParticles = _activeCount;
        if (activeParticles == 0) return;

        // Update vertex buffer
        int vertexIndex = 0;
        for (int i = 0; i < MAX_PARTICLES && vertexIndex / 4 < MAX_PARTICLES; i++)
        {
            var particle = _particles[i];
            if (!particle.IsAlive) continue;

            Vector2 pos = particle.Position;
            Vector4 color = particle.Color;  // 直接使用 Vector4 颜色，无需分解
            float size = particle.Size;

            // Four vertices of quad
            float halfSize = size * 0.5f;

            // Bottom-left
            AddVertex(vertexIndex++, pos.X - halfSize, pos.Y - halfSize, 0.0f, 0.0f, size, color.X, color.Y, color.Z, color.W);
            // Bottom-right
            AddVertex(vertexIndex++, pos.X + halfSize, pos.Y - halfSize, 1.0f, 0.0f, size, color.X, color.Y, color.Z, color.W);
            // Top-right
            AddVertex(vertexIndex++, pos.X + halfSize, pos.Y + halfSize, 1.0f, 1.0f, size, color.X, color.Y, color.Z, color.W);
            // Top-left
            AddVertex(vertexIndex++, pos.X - halfSize, pos.Y + halfSize, 0.0f, 1.0f, size, color.X, color.Y, color.Z, color.W);
        }

        int renderParticles = vertexIndex / 4;
        if (renderParticles == 0) return;

        // Update mesh data
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, renderParticles * VERTICES_PER_PARTICLE * VERTEX_SIZE * sizeof(float), _vertices);

        // Set render state
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // Use shader
        _shader.Use();
        _shader.SetMatrix4x4("uView", camera.GetViewMatrix());
        _shader.SetMatrix4x4("uProjection", camera.GetProjectionMatrix());
        _shader.SetFloat("u_time", time);

        // Render
        GL.BindVertexArray(_vao);
        GL.DrawElements(PrimitiveType.Triangles, renderParticles * INDICES_PER_PARTICLE, DrawElementsType.UnsignedShort, 0);
        GL.BindVertexArray(0);

        GL.Disable(EnableCap.Blend);
    }

    private void AddVertex(int index, float x, float y, float u, float v, float size, float r, float g, float b, float a)
    {
        int i = index * VERTEX_SIZE;
        _vertices[i] = x;
        _vertices[i + 1] = y;
        _vertices[i + 2] = u;
        _vertices[i + 3] = v;
        _vertices[i + 4] = size;
        _vertices[i + 5] = r;
        _vertices[i + 6] = g;
        _vertices[i + 7] = b;
        _vertices[i + 8] = a;
    }

    /// <summary>
    /// Clear all particles / 清除所有粒�?
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < MAX_PARTICLES; i++)
        {
            _particles[i].Kill();
        }
        _activeCount = 0;
    }

    /// <summary>
    /// Get active particle count / 获取活跃粒子数量
    /// </summary>
    public int GetActiveParticleCount()
    {
        return _activeCount;
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_vao != 0)
            GL.DeleteVertexArray(_vao);
        if (_vbo != 0)
            GL.DeleteBuffer(_vbo);
        if (_ebo != 0)
            GL.DeleteBuffer(_ebo);

        _shader?.Dispose();
        Array.Clear(_particles, 0, _particles.Length);

        _disposed = true;
    }
}




