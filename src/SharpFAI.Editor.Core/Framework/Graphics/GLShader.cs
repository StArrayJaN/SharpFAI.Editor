using System.Numerics;
using OpenTK.Graphics.OpenGL4;

namespace SharpFAI.Editor.Core.Framework.Graphics;

/// <summary>
/// OpenGL shader implementation
/// OpenGL着色器实现
/// </summary>
public class GLShader : Shader
{
    private int _programId;
    private int _vertexShaderId;
    private int _fragmentShaderId;
    private int _geometryShaderId;

    /// <summary>
    /// Create a GL shader with vertex and fragment source
    /// 使用顶点和片段着色器源代码创建GL着色器
    /// </summary>
    public GLShader(string vertexSource, string fragmentSource, string geometrySource = null)
        : base(vertexSource, fragmentSource, geometrySource)
    {
    }
    
    /// <summary>
    /// Compile and link shader program / 编译并链接着色器程序
    /// </summary>
    public override void Compile()
    {
        // Reset compile log
        CompileLog = string.Empty;
        HasCompileErrors = false;
        
        // Compile vertex shader
        _vertexShaderId = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(_vertexShaderId, VertexSource);
        GL.CompileShader(_vertexShaderId);
        CheckCompileErrors(_vertexShaderId, "VERTEX");
        
        // Compile fragment shader
        _fragmentShaderId = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(_fragmentShaderId, FragmentSource);
        GL.CompileShader(_fragmentShaderId);
        CheckCompileErrors(_fragmentShaderId, "FRAGMENT");
        
        // Compile geometry shader (if provided)
        if (!string.IsNullOrEmpty(GeometrySource))
        {
            _geometryShaderId = GL.CreateShader(ShaderType.GeometryShader);
            GL.ShaderSource(_geometryShaderId, GeometrySource);
            GL.CompileShader(_geometryShaderId);
            CheckCompileErrors(_geometryShaderId, "GEOMETRY");
        }
        
        // Create shader program
        _programId = GL.CreateProgram();
        GL.AttachShader(_programId, _vertexShaderId);
        GL.AttachShader(_programId, _fragmentShaderId);
        if (_geometryShaderId != 0)
        {
            GL.AttachShader(_programId, _geometryShaderId);
        }
        
        GL.LinkProgram(_programId);
        CheckLinkErrors(_programId);
        
        // Delete shaders (they're linked into the program now)
        GL.DeleteShader(_vertexShaderId);
        GL.DeleteShader(_fragmentShaderId);
        if (_geometryShaderId != 0)
        {
            GL.DeleteShader(_geometryShaderId);
        }
        
        base.Compile();
    }
    
    /// <summary>
    /// Use this shader for rendering / 使用此着色器进行渲染
    /// </summary>
    public override void Use()
    {
        base.Use();
        GL.UseProgram(_programId);
    }
    
    /// <summary>
    /// Get view matrix (if set) / 获取视图矩阵（如果已设置�?    /// </summary>
    internal Matrix4x4 GetViewMatrix()
    {
        // This is a helper method for Tail to access current view matrix
        // In a real implementation, you might want to cache this
        return Matrix4x4.Identity;
    }
    
    /// <summary>
    /// Get projection matrix (if set) / 获取投影矩阵（如果已设置�?    /// </summary>
    internal Matrix4x4 GetProjectionMatrix()
    {
        // This is a helper method for Tail to access current projection matrix
        // In a real implementation, you might want to cache this
        return Matrix4x4.Identity;
    }
    
    /// <summary>
    /// Set uniform integer value / 设置uniform整数�?    /// </summary>
    public override void SetInt(string name, int value)
    {
        base.SetInt(name, value);
        int location = GetUniformLocation(name);
        if (location >= 0)
        {
            GL.Uniform1(location, value);
        }
    }
    
    /// <summary>
    /// Set uniform float value / 设置uniform浮点�?    /// </summary>
    public override void SetFloat(string name, float value)
    {
        base.SetFloat(name, value);
        int location = GetUniformLocation(name);
        if (location >= 0)
        {
            GL.Uniform1(location, value);
        }
    }
    
    /// <summary>
    /// Set uniform Vector2 value / 设置uniform Vector2�?    /// </summary>
    public override void SetVector2(string name, Vector2 value)
    {
        base.SetVector2(name, value);
        int location = GetUniformLocation(name);
        if (location >= 0)
        {
            GL.Uniform2(location, value.X, value.Y);
        }
    }
    
    /// <summary>
    /// Set uniform Vector3 value / 设置uniform Vector3�?    /// </summary>
    public override void SetVector3(string name, Vector3 value)
    {
        base.SetVector3(name, value);
        int location = GetUniformLocation(name);
        if (location >= 0)
        {
            GL.Uniform3(location, value.X, value.Y, value.Z);
        }
    }
    
    /// <summary>
    /// Set uniform Vector4 value / 设置uniform Vector4�?    /// </summary>
    public override void SetVector4(string name, Vector4 value)
    {
        base.SetVector4(name, value);
        int location = GetUniformLocation(name);
        if (location >= 0)
        {
            GL.Uniform4(location, value.X, value.Y, value.Z, value.W);
        }
    }
    
    /// <summary>
    /// Set uniform Matrix4x4 value / 设置uniform Matrix4x4�?    /// </summary>
    public override void SetMatrix4x4(string name, Matrix4x4 value)
    {
        base.SetMatrix4x4(name, value);
        int location = GetUniformLocation(name);
        if (location >= 0)
        {
            unsafe
            {
                float* ptr = (float*)&value;
                GL.UniformMatrix4(location, 1, false, ptr);
            }
        }
    }
    
    /// <summary>
    /// Get uniform location / 获取uniform位置
    /// </summary>
    public override int GetUniformLocation(string name)
    {
        if (_uniformLocations.TryGetValue(name, out int location))
        {
            return location;
        }
        
        location = GL.GetUniformLocation(_programId, name);
        _uniformLocations[name] = location;
        
        return location;
    }
    
    /// <summary>
    /// Get attribute location / 获取attribute位置
    /// </summary>
    public override int GetAttributeLocation(string name)
    {
        if (_attributeLocations.TryGetValue(name, out int location))
        {
            return location;
        }
        
        location = GL.GetAttribLocation(_programId, name);
        _attributeLocations[name] = location;
        
        return location;
    }
    
    /// <summary>
    /// Check shader compilation errors / 检查着色器编译错误
    /// </summary>
    private void CheckCompileErrors(int shader, string type)
    {
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
        string infoLog = GL.GetShaderInfoLog(shader);
        
        // Always append to compile log if there's any info
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            CompileLog += $"=== {type} Shader ===\n{infoLog}\n";
        }
        
        if (success == 0)
        {
            HasCompileErrors = true;
            throw new Exception($"Shader compilation error ({type}):\n{infoLog}");
        }
    }
    
    /// <summary>
    /// Check program linking errors / 检查程序链接错�?    /// </summary>
    private void CheckLinkErrors(int program)
    {
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
        string infoLog = GL.GetProgramInfoLog(program);
        
        // Always append to compile log if there's any info
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            CompileLog += $"=== Program Linking ===\n{infoLog}\n";
        }
        
        if (success == 0)
        {
            HasCompileErrors = true;
            throw new Exception($"Shader linking error:\n{infoLog}");
        }
        
        // If we got here and there's no log, compilation was successful
        if (string.IsNullOrWhiteSpace(CompileLog))
        {
            CompileLog = "Shader compiled successfully with no warnings or errors.";
        }
    }
    
    /// <summary>
    /// Dispose shader resources / 释放着色器资源
    /// </summary>
    public override void Dispose()
    {
        if (_programId != 0)
        {
            GL.DeleteProgram(_programId);
            _programId = 0;
        }
        
        base.Dispose();
    }
    
    /// <summary>
    /// Create a default 2D GL shader / 创建默认2D GL着色器
    /// </summary>
    public new static GLShader CreateDefault2D()
    {
        Shader baseShader = Shader.CreateDefault2D();
        return new GLShader(baseShader.VertexSource, baseShader.FragmentSource);
    }
    
    /// <summary>
    /// Create a simple color GL shader / 创建简单颜色GL着色器
    /// </summary>
    public new static GLShader CreateColorShader()
    {
        Shader baseShader = Shader.CreateColorShader();
        return new GLShader(baseShader.VertexSource, baseShader.FragmentSource);
    }
}


