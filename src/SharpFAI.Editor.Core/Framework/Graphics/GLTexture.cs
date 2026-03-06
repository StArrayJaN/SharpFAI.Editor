/*
using System;
using System.Numerics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using OpenTK.Graphics.OpenGL4;
using SharpFAI.Framework;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace SharpFAI.Editor.Core.Framework.Graphics;

/// <summary>
/// OpenGL texture implementation
/// OpenGL纹理实现
/// </summary>
public class GLTexture : ITexture
{
    private int _textureId;
    private int _originalWidth;
    private int _originalHeight;
    private int _maxWidth;
    private int _maxHeight;
    
    public int Width { get; private set; }
    public int Height { get; private set; }
    public bool IsLoaded { get; private set; }
    public void Load(string path)
    {
        _texturePath = path;
        Load();
    }

    public void Update(Vector2 offset, Vector2 size, byte[] rgba)
    {
        if (!IsLoaded || _textureId <= 0)
            return;
            
        GL.BindTexture(TextureTarget.Texture2D, _textureId);
        GL.TexSubImage2D(
            TextureTarget.Texture2D,
            0,                          // mipmap level
            (int)offset.X,              // x offset
            (int)offset.Y,              // y offset
            (int)size.X,                // width
            (int)size.Y,                // height
            PixelFormat.Rgba,           // format
            PixelType.UnsignedByte,     // data type
            rgba                        // pixel data
        );
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void Bind(IShader shader)
    {
        if (!IsLoaded || _textureId <= 0)
            return;
            
        // Activate texture unit 0 (default for most basic shaders)
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _textureId);
        
        // Set the texture uniform in the shader if provided
        if (shader != null)
        {
            shader.SetInt("uTexture", 0); // Set texture sampler to use texture unit 0
            shader.SetInt("uUseTexture", 1); // Enable texture sampling in the shader
        }
    }

    public void Unbind()
    {
        GL.BindTexture(TextureTarget.Texture2D, 0);
        
        // Reset texture usage flag in the currently bound shader (if any)
        // This is a bit of a hack, but it's the simplest way to ensure the shader knows
        // that we're no longer using a texture
        try
        {
            int currentProgram;
            GL.GetInteger(GetPName.CurrentProgram, out currentProgram);
            if (currentProgram > 0)
            {
                int location = GL.GetUniformLocation(currentProgram, "uUseTexture");
                if (location >= 0)
                {
                    GL.Uniform1(location, 0); // Disable texture sampling
                }
            }
        }
        catch (Exception ex)
        {
            // Ignore errors here, as this is just a cleanup step
            System.Diagnostics.Debug.WriteLine($"Error resetting uUseTexture: {ex.Message}");
        }
    }

    public void SetFilter(TextureFilter min, TextureFilter mag)
    {
        if (!IsLoaded || _textureId <= 0)
            return;
            
        GL.BindTexture(TextureTarget.Texture2D, _textureId);
        
        // Set minification filter
        GL.TexParameter(TextureTarget.Texture2D, 
            TextureParameterName.TextureMinFilter, 
            (int)(min == TextureFilter.Linear ? TextureMinFilter.Linear : TextureMinFilter.Nearest));
        
        // Set magnification filter
        GL.TexParameter(TextureTarget.Texture2D, 
            TextureParameterName.TextureMagFilter, 
            (int)(mag == TextureFilter.Linear ? TextureMagFilter.Linear : TextureMagFilter.Nearest));
            
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void SetWrap(TextureWrap s, TextureWrap t)
    {
        if (!IsLoaded || _textureId <= 0)
            return;
            
        GL.BindTexture(TextureTarget.Texture2D, _textureId);
        
        // Set horizontal wrapping mode
        GL.TexParameter(TextureTarget.Texture2D, 
            TextureParameterName.TextureWrapS, 
            (int)(s == TextureWrap.Repeat ? TextureWrapMode.Repeat : TextureWrapMode.ClampToEdge));
        
        // Set vertical wrapping mode
        GL.TexParameter(TextureTarget.Texture2D, 
            TextureParameterName.TextureWrapT, 
            (int)(t == TextureWrap.Repeat ? TextureWrapMode.Repeat : TextureWrapMode.ClampToEdge));
            
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void Dispose()
    {
        if (_textureId > 0)
        {
            GL.DeleteTexture(_textureId);
            _textureId = 0;
        }
        IsLoaded = false;
    }

    private string _texturePath;
    
    /// <summary>
    /// Create an empty GL texture / 创建空GL纹理
    /// </summary>
    public GLTexture()
    {
        _textureId = 0;
        Width = 0;
        Height = 0;
        IsLoaded = false;
        _texturePath = string.Empty;
    }
    
    /// <summary>
    /// Create a GL texture with path and load immediately
    /// 使用路径创建GL纹理并立即加�?    /// </summary>
    public GLTexture(string path, bool autoLoad = false)
    {
        _textureId = 0;
        Width = 0;
        Height = 0;
        IsLoaded = false;
        _texturePath = path;
        if (autoLoad)
        {
            try
            {
                Load();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load texture: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Set maximum texture size and scale accordingly
    /// 设置最大纹理大小并相应缩放
    /// </summary>
    public void SetMaxSize(int maxWidth, int maxHeight)
    {
        _maxWidth = maxWidth;
        _maxHeight = maxHeight;
        ScaleToMaxSize();
    }
    
    /// <summary>
    /// Scale texture dimensions to fit within max size while maintaining aspect ratio
    /// 缩放纹理尺寸以适应最大大小，同时保持宽高�?    /// </summary>
    private void ScaleToMaxSize()
    {
        if (_maxWidth <= 0 || _maxHeight <= 0 || _originalWidth <= 0 || _originalHeight <= 0)
            return;
        
        if (_originalWidth > _maxWidth || _originalHeight > _maxHeight)
        {
            float scaleX = (float)_maxWidth / _originalWidth;
            float scaleY = (float)_maxHeight / _originalHeight;
            float scale = Math.Min(scaleX, scaleY);
            
            Width = (int)(_originalWidth * scale);
            Height = (int)(_originalHeight * scale);
        }
        else
        {
            Width = _originalWidth;
            Height = _originalHeight;
        }
    }
    
    /// <summary>
    /// Load texture from stored path / 从存储的路径加载纹理
    /// </summary>
    public void Load()
    {
        if (IsLoaded) return;
        
        if (string.IsNullOrEmpty(_texturePath))
            throw new InvalidOperationException("Texture path is not set");
            
        try
        {
            // Delete existing texture if any
            if (_textureId > 0)
            {
                GL.DeleteTexture(_textureId);
                _textureId = 0;
            }
            
            // Generate new texture ID
            _textureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _textureId);
            
            // Load image from file
            using (var bitmap = new Bitmap(_texturePath))
            {
                // Store original dimensions
                _originalWidth = bitmap.Width;
                _originalHeight = bitmap.Height;
                
                // Apply max size constraints if set
                ScaleToMaxSize();
                
                // If scaling is needed
                Bitmap resizedBitmap = bitmap;
                if ((Width != _originalWidth || Height != _originalHeight) && Width > 0 && Height > 0)
                {
                    try
                    {
                        // Create a new bitmap with the desired size
                        resizedBitmap = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        
                        // Use Graphics to draw the original bitmap onto the new one with scaling
                        using (Graphics g = Graphics.FromImage(resizedBitmap))
                        {
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.DrawImage(bitmap, 0, 0, Width, Height);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error resizing bitmap: {ex.Message}");
                        // Fall back to original bitmap
                        resizedBitmap = bitmap;
                        Width = _originalWidth;
                        Height = _originalHeight;
                    }
                }
                
                // Lock bitmap data and upload to GPU
                BitmapData data = resizedBitmap.LockBits(
                    new Rectangle(0, 0, Width, Height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                
                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    0,                          // mipmap level
                    PixelInternalFormat.Rgba,   // internal format
                    Width,                      // width
                    Height,                     // height
                    0,                          // border
                    PixelFormat.Bgra,           // format (BGRA for Windows bitmaps)
                    PixelType.UnsignedByte,     // data type
                    data.Scan0                  // pixel data
                );
                
                // Unlock bitmap data
                resizedBitmap.UnlockBits(data);
                
                // Clean up if we created a resized bitmap
                if (resizedBitmap != bitmap)
                    resizedBitmap.Dispose();
            }
            
            // Set default filtering and wrapping modes
            SetFilter(TextureFilter.Linear, TextureFilter.Linear);
            SetWrap(TextureWrap.ClampToEdge, TextureWrap.ClampToEdge);
            
            // Generate mipmaps for better quality when scaling down
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            
            // Unbind texture
            GL.BindTexture(TextureTarget.Texture2D, 0);
            
            IsLoaded = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load texture '{_texturePath}': {ex.Message}");
            if (_textureId > 0)
            {
                GL.DeleteTexture(_textureId);
                _textureId = 0;
            }
            IsLoaded = false;
            throw;
        }
    }
  
}
*/

