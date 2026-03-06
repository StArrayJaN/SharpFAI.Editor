using System.Numerics;
using OpenTK.Graphics.OpenGL4;
using SharpFAI.Framework;

namespace SharpFAI.Editor.Core.Framework.Graphics;

/// <summary>
/// OpenGL mesh implementation using VAO/VBO/EBO
/// 使用VAO/VBO/EBO的OpenGL网格实现
/// </summary>
public class GLMesh : Mesh
{
    private int _vao; // Vertex Array Object
    private int _vbo; // Vertex Buffer Object
    private int _ebo; // Element Buffer Object
    private int _colorVbo; // Color Buffer Object
    private int _texCoordVbo; // Texture Coordinate Buffer Object
    private bool _isGLInitialized;

    /// <summary>
    /// Create an empty GL mesh / 创建空GL网格
    /// </summary>
    public GLMesh()
    {
    }
    
    /// <summary>
    /// Create a GL mesh with vertices and indices
    /// 使用顶点和索引创建GL网格
    /// </summary>
    public GLMesh(Vector3[] vertices, int[] indices) : base(vertices, indices)
    {
    }
    
    /// <summary>
    /// Create a GL mesh with full vertex data
    /// 使用完整顶点数据创建GL网格
    /// </summary>
    public GLMesh(Vector3[] vertices, int[] indices, Vector4[]? colors = null, Vector2[]? texCoords = null, Vector3[]? normals = null)
        : base(vertices, indices, colors, texCoords, normals)
    {
    }
    
    /// <summary>
    /// Upload mesh data to GPU / 上传网格数据到GPU
    /// </summary>
    public override void Upload()
    {
        base.Upload();
        
        if (_isGLInitialized)
        {
            // Clean up existing buffers
            DeleteBuffers();
        }
        
        // Generate VAO
        _vao = GL.GenVertexArray();
        GL.BindVertexArray(_vao);
        
        // Upload vertex positions
        _vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        
        float[] vertexData = new float[Vertices.Length * 3];
        for (int i = 0; i < Vertices.Length; i++)
        {
            vertexData[i * 3 + 0] = Vertices[i].X;
            vertexData[i * 3 + 1] = Vertices[i].Y;
            vertexData[i * 3 + 2] = Vertices[i].Z;
        }
        GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Length * sizeof(float), vertexData, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        
        // Upload vertex colors
        if (Colors != null && Colors.Length > 0)
        {
            _colorVbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _colorVbo);
            
            // Optimize: Colors are already Vector4, convert directly to float array
            float[] colorData = new float[Colors.Length * 4];
            for (int i = 0; i < Colors.Length; i++)
            {
                colorData[i * 4 + 0] = Colors[i].X;
                colorData[i * 4 + 1] = Colors[i].Y;
                colorData[i * 4 + 2] = Colors[i].Z;
                colorData[i * 4 + 3] = Colors[i].W;
            }
            GL.BufferData(BufferTarget.ArrayBuffer, colorData.Length * sizeof(float), colorData, BufferUsageHint.DynamicDraw);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
        }
        
        // Upload texture coordinates
        if (TexCoords != null && TexCoords.Length > 0)
        {
            _texCoordVbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _texCoordVbo);
            
            float[] texCoordData = new float[TexCoords.Length * 2];
            for (int i = 0; i < TexCoords.Length; i++)
            {
                texCoordData[i * 2 + 0] = TexCoords[i].X;
                texCoordData[i * 2 + 1] = TexCoords[i].Y;
            }
            GL.BufferData(BufferTarget.ArrayBuffer, texCoordData.Length * sizeof(float), texCoordData, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
            GL.EnableVertexAttribArray(2);
        }
        
        // Upload indices
        _ebo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, Indices.Length * sizeof(int), Indices, BufferUsageHint.StaticDraw);
        
    // Unbind VAO and array buffer. Keep EBO bound to the VAO (no need to unbind)
    GL.BindVertexArray(0);
    GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        
        _isGLInitialized = true;
    }
    
    /// <summary>
    /// Bind mesh for rendering / 绑定网格用于渲染
    /// </summary>
    public override void Bind()
    {
        base.Bind();
        
        if (!_isGLInitialized)
        {
            Upload();
        }
        
        GL.BindVertexArray(_vao);
    }
    
    /// <summary>
    /// Unbind mesh / 解绑网格
    /// </summary>
    public override void Unbind()
    {
        base.Unbind();
        GL.BindVertexArray(0);
    }
    
    /// <summary>
    /// Render the mesh / 渲染网格
    /// </summary>
    public void Render()
    {
        Bind();
        GL.DrawElements(PrimitiveType.Triangles, Indices.Length, DrawElementsType.UnsignedInt, 0);
        Unbind();
    }
    
    /// <summary>
    /// Render the mesh with shader and apply model matrix / 使用着色器渲染网格并应用模型矩�?    /// </summary>
    /// <param name="shader">Shader to use for rendering / 用于渲染的着色器</param>
    public void Render(IShader shader)
    {
        if (shader == null)
            throw new ArgumentNullException(nameof(shader));
            
        shader.SetMatrix4x4("uModel", GetModelMatrix());
        Render();
    }
    
    /// <summary>
    /// Update vertex colors dynamically / 动态更新顶点颜�?    /// </summary>
    public void UpdateColors(Vector4[] newColors)
    {
        if (newColors == null || newColors.Length != Colors.Length)
            throw new ArgumentException("Color array length must match existing colors");
        
        Colors = newColors;
        
        if (_isGLInitialized && _colorVbo != 0)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, _colorVbo);
            
            float[] colorData = new float[Colors.Length * 4];
            for (int i = 0; i < Colors.Length; i++)
            {
                colorData[i * 4 + 0] = Colors[i].X;
                colorData[i * 4 + 1] = Colors[i].Y;
                colorData[i * 4 + 2] = Colors[i].Z;
                colorData[i * 4 + 3] = Colors[i].W;
            }
            GL.BufferData(BufferTarget.ArrayBuffer, colorData.Length * sizeof(float), colorData, BufferUsageHint.DynamicDraw);
            
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }
    }
    
    /// <summary>
    /// Delete OpenGL buffers / 删除OpenGL缓冲�?    /// </summary>
    private void DeleteBuffers()
    {
        if (_vao != 0)
        {
            GL.DeleteVertexArray(_vao);
            _vao = 0;
        }
        
        if (_vbo != 0)
        {
            GL.DeleteBuffer(_vbo);
            _vbo = 0;
        }
        
        if (_colorVbo != 0)
        {
            GL.DeleteBuffer(_colorVbo);
            _colorVbo = 0;
        }
        
        if (_texCoordVbo != 0)
        {
            GL.DeleteBuffer(_texCoordVbo);
            _texCoordVbo = 0;
        }
        
        if (_ebo != 0)
        {
            GL.DeleteBuffer(_ebo);
            _ebo = 0;
        }
        
        _isGLInitialized = false;
    }
    
    /// <summary>
    /// Dispose mesh resources / 释放网格资源
    /// </summary>
    public override void Dispose()
    {
        DeleteBuffers();
        base.Dispose();
    }
    
    /// <summary>
    /// Create a GL quad mesh / 创建GL四边形网�?    /// </summary>
    public new static GLMesh CreateQuad(float width = 1.0f, float height = 1.0f)
    {
        Mesh baseMesh = Mesh.CreateQuad(width, height);
        return new GLMesh(baseMesh.Vertices, baseMesh.Indices, baseMesh.Colors, baseMesh.TexCoords, baseMesh.Normals);
    }
    
    /// <summary>
    /// Create a GL circle mesh / 创建GL圆形网格
    /// </summary>
    public static GLMesh CreateCircle(float radius = 1.0f, int segments = 32, Vector4? color = null)
    {
        Mesh baseMesh = Mesh.CreateCircle(radius, segments, color);
        return new GLMesh(baseMesh.Vertices, baseMesh.Indices, baseMesh.Colors, baseMesh.TexCoords, baseMesh.Normals);
    }
}


