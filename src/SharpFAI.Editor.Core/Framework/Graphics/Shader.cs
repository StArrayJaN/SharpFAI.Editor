using System.Drawing;
using System.Numerics;
using SharpFAI.Framework;

namespace SharpFAI.Editor.Core.Framework.Graphics;

/// <summary>
/// Basic shader implementation for storing shader source code
/// 用于存储着色器源代码的基本着色器实现
/// </summary>
public class Shader : IShader
{
    /// <summary>
    /// Vertex shader source code / 顶点着色器源代�?    /// </summary>
    public string VertexSource { get; protected set; }
    
    /// <summary>
    /// Fragment shader source code / 片段着色器源代�?    /// </summary>
    public string FragmentSource { get; protected set; }
    
    /// <summary>
    /// Geometry shader source code (optional) / 几何着色器源代码（可选）
    /// </summary>
    public string GeometrySource { get; protected set; }
    
    /// <summary>
    /// Check if shader is compiled / 检查着色器是否已编�?    /// </summary>
    public bool IsCompiled { get; protected set; }
    
    /// <summary>
    /// Get compilation log / 获取编译日志
    /// </summary>
    public string CompileLog { get; protected set; } = string.Empty;
    
    /// <summary>
    /// Check if there were compilation errors / 检查是否有编译错误
    /// </summary>
    public bool HasCompileErrors { get; protected set; }
    
    protected Dictionary<string, int> _uniformLocations = new();
    protected Dictionary<string, int> _attributeLocations = new();
    protected bool _disposed;

    /// <summary>
    /// Create a shader with vertex and fragment source
    /// 使用顶点和片段着色器源代码创建着色器
    /// </summary>
    public Shader(string vertexSource, string fragmentSource, string geometrySource = null)
    {
        VertexSource = vertexSource ?? throw new ArgumentNullException(nameof(vertexSource));
        FragmentSource = fragmentSource ?? throw new ArgumentNullException(nameof(fragmentSource));
        GeometrySource = geometrySource;
        IsCompiled = false;
    }
    
    /// <summary>
    /// Compile and link shader program (override in derived classes)
    /// 编译并链接着色器程序（在派生类中重写�?    /// </summary>
    public virtual void Compile()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Shader));
            
        IsCompiled = true;
    }
    
    /// <summary>
    /// Use this shader for rendering (override in derived classes)
    /// 使用此着色器进行渲染（在派生类中重写�?    /// </summary>
    public virtual void Use()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Shader));
            
        if (!IsCompiled)
            throw new InvalidOperationException("Shader must be compiled before use");
    }
    
    /// <summary>
    /// Set uniform integer value / 设置uniform整数�?    /// </summary>
    public virtual void SetInt(string name, int value)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Shader));
    }
    
    /// <summary>
    /// Set uniform float value / 设置uniform浮点�?    /// </summary>
    public virtual void SetFloat(string name, float value)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Shader));
    }
    
    /// <summary>
    /// Set uniform Vector2 value / 设置uniform Vector2�?    /// </summary>
    public virtual void SetVector2(string name, Vector2 value)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Shader));
    }
    
    /// <summary>
    /// Set uniform Vector3 value / 设置uniform Vector3�?    /// </summary>
    public virtual void SetVector3(string name, Vector3 value)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Shader));
    }
    
    /// <summary>
    /// Set uniform Vector4 value / 设置uniform Vector4�?    /// </summary>
    public virtual void SetVector4(string name, Vector4 value)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Shader));
    }
    
    /// <summary>
    /// Set uniform Color value / 设置uniform颜色�?    /// </summary>
    public virtual void SetColor(string name, Color value)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Shader));
            
        SetVector4(name, new Vector4(
            value.R / 255.0f,
            value.G / 255.0f,
            value.B / 255.0f,
            value.A / 255.0f
        ));
    }
    
    /// <summary>
    /// Set uniform Matrix4x4 value / 设置uniform Matrix4x4�?    /// </summary>
    public virtual void SetMatrix4x4(string name, Matrix4x4 value)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Shader));
    }
    
    /// <summary>
    /// Get uniform location / 获取uniform位置
    /// </summary>
    public virtual int GetUniformLocation(string name)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Shader));
            
        if (_uniformLocations.TryGetValue(name, out int location))
            return location;
            
        return -1;
    }
    
    /// <summary>
    /// Get attribute location / 获取attribute位置
    /// </summary>
    public virtual int GetAttributeLocation(string name)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Shader));
            
        if (_attributeLocations.TryGetValue(name, out int location))
            return location;
            
        return -1;
    }
    
    /// <summary>
    /// Create a default 2D shader / 创建默认2D着色器
    /// </summary>
    public static Shader CreateDefault2D()
    {
        string vertexSource = @"
#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec4 aColor;
layout (location = 2) in vec2 aTexCoord;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec4 vColor;
out vec2 vTexCoord;

void main()
{
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
    vColor = aColor;
    vTexCoord = aTexCoord;
}
";

        string fragmentSource = @"
#version 330 core

in vec4 vColor;
in vec2 vTexCoord;

uniform sampler2D uTexture;
uniform bool uUseTexture;

out vec4 FragColor;

void main()
{
    if (uUseTexture)
        FragColor = texture(uTexture, vTexCoord) * vColor;
    else
        FragColor = vColor;
}
";

        return new Shader(vertexSource, fragmentSource);
    }
    
    /// <summary>
    /// Create a simple color shader / 创建简单颜色着色器
    /// </summary>
    public static Shader CreateColorShader()
    {
        string vertexSource = @"
#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec4 aColor;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec4 vColor;

void main()
{
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
    vColor = aColor;
}
";

        string fragmentSource = @"
#version 330 core

in vec4 vColor;
out vec4 FragColor;

void main()
{
    FragColor = vColor;
}
";

        return new Shader(vertexSource, fragmentSource);
    }
    
    /// <summary>
    /// Dispose shader resources / 释放着色器资源
    /// </summary>
    public virtual void Dispose()
    {
        if (_disposed)
            return;
            
        _uniformLocations.Clear();
        _attributeLocations.Clear();
        
        _disposed = true;
        GC.SuppressFinalize(this);
    }
    
    ~Shader()
    {
        Dispose();
    }
}


