using System.Drawing;
using System.Numerics;
using SharpFAI.Framework;

namespace SharpFAI.Editor.Core.Framework.Graphics;

/// <summary>
/// Basic mesh implementation for storing vertex data
/// 用于存储顶点数据的基本网格实�?/// 优化特性：
/// - 使用 Vector4[] 替代 Color[] 存储颜色，减少转换开销
/// - 所有数据字段均为属性，提供统一访问接口
/// </summary>
public class Mesh : IMesh
{
    /// <summary>
    /// Vertex positions / 顶点位置
    /// </summary>
    public Vector3[] Vertices { get; set; }
    
    /// <summary>
    /// Triangle indices / 三角形索�?    /// </summary>
    public int[] Indices { get; set; }
    
    /// <summary>
    /// Vertex colors as Vector4 (RGBA in [0,1]) / 顶点颜色（Vector4 RGBA�?    /// 优化：相�?Color[]，Vector4[] 避免�?GPU 上传时的转换开销
    /// </summary>
    public Vector4[] Colors { get; set; }

    /// <summary>
    /// 接口实现: 为了支持优化�?Vector4 颜色存储，同时保�?IMesh 兼容�?    /// 该属性提�?Color[] 视图用于接口实现
    /// </summary>
    Color[] IMesh.Colors
    {
        get => ConvertVector4ToColor(Colors);
        set => Colors = ConvertColorToVector4(value);
    }
    
    /// <summary>
    /// Texture coordinates / 纹理坐标
    /// </summary>
    public Vector2[] TexCoords { get; set; }
    
    /// <summary>
    /// Vertex normals / 顶点法线
    /// </summary>
    public Vector3[] Normals { get; set; }
    
    /// <summary>
    /// Get vertex count / 获取顶点数量
    /// </summary>
    public int VertexCount => Vertices?.Length ?? 0;
    
    /// <summary>
    /// Get triangle count / 获取三角形数�?    /// </summary>
    public int TriangleCount => (Indices?.Length ?? 0) / 3;
    
    /// <summary>
    /// Mesh position in world space / 网格在世界空间的位置
    /// </summary>
    public Vector3 Position { get; set; } = Vector3.Zero;
    
    /// <summary>
    /// Mesh rotation in degrees / 网格旋转角度（度�?    /// </summary>
    public Vector3 Rotation { get; set; } = Vector3.Zero;
    
    /// <summary>
    /// Mesh scale / 网格缩放
    /// </summary>
    public Vector3 Scale { get; set; } = Vector3.One;
    
    private bool _isUploaded;
    private bool _disposed;

    /// <summary>
    /// Create an empty mesh / 创建空网�?    /// </summary>
    public Mesh()
    {
        Vertices = Array.Empty<Vector3>();
        Indices = Array.Empty<int>();
        Colors = Array.Empty<Vector4>();
        TexCoords = Array.Empty<Vector2>();
        Normals = Array.Empty<Vector3>();
    }
    
    /// <summary>
    /// Create a mesh with vertices and indices
    /// 使用顶点和索引创建网�?    /// </summary>
    public Mesh(Vector3[] vertices, int[] indices)
    {
        Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
        Indices = indices ?? throw new ArgumentNullException(nameof(indices));
        Colors = CreateDefaultColors(vertices.Length);
        TexCoords = CreateDefaultTexCoords(vertices.Length);
        Normals = CreateDefaultNormals(vertices.Length);
    }
    
    /// <summary>
    /// Create a mesh with full vertex data
    /// 使用完整顶点数据创建网格
    /// </summary>
    public Mesh(Vector3[] vertices, int[] indices, Vector4[]? colors = null, Vector2[]? texCoords = null, Vector3[]? normals = null)
    {
        Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
        Indices = indices ?? throw new ArgumentNullException(nameof(indices));
        
        Colors = colors ?? CreateDefaultColors(vertices.Length);
        TexCoords = texCoords ?? CreateDefaultTexCoords(vertices.Length);
        Normals = normals ?? CreateDefaultNormals(vertices.Length);
    }
    
    /// <summary>
    /// Upload mesh data to GPU (override in derived classes for actual GPU upload)
    /// 上传网格数据到GPU（在派生类中重写以实现真正的GPU上传�?    /// </summary>
    public virtual void Upload()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Mesh));
            
        _isUploaded = true;
    }
    
    /// <summary>
    /// Update mesh data / 更新网格数据
    /// </summary>
    public virtual void UpdateData()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Mesh));
            
        if (_isUploaded)
        {
            // Re-upload data if already uploaded
            Upload();
        }
    }
    
    /// <summary>
    /// Bind mesh for rendering (override in derived classes)
    /// 绑定网格用于渲染（在派生类中重写�?    /// </summary>
    public virtual void Bind()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Mesh));
    }
    
    /// <summary>
    /// Unbind mesh (override in derived classes)
    /// 解绑网格（在派生类中重写�?    /// </summary>
    public virtual void Unbind()
    {
    }
    
    /// <summary>
    /// Calculate normals from triangle data
    /// 从三角形数据计算法线
    /// </summary>
    public void CalculateNormals()
    {
        if (Vertices == null || Indices == null)
            return;
            
        Normals = new Vector3[Vertices.Length];
        
        // Calculate face normals and accumulate
        for (int i = 0; i < Indices.Length; i += 3)
        {
            int idx0 = Indices[i];
            int idx1 = Indices[i + 1];
            int idx2 = Indices[i + 2];
            
            Vector3 v0 = Vertices[idx0];
            Vector3 v1 = Vertices[idx1];
            Vector3 v2 = Vertices[idx2];
            
            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;
            Vector3 normal = Vector3.Cross(edge1, edge2);
            
            Normals[idx0] += normal;
            Normals[idx1] += normal;
            Normals[idx2] += normal;
        }
        
        // Normalize all normals
        for (int i = 0; i < Normals.Length; i++)
        {
            Normals[i] = Vector3.Normalize(Normals[i]);
        }
    }
    
    /// <summary>
    /// Create a quad mesh / 创建四边形网�?    /// </summary>
    public static Mesh CreateQuad(float width = 1.0f, float height = 1.0f)
    {
        float halfWidth = width / 2.0f;
        float halfHeight = height / 2.0f;
        
        Vector3[] vertices = new[]
        {
            new Vector3(-halfWidth, -halfHeight, 0),
            new Vector3(halfWidth, -halfHeight, 0),
            new Vector3(halfWidth, halfHeight, 0),
            new Vector3(-halfWidth, halfHeight, 0)
        };
        
        int[] indices = new[] { 0, 1, 2, 2, 3, 0 };
        
        Vector2[] texCoords = new[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };
        
        return new Mesh(vertices, indices, texCoords: texCoords);
    }
    
    /// <summary>
    /// Create a circle mesh / 创建圆形网格
    /// </summary>
    public static Mesh CreateCircle(float radius = 1.0f, int segments = 32, Vector4? color = null)
    {
        Vector4 circleColor = color ?? Vector4.One;
        Vector3[] vertices = new Vector3[segments + 1];
        Vector4[] colors = new Vector4[segments + 1];
        int[] indices = new int[segments * 3];
        
        // Center vertex
        vertices[0] = Vector3.Zero;
        colors[0] = circleColor;
        
        // Circle vertices
        for (int i = 0; i < segments; i++)
        {
            float angle = 2.0f * MathF.PI * i / segments;
            vertices[i + 1] = new Vector3(
                MathF.Cos(angle) * radius,
                MathF.Sin(angle) * radius,
                0
            );
            colors[i + 1] = circleColor;
        }
        
        // Triangle indices
        for (int i = 0; i < segments; i++)
        {
            indices[i * 3] = 0;
            indices[i * 3 + 1] = i + 1;
            indices[i * 3 + 2] = (i + 1) % segments + 1;
        }
        
        return new Mesh(vertices, indices, colors);
    }
    
    /// <summary>
    /// Get the model matrix for this mesh / 获取此网格的模型矩阵
    /// </summary>
    public Matrix4x4 GetModelMatrix()
    {
        // Create transformation matrices
        Matrix4x4 translation = Matrix4x4.CreateTranslation(Position);
        
        // Rotation: Convert degrees to radians and create rotation matrix
        Matrix4x4 rotationX = Matrix4x4.CreateRotationX(Rotation.X * MathF.PI / 180.0f);
        Matrix4x4 rotationY = Matrix4x4.CreateRotationY(Rotation.Y * MathF.PI / 180.0f);
        Matrix4x4 rotationZ = Matrix4x4.CreateRotationZ(Rotation.Z * MathF.PI / 180.0f);
        Matrix4x4 rotation = rotationZ * rotationY * rotationX;
        
        Matrix4x4 scale = Matrix4x4.CreateScale(Scale);
        
        // Combine: Scale * Rotation * Translation
        return scale * rotation * translation;
    }
    
    /// <summary>
    /// Get the axis-aligned bounding box in local space / 获取局部空间的轴对齐包围盒
    /// </summary>
    public void GetLocalBounds(out Vector3 min, out Vector3 max)
    {
        if (Vertices == null || Vertices.Length == 0)
        {
            min = Vector3.Zero;
            max = Vector3.Zero;
            return;
        }
        
        min = Vertices[0];
        max = Vertices[0];
        
        for (int i = 1; i < Vertices.Length; i++)
        {
            min = Vector3.Min(min, Vertices[i]);
            max = Vector3.Max(max, Vertices[i]);
        }
    }
    
    /// <summary>
    /// Get the axis-aligned bounding box in world space / 获取世界空间的轴对齐包围�?    /// </summary>
    public void GetWorldBounds(out Vector3 min, out Vector3 max)
    {
        if (Vertices == null || Vertices.Length == 0)
        {
            min = Position;
            max = Position;
            return;
        }
        
        // Get local bounds
        GetLocalBounds(out Vector3 localMin, out Vector3 localMax);
        
        // Transform all 8 corners of the bounding box by the model matrix
        Matrix4x4 modelMatrix = GetModelMatrix();
        
        Vector3[] corners = new Vector3[8]
        {
            new Vector3(localMin.X, localMin.Y, localMin.Z),
            new Vector3(localMax.X, localMin.Y, localMin.Z),
            new Vector3(localMin.X, localMax.Y, localMin.Z),
            new Vector3(localMax.X, localMax.Y, localMin.Z),
            new Vector3(localMin.X, localMin.Y, localMax.Z),
            new Vector3(localMax.X, localMin.Y, localMax.Z),
            new Vector3(localMin.X, localMax.Y, localMax.Z),
            new Vector3(localMax.X, localMax.Y, localMax.Z)
        };
        
        // Transform first corner to initialize min/max
        Vector3 transformedCorner = Vector3.Transform(corners[0], modelMatrix);
        min = transformedCorner;
        max = transformedCorner;
        
        // Transform remaining corners and expand bounds
        for (int i = 1; i < corners.Length; i++)
        {
            transformedCorner = Vector3.Transform(corners[i], modelMatrix);
            min = Vector3.Min(min, transformedCorner);
            max = Vector3.Max(max, transformedCorner);
        }
    }
    
    /// <summary>
    /// Transform all vertices by a matrix / 通过矩阵变换所有顶�?    /// </summary>
    public void Transform(Matrix4x4 matrix)
    {
        if (Vertices == null)
            return;
            
        for (int i = 0; i < Vertices.Length; i++)
        {
            Vertices[i] = Vector3.Transform(Vertices[i], matrix);
        }
        
        // Transform normals (use inverse transpose for proper normal transformation)
        if (Normals != null)
        {
            for (int i = 0; i < Normals.Length; i++)
            {
                Normals[i] = Vector3.TransformNormal(Normals[i], matrix);
                Normals[i] = Vector3.Normalize(Normals[i]);
            }
        }
    }
    
    /// <summary>
    /// Dispose mesh resources / 释放网格资源
    /// </summary>
    public virtual void Dispose()
    {
        if (_disposed)
            return;
            
        Vertices = null;
        Indices = null;
        Colors = null;
        TexCoords = null;
        Normals = null;
        
        _disposed = true;
        GC.SuppressFinalize(this);
    }
    
    private static Vector4[] CreateDefaultColors(int count)
    {
        Vector4[] colors = new Vector4[count];
        for (int i = 0; i < count; i++)
            colors[i] = Vector4.One;
        return colors;
    }
    
    private static Vector2[] CreateDefaultTexCoords(int count)
    {
        Vector2[] texCoords = new Vector2[count];
        for (int i = 0; i < count; i++)
            texCoords[i] = Vector2.Zero;
        return texCoords;
    }
    
    private static Vector3[] CreateDefaultNormals(int count)
    {
        Vector3[] normals = new Vector3[count];
        for (int i = 0; i < count; i++)
            normals[i] = new Vector3(0, 0, 1);
        return normals;
    }

    /// <summary>
    /// 转换 Vector4 颜色数组�?Color 数组（用于接口兼容性）
    /// </summary>
    private static Color[] ConvertVector4ToColor(Vector4[] colors)
    {
        if (colors == null || colors.Length == 0)
            return Array.Empty<Color>();
        
        Color[] result = new Color[colors.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            result[i] = Color.FromArgb(
                (int)(colors[i].W * 255),  // Alpha
                (int)(colors[i].X * 255),  // Red
                (int)(colors[i].Y * 255),  // Green
                (int)(colors[i].Z * 255)   // Blue
            );
        }
        return result;
    }

    /// <summary>
    /// 转换 Color 数组�?Vector4 颜色数组（用于接口兼容性）
    /// </summary>
    private static Vector4[] ConvertColorToVector4(Color[] colors)
    {
        if (colors == null || colors.Length == 0)
            return Array.Empty<Vector4>();
        
        Vector4[] result = new Vector4[colors.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            result[i] = new Vector4(
                colors[i].R / 255.0f,
                colors[i].G / 255.0f,
                colors[i].B / 255.0f,
                colors[i].A / 255.0f
            );
        }
        return result;
    }
    
    ~Mesh()
    {
        Dispose();
    }
}


