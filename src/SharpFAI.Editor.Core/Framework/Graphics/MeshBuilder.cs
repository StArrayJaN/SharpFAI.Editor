using System.Drawing;
using System.Numerics;

namespace SharpFAI.Editor.Core.Framework.Graphics;

/// <summary>
/// Helper class for building complex meshes
/// 用于构建复杂网格的辅助类
/// </summary>
public class MeshBuilder
{
    private List<Vector3> _vertices = new();
    private List<int> _indices = new();
    private List<Color> _colors = new();
    private List<Vector2> _texCoords = new();
    private List<Vector3> _normals = new();

    /// <summary>
    /// Add a vertex to the mesh / 向网格添加顶�?
    /// </summary>
    public void AddVertex(Vector3 position, Color? color = null, Vector2? texCoord = null, Vector3? normal = null)
    {
        _vertices.Add(position);
        _colors.Add(color ?? Color.White);
        _texCoords.Add(texCoord ?? Vector2.Zero);
        _normals.Add(normal ?? new Vector3(0, 0, 1));
    }

    /// <summary>
    /// Add a triangle by vertex indices / 通过顶点索引添加三角�?
    /// </summary>
    public void AddTriangle(int index0, int index1, int index2)
    {
        _indices.Add(index0);
        _indices.Add(index1);
        _indices.Add(index2);
    }

    /// <summary>
    /// Add a quad by vertex indices / 通过顶点索引添加四边�?
    /// </summary>
    public void AddQuad(int index0, int index1, int index2, int index3)
    {
        // First triangle
        _indices.Add(index0);
        _indices.Add(index1);
        _indices.Add(index2);
        
        // Second triangle
        _indices.Add(index2);
        _indices.Add(index3);
        _indices.Add(index0);
    }

    /// <summary>
    /// Clear all mesh data / 清除所有网格数�?
    /// </summary>
    public void Clear()
    {
        _vertices.Clear();
        _indices.Clear();
        _colors.Clear();
        _texCoords.Clear();
        _normals.Clear();
    }

    /// <summary>
    /// Build and return the mesh / 构建并返回网�?
    /// </summary>
    public Mesh Build()
    {
        // 转换 Color[] �?Vector4[]
        var colors = new Vector4[_colors.Count];
        for (int i = 0; i < _colors.Count; i++)
        {
            colors[i] = new Vector4(
                _colors[i].R / 255.0f,
                _colors[i].G / 255.0f,
                _colors[i].B / 255.0f,
                _colors[i].A / 255.0f
            );
        }

        return new Mesh(
            _vertices.ToArray(),
            _indices.ToArray(),
            colors,
            _texCoords.ToArray(),
            _normals.ToArray()
        );
    }

    /// <summary>
    /// Create a line mesh / 创建线条网格
    /// </summary>
    public static Mesh CreateLine(Vector2 start, Vector2 end, float thickness = 1.0f, Color? color = null)
    {
        Color lineColor = color ?? Color.White;
        MeshBuilder builder = new MeshBuilder();

        Vector2 direction = Vector2.Normalize(end - start);
        Vector2 perpendicular = new Vector2(-direction.Y, direction.X) * thickness / 2.0f;

        // Create four corners of the line rectangle
        builder.AddVertex(new Vector3(start - perpendicular, 0), lineColor);
        builder.AddVertex(new Vector3(start + perpendicular, 0), lineColor);
        builder.AddVertex(new Vector3(end + perpendicular, 0), lineColor);
        builder.AddVertex(new Vector3(end - perpendicular, 0), lineColor);

        builder.AddQuad(0, 1, 2, 3);

        return builder.Build();
    }

    /// <summary>
    /// Create a rectangle mesh / 创建矩形网格
    /// </summary>
    public static Mesh CreateRectangle(float x, float y, float width, float height, Color? color = null)
    {
        Color rectColor = color ?? Color.White;
        MeshBuilder builder = new MeshBuilder();

        builder.AddVertex(new Vector3(x, y, 0), rectColor, new Vector2(0, 0));
        builder.AddVertex(new Vector3(x + width, y, 0), rectColor, new Vector2(1, 0));
        builder.AddVertex(new Vector3(x + width, y + height, 0), rectColor, new Vector2(1, 1));
        builder.AddVertex(new Vector3(x, y + height, 0), rectColor, new Vector2(0, 1));

        builder.AddQuad(0, 1, 2, 3);

        return builder.Build();
    }

    /// <summary>
    /// Create a ring mesh / 创建圆环网格
    /// </summary>
    public static Mesh CreateRing(float innerRadius, float outerRadius, int segments = 32, Color? color = null)
    {
        Color ringColor = color ?? Color.White;
        MeshBuilder builder = new MeshBuilder();

        for (int i = 0; i < segments; i++)
        {
            float angle = 2.0f * MathF.PI * i / segments;
            float cos = MathF.Cos(angle);
            float sin = MathF.Sin(angle);

            // Inner vertex
            builder.AddVertex(new Vector3(cos * innerRadius, sin * innerRadius, 0), ringColor);
            // Outer vertex
            builder.AddVertex(new Vector3(cos * outerRadius, sin * outerRadius, 0), ringColor);
        }

        for (int i = 0; i < segments; i++)
        {
            int nextIndex = (i + 1) % segments;
            
            int innerCurrent = i * 2;
            int outerCurrent = i * 2 + 1;
            int innerNext = nextIndex * 2;
            int outerNext = nextIndex * 2 + 1;

            builder.AddQuad(innerCurrent, outerCurrent, outerNext, innerNext);
        }

        return builder.Build();
    }

    /// <summary>
    /// Create a polygon mesh / 创建多边形网�?
    /// </summary>
    public static Mesh CreatePolygon(Vector2[] points, Color? color = null)
    {
        if (points == null || points.Length < 3)
            throw new ArgumentException("Polygon must have at least 3 points");

        Color polyColor = color ?? Color.White;
        MeshBuilder builder = new MeshBuilder();

        // Calculate centroid for triangulation
        Vector2 centroid = Vector2.Zero;
        foreach (var point in points)
        {
            centroid += point;
        }
        centroid /= points.Length;

        // Add centroid as first vertex
        builder.AddVertex(new Vector3(centroid, 0), polyColor);

        // Add all polygon points
        foreach (var point in points)
        {
            builder.AddVertex(new Vector3(point, 0), polyColor);
        }

        // Create triangles from centroid to each edge
        for (int i = 0; i < points.Length; i++)
        {
            int current = i + 1;
            int next = (i + 1) % points.Length + 1;
            builder.AddTriangle(0, current, next);
        }

        return builder.Build();
    }

    /// <summary>
    /// Create an arc mesh / 创建弧形网格
    /// </summary>
    public static Mesh CreateArc(float radius, float startAngle, float endAngle, int segments = 32, Color? color = null)
    {
        Color arcColor = color ?? Color.White;
        MeshBuilder builder = new MeshBuilder();

        // Center vertex
        builder.AddVertex(Vector3.Zero, arcColor);

        // Arc vertices
        float angleStep = (endAngle - startAngle) / segments;
        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + angleStep * i;
            float cos = MathF.Cos(angle * MathF.PI / 180.0f);
            float sin = MathF.Sin(angle * MathF.PI / 180.0f);
            builder.AddVertex(new Vector3(cos * radius, sin * radius, 0), arcColor);
        }

        // Create triangles
        for (int i = 0; i < segments; i++)
        {
            builder.AddTriangle(0, i + 1, i + 2);
        }

        return builder.Build();
    }

    /// <summary>
    /// Create a star mesh / 创建星形网格
    /// </summary>
    public static Mesh CreateStar(float outerRadius, float innerRadius, int points = 5, Color? color = null)
    {
        Color starColor = color ?? Color.White;
        MeshBuilder builder = new MeshBuilder();

        // Center vertex
        builder.AddVertex(Vector3.Zero, starColor);

        // Create alternating outer and inner points
        for (int i = 0; i < points * 2; i++)
        {
            float angle = MathF.PI * i / points;
            float radius = i % 2 == 0 ? outerRadius : innerRadius;
            float cos = MathF.Cos(angle);
            float sin = MathF.Sin(angle);
            builder.AddVertex(new Vector3(cos * radius, sin * radius, 0), starColor);
        }

        // Create triangles from center
        for (int i = 0; i < points * 2; i++)
        {
            int current = i + 1;
            int next = (i + 1) % (points * 2) + 1;
            builder.AddTriangle(0, current, next);
        }

        return builder.Build();
    }
}




