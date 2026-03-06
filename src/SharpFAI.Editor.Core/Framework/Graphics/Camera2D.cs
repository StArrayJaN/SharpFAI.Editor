using System.Numerics;
using SharpFAI.Framework;
using SharpFAI.Util;

namespace SharpFAI.Editor.Core.Framework.Graphics;
public class Camera2D : ICamera
{
    /// <summary>
    /// Camera position in world space / 相机在世界空间的位置
    /// </summary>
    public Vector2 Position { get; set; }
    
    /// <summary>
    /// Camera rotation in degrees / 相机旋转角度
    /// </summary>
    public float Rotation { get; set; }
    
    /// <summary>
    /// Camera zoom level / 相机缩放级别
    /// </summary>
    public float Zoom { get; set; }
    
    /// <summary>
    /// Viewport width / 视口宽度
    /// </summary>
    public float ViewportWidth { get; set; }
    
    /// <summary>
    /// Viewport height / 视口高度
    /// </summary>
    public float ViewportHeight { get; set; }
    
    /// <summary>
    /// Near clipping plane / 近裁剪平面
    /// </summary>
    public float NearPlane { get; set; }
    
    /// <summary>
    /// Far clipping plane / 远裁剪平面
    /// </summary>
    public float FarPlane { get; set; }
    
    // Movement animation
    private Vector2 _targetPosition;
    private float _moveProgress;
    private float _moveDuration;
    private bool _isMoving;
    
    // Shake effect
    private float _shakeIntensity;
    private float _shakeDuration;
    private float _shakeTimeRemaining;
    private Vector2 _shakeOffset;
    private Random _random;

    /// <summary>
    /// Create a 2D camera with default settings
    /// 创建具有默认设置的2D相机
    /// </summary>
    public Camera2D(float viewportWidth = 1920, float viewportHeight = 1080)
    {
        Position = Vector2.Zero;
        Rotation = 0;
        Zoom = 1.0f;
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
        NearPlane = -100.0f;
        FarPlane = 100.0f;
        
        _random = new Random();
        Reset();
    }
    
    /// <summary>
    /// Get the view matrix / 获取视图矩阵
    /// </summary>
    public Matrix4x4 GetViewMatrix()
    {
        Vector2 effectivePosition = Position + _shakeOffset;
        
        // Create view matrix: T * R * S
        Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(
            -effectivePosition.X,
            -effectivePosition.Y,
            0
        );
        
        Matrix4x4 rotationMatrix = Matrix4x4.CreateRotationZ(
            -Rotation * FloatMath.PI / 180.0f
        );
        
        // Inverted zoom: larger Zoom value = smaller scale (zoomed out)
        // 反转缩放：较大的Zoom值 = 较小的缩放（缩小）
        float inverseZoom = 1.0f / Zoom;
        Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(inverseZoom, inverseZoom, 1.0f);
        
        return translationMatrix * rotationMatrix * scaleMatrix;
    }
    
    /// <summary>
    /// Get the projection matrix / 获取投影矩阵
    /// </summary>
    public Matrix4x4 GetProjectionMatrix()
    {
        float halfWidth = ViewportWidth / 2.0f;
        float halfHeight = ViewportHeight / 2.0f;
        
        return Matrix4x4.CreateOrthographicOffCenter(
            -halfWidth,
            halfWidth,
            -halfHeight,
            halfHeight,
            NearPlane,
            FarPlane
        );
    }
    
    /// <summary>
    /// Update camera state / 更新相机状态
    /// </summary>
    public void Update(float deltaTime)
    {
        // Update movement animation
        if (_isMoving)
        {
            _moveProgress += deltaTime / _moveDuration;
            
            if (_moveProgress >= 1.0f)
            {
                _moveProgress = 1.0f;
                _isMoving = false;
            }
            
            float t = _moveProgress;
            Position = Vector2.Lerp(Position, _targetPosition, t);
        }
        
        // Update shake effect
        if (_shakeTimeRemaining > 0)
        {
            _shakeTimeRemaining -= deltaTime;
            
            if (_shakeTimeRemaining <= 0)
            {
                _shakeOffset = Vector2.Zero;
            }
            else
            {
                float shakeStrength = _shakeIntensity * (_shakeTimeRemaining / _shakeDuration);
                _shakeOffset = new Vector2(
                    ((float)_random.NextDouble() * 2 - 1) * shakeStrength,
                    ((float)_random.NextDouble() * 2 - 1) * shakeStrength
                );
            }
        }
    }
    
    
    
    /// <summary>
    /// Set camera to follow a target / 设置相机跟随目标
    /// </summary>
    public void SetTarget(Vector2 target)
    {
        Position = target;
    }
    
    /// <summary>
    /// Reset camera to default state / 重置相机到默认状态
    /// </summary>
    public void Reset()
    {
        Position = Vector2.Zero;
        Rotation = 0;
        Zoom = 1.0f;
        _isMoving = false;
        _shakeTimeRemaining = 0;
        _shakeOffset = Vector2.Zero;
    }
    
    /// <summary>
    /// Apply screen shake effect / 应用屏幕震动效果
    /// </summary>
    public void Shake(float intensity, float duration)
    {
        _shakeIntensity = intensity;
        _shakeDuration = duration;
        _shakeTimeRemaining = duration;
    }
    
    /// <summary>
    /// Convert screen coordinates to world coordinates / 将屏幕坐标转换为世界坐标
    /// </summary>
    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        // Normalize screen coordinates to [-1, 1]
        float normalizedX = (screenPosition.X / ViewportWidth) * 2 - 1;
        float normalizedY = 1 - (screenPosition.Y / ViewportHeight) * 2;
        
        // Apply inverse projection
        float worldX = normalizedX * (ViewportWidth / 2.0f) / Zoom;
        float worldY = normalizedY * (ViewportHeight / 2.0f) / Zoom;
        
        // Apply rotation
        float rotRad = Rotation * FloatMath.PI / 180.0f;
        float cosRot = FloatMath.Cos(rotRad);
        float sinRot = FloatMath.Sin(rotRad);
        
        float rotatedX = worldX * cosRot - worldY * sinRot;
        float rotatedY = worldX * sinRot + worldY * cosRot;
        
        // Apply camera position
        return new Vector2(
            rotatedX + Position.X,
            rotatedY + Position.Y
        );
    }
    
    /// <summary>
    /// Convert world coordinates to screen coordinates / 将世界坐标转换为屏幕坐标
    /// </summary>
    public Vector2 WorldToScreen(Vector2 worldPosition)
    {
        // Translate to camera space
        float relativeX = worldPosition.X - Position.X;
        float relativeY = worldPosition.Y - Position.Y;
        
        // Apply rotation
        float rotRad = -Rotation * FloatMath.PI / 180.0f;
        float cosRot = FloatMath.Cos(rotRad);
        float sinRot = FloatMath.Sin(rotRad);
        
        float rotatedX = relativeX * cosRot - relativeY * sinRot;
        float rotatedY = relativeX * sinRot + relativeY * cosRot;
        
        // Apply inverse zoom
        // With inverted zoom: divide by Zoom instead of multiply
        // 使用反转缩放：除以Zoom而不是乘以
        rotatedX /= Zoom;
        rotatedY /= Zoom;
        
        // Convert to screen space
        float screenX = (rotatedX / (ViewportWidth / 2.0f) + 1) * ViewportWidth / 2.0f;
        float screenY = (1 - rotatedY / (ViewportHeight / 2.0f)) * ViewportHeight / 2.0f;
        
        return new Vector2(screenX, screenY);
    }
    
    /// <summary>
    /// Apply camera matrices to shader / 将相机矩阵应用到着色器
    /// </summary>
    /// <param name="shader">Shader to apply matrices to / 要应用矩阵的着色器</param>
    public void Render(IShader shader)
    {
        if (shader == null)
            throw new ArgumentNullException(nameof(shader));
            
        shader.SetMatrix4x4("uView", GetViewMatrix());
        shader.SetMatrix4x4("uProjection", GetProjectionMatrix());
    }
    
    /// <summary>
    /// Get the frustum bounds in world space / 获取视锥体在世界空间的边界
    /// </summary>
    public void GetFrustumBounds(out Vector2 min, out Vector2 max)
    {
        // Calculate the half-size of the viewport in world units
        // With inverted zoom: larger Zoom = larger visible area
        // 使用反转缩放：较大的Zoom = 较大的可见区域
        float halfWidth = ViewportWidth * Zoom / 2.0f;
        float halfHeight = ViewportHeight * Zoom / 2.0f;
        
        if (Rotation == 0)
        {
            // Simple case: no rotation
            min = new Vector2(Position.X - halfWidth, Position.Y - halfHeight);
            max = new Vector2(Position.X + halfWidth, Position.Y + halfHeight);
        }
        else
        {
            // Complex case: with rotation
            // Calculate the four corners of the frustum
            float rotRad = Rotation * FloatMath.PI / 180.0f;
            float cosRot = FloatMath.Cos(rotRad);
            float sinRot = FloatMath.Sin(rotRad);
            
            Vector2[] corners = new Vector2[4];
            corners[0] = RotatePoint(new Vector2(-halfWidth, -halfHeight), cosRot, sinRot);
            corners[1] = RotatePoint(new Vector2(halfWidth, -halfHeight), cosRot, sinRot);
            corners[2] = RotatePoint(new Vector2(-halfWidth, halfHeight), cosRot, sinRot);
            corners[3] = RotatePoint(new Vector2(halfWidth, halfHeight), cosRot, sinRot);
            
            // Find the AABB of the rotated frustum
            min = new Vector2(float.MaxValue, float.MaxValue);
            max = new Vector2(float.MinValue, float.MinValue);
            
            foreach (var corner in corners)
            {
                Vector2 worldCorner = corner + Position;
                min.X = FloatMath.Min(min.X, worldCorner.X);
                min.Y = FloatMath.Min(min.Y, worldCorner.Y);
                max.X = FloatMath.Max(max.X, worldCorner.X);
                max.Y = FloatMath.Max(max.Y, worldCorner.Y);
            }
        }
    }
    
    /// <summary>
    /// Check if a point is visible in the camera frustum / 检查点是否在相机视锥体内
    /// </summary>
    public bool IsPointVisible(Vector2 point)
    {
        // Calculate the half-size of the viewport in world units
        // With inverted zoom: larger Zoom = larger visible area
        // 使用反转缩放：较大的Zoom = 较大的可见区域
        float halfWidth = ViewportWidth * Zoom / 2.0f;
        float halfHeight = ViewportHeight * Zoom / 2.0f;
        
        // Transform point to camera space
        Vector2 relativePos = point - Position;
        
        if (Rotation != 0)
        {
            // Apply inverse rotation
            float rotRad = -Rotation * FloatMath.PI / 180.0f;
            float cosRot = FloatMath.Cos(rotRad);
            float sinRot = FloatMath.Sin(rotRad);
            relativePos = RotatePoint(relativePos, cosRot, sinRot);
        }
        
        // Check if point is within frustum bounds
        return relativePos.X >= -halfWidth && relativePos.X <= halfWidth &&
               relativePos.Y >= -halfHeight && relativePos.Y <= halfHeight;
    }
    
    /// <summary>
    /// Check if a circle is visible in the camera frustum / 检查圆形是否在相机视锥体内
    /// </summary>
    public bool IsCircleVisible(Vector2 center, float radius)
    {
        // Calculate the half-size of the viewport in world units
        // With inverted zoom: larger Zoom = larger visible area
        // 使用反转缩放：较大的Zoom = 较大的可见区域
        float halfWidth = ViewportWidth * Zoom / 2.0f;
        float halfHeight = ViewportHeight * Zoom / 2.0f;
        
        // Transform center to camera space
        Vector2 relativePos = center - Position;
        
        if (Rotation != 0)
        {
            // Apply inverse rotation
            float rotRad = -Rotation * FloatMath.PI / 180.0f;
            float cosRot = FloatMath.Cos(rotRad);
            float sinRot = FloatMath.Sin(rotRad);
            relativePos = RotatePoint(relativePos, cosRot, sinRot);
        }
        
        // Check if circle intersects with frustum (expanded by radius)
        return relativePos.X + radius >= -halfWidth && relativePos.X - radius <= halfWidth &&
               relativePos.Y + radius >= -halfHeight && relativePos.Y - radius <= halfHeight;
    }
    
    /// <summary>
    /// Check if an axis-aligned bounding box is visible in the camera frustum / 检查轴对齐包围盒是否在相机视锥体内
    /// </summary>
    public bool IsAABBVisible(Vector2 min, Vector2 max)
    {
        if (Rotation == 0)
        {
            // Simple case: no rotation
            GetFrustumBounds(out Vector2 frustumMin, out Vector2 frustumMax);
            
            // AABB overlap test
            return !(max.X < frustumMin.X || min.X > frustumMax.X ||
                    max.Y < frustumMin.Y || min.Y > frustumMax.Y);
        }

        // Complex case: with rotation
        // Test if any corner of the AABB is visible, or if the AABB contains the camera
        Vector2[] corners = new Vector2[4]
        {
            new Vector2(min.X, min.Y),
            new Vector2(max.X, min.Y),
            new Vector2(min.X, max.Y),
            new Vector2(max.X, max.Y)
        };
            
        // Check if any corner is visible
        foreach (var corner in corners)
        {
            if (IsPointVisible(corner))
                return true;
        }
            
        // Check if the AABB contains the camera center
        if (min.X <= Position.X && Position.X <= max.X &&
            min.Y <= Position.Y && Position.Y <= max.Y)
            return true;
            
        // Check if frustum corners are inside the AABB
        // With inverted zoom: larger Zoom = larger visible area
        // 使用反转缩放：较大的Zoom = 较大的可见区域
        float halfWidth = ViewportWidth * Zoom / 2.0f;
        float halfHeight = ViewportHeight * Zoom / 2.0f;
            
        float rotRad = Rotation * FloatMath.PI / 180.0f;
        float cosRot = FloatMath.Cos(rotRad);
        float sinRot = FloatMath.Sin(rotRad);
            
        Vector2[] frustumCorners = new Vector2[4];
        frustumCorners[0] = RotatePoint(new Vector2(-halfWidth, -halfHeight), cosRot, sinRot) + Position;
        frustumCorners[1] = RotatePoint(new Vector2(halfWidth, -halfHeight), cosRot, sinRot) + Position;
        frustumCorners[2] = RotatePoint(new Vector2(-halfWidth, halfHeight), cosRot, sinRot) + Position;
        frustumCorners[3] = RotatePoint(new Vector2(halfWidth, halfHeight), cosRot, sinRot) + Position;
            
        foreach (var corner in frustumCorners)
        {
            if (corner.X >= min.X && corner.X <= max.X &&
                corner.Y >= min.Y && corner.Y <= max.Y)
                return true;
        }
            
        return false;
    }
    
    /// <summary>
    /// Helper method to rotate a point / 旋转点的辅助方法
    /// </summary>
    private Vector2 RotatePoint(Vector2 point, float cosRot, float sinRot)
    {
        return new Vector2(
            point.X * cosRot - point.Y * sinRot,
            point.X * sinRot + point.Y * cosRot
        );
    }
}

