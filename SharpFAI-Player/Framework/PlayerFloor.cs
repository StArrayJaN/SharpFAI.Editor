using System.Numerics;
using SharpFAI.Framework;

namespace SharpFAI_Player.Framework;

public class PlayerFloor : IDisposable
{
    private GLMesh mesh;
    public readonly Floor floor;
    public bool isHit;
    public bool isSelected; // 是否被选中
    private bool _disposed;
    private Vector4[] _originalColors; // 保存原始颜色
    
    public PlayerFloor(Floor floor)
    {
        this.floor = floor;
        var poly = floor.GeneratePolygon();
        _originalColors = new Vector4[poly.colors.Length];
        for (int i = 0; i < poly.colors.Length; i++)
        {
            _originalColors[i] = new Vector4(
                poly.colors[i].R / 255.0f,
                poly.colors[i].G / 255.0f,
                poly.colors[i].B / 255.0f,
                poly.colors[i].A / 255.0f
            );
        }
        mesh = new (poly.vertices, poly.triangles.Select(a => (int)a).ToArray(), _originalColors)
        {
            Position = new(floor.position.X, floor.position.Y, 0)
        };
    }
    
    /// <summary>
    /// 设置选中状态，选中时显示为绿色
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (isSelected == selected) return;
        isSelected = selected;
        
        // 更新颜色
        var colors = new Vector4[_originalColors.Length];
        for (int i = 0; i < _originalColors.Length; i++)
        {
            if (selected)
            {
                // 选中时：将白色部分变为绿色，保持黑色边框
                if (_originalColors[i].X > 0.5f && _originalColors[i].Y > 0.5f && _originalColors[i].Z > 0.5f)
                {
                    // 白色部分变为绿色
                    colors[i] = new Vector4(0.2f, 0.8f, 0.2f, 1.0f);
                }
                else
                {
                    // 保持黑色边框
                    colors[i] = _originalColors[i];
                }
            }
            else
            {
                // 未选中时恢复原始颜色
                colors[i] = _originalColors[i];
            }
        }
        
        mesh.UpdateColors(colors);
    }
    
    public void Render(IShader shader)
    {
        if (isHit) return;
        mesh.Render(shader);
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;
            
        mesh?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}