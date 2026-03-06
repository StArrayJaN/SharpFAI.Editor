using System.Runtime.CompilerServices;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace SharpFAI.Editor.Core.Framework.Graphics;

/// <summary>
/// Standalone ImGui gradient text renderer with embedded shaders
/// 独立�?ImGui 渐变文字渲染器，内嵌着色器
/// </summary>
public class ImGuiGradientTextRenderer : IDisposable
{
    private bool _disposed;
    private int _vertexArray;
    private int _vertexBuffer;
    private int _vertexBufferSize;
    private int _indexBuffer;
    private int _indexBufferSize;
    private int _fontTexture;
    
    private int _standardShader;
    private int _standardShaderFontTextureLocation;
    private int _standardShaderProjectionMatrixLocation;
    
    private int _gradientShader;
    private int _gradientShaderFontTextureLocation;
    private int _gradientShaderProjectionMatrixLocation;
    private int _gradientShaderTimeLocation;
    private int _gradientShaderModeLocation;

    private int _windowWidth;
    private int _windowHeight;
    private float _time = 0f;
    private bool _useGradientShader = false;
    private int _gradientMode = 3; // 0=horizontal, 1=vertical, 2=diagonal, 3=rainbow wave

    // Embedded shaders / 内嵌着色器
    private const string STANDARD_VERTEX_SHADER = @"#version 330 core
uniform mat4 projection_matrix;
layout(location = 0) in vec2 in_position;
layout(location = 1) in vec2 in_texCoord;
layout(location = 2) in vec4 in_color;
out vec4 color;
out vec2 texCoord;
void main()
{
    gl_Position = projection_matrix * vec4(in_position, 0, 1);
    color = in_color;
    texCoord = in_texCoord;
}";

    private const string STANDARD_FRAGMENT_SHADER = @"#version 330 core
uniform sampler2D in_fontTexture;
in vec4 color;
in vec2 texCoord;
out vec4 outputColor;
void main()
{
    outputColor = color * texture(in_fontTexture, texCoord);
}";

    private const string GRADIENT_VERTEX_SHADER = @"#version 330 core
uniform mat4 projection_matrix;
layout(location = 0) in vec2 in_position;
layout(location = 1) in vec2 in_texCoord;
layout(location = 2) in vec4 in_color;
out vec4 color;
out vec2 texCoord;
out vec2 screenPos;
void main()
{
    gl_Position = projection_matrix * vec4(in_position, 0, 1);
    color = in_color;
    texCoord = in_texCoord;
    screenPos = in_position;
}";

    private const string GRADIENT_FRAGMENT_SHADER = @"#version 330 core
uniform sampler2D in_fontTexture;
uniform float time;
uniform int gradientMode;
in vec4 color;
in vec2 texCoord;
in vec2 screenPos;
out vec4 outputColor;

vec3 hsv2rgb(vec3 c) {
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

void main()
{
    vec4 texColor = texture(in_fontTexture, texCoord);
    
    if (texColor.a > 0.01 && texColor.a < 0.99) {
        vec3 gradientColor;
        
        if (gradientMode == 0) {
            float t = fract(screenPos.x * 0.002 + time * 0.2);
            gradientColor = hsv2rgb(vec3(t, 0.8, 1.0));
        }
        else if (gradientMode == 1) {
            float t = fract(screenPos.y * 0.002 + time * 0.2);
            gradientColor = hsv2rgb(vec3(t, 0.8, 1.0));
        }
        else if (gradientMode == 2) {
            float t = fract((screenPos.x + screenPos.y) * 0.001 + time * 0.2);
            gradientColor = hsv2rgb(vec3(t, 0.8, 1.0));
        }
        else {
            float t = fract(screenPos.x * 0.003 + sin(screenPos.y * 0.01 + time * 2.0) * 0.1 + time * 0.3);
            gradientColor = hsv2rgb(vec3(t, 0.9, 1.0));
        }
        
        outputColor = vec4(gradientColor * color.rgb, color.a * texColor.a);
    }
    else {
        outputColor = color * texColor;
    }
}";

    public ImGuiGradientTextRenderer(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
        CreateDeviceResources();
    }

    public void WindowResized(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
    }

    private void CreateDeviceResources()
    {
        _vertexArray = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArray);

        _vertexBufferSize = 10000;
        _indexBufferSize = 2000;

        _vertexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        _indexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        RecreateFontDeviceTexture();

        // Create standard shader
        _standardShader = CreateProgram("Standard", STANDARD_VERTEX_SHADER, STANDARD_FRAGMENT_SHADER);
        _standardShaderProjectionMatrixLocation = GL.GetUniformLocation(_standardShader, "projection_matrix");
        _standardShaderFontTextureLocation = GL.GetUniformLocation(_standardShader, "in_fontTexture");

        // Create gradient shader
        _gradientShader = CreateProgram("Gradient", GRADIENT_VERTEX_SHADER, GRADIENT_FRAGMENT_SHADER);
        _gradientShaderProjectionMatrixLocation = GL.GetUniformLocation(_gradientShader, "projection_matrix");
        _gradientShaderFontTextureLocation = GL.GetUniformLocation(_gradientShader, "in_fontTexture");
        _gradientShaderTimeLocation = GL.GetUniformLocation(_gradientShader, "time");
        _gradientShaderModeLocation = GL.GetUniformLocation(_gradientShader, "gradientMode");

        // Setup vertex attributes
        GL.BindVertexArray(_vertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);

        int stride = Unsafe.SizeOf<ImDrawVert>();
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 8);
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, stride, 16);

        GL.BindVertexArray(0);
    }

    private void RecreateFontDeviceTexture()
    {
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out _);

        _fontTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _fontTexture);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

        io.Fonts.SetTexID((IntPtr)_fontTexture);
        io.Fonts.ClearTexData();
    }

    private int CreateProgram(string name, string vertexSource, string fragmentSource)
    {
        int program = GL.CreateProgram();
        int vertex = CompileShader(name, ShaderType.VertexShader, vertexSource);
        int fragment = CompileShader(name, ShaderType.FragmentShader, fragmentSource);

        GL.AttachShader(program, vertex);
        GL.AttachShader(program, fragment);
        GL.LinkProgram(program);

        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
        if (success == 0)
        {
            Console.WriteLine($"GL.LinkProgram had errors for {name}:\n{GL.GetProgramInfoLog(program)}");
        }

        GL.DetachShader(program, vertex);
        GL.DetachShader(program, fragment);
        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);

        return program;
    }

    private int CompileShader(string name, ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
        if (success == 0)
        {
            Console.WriteLine($"GL.CompileShader for {name} [{type}] had errors:\n{GL.GetShaderInfoLog(shader)}");
        }

        return shader;
    }

    public void Update(float deltaSeconds)
    {
        _time += deltaSeconds;
    }

    public void SetGradientMode(int mode) => _gradientMode = mode;
    public void ToggleGradientShader() => _useGradientShader = !_useGradientShader;
    public bool IsGradientShaderEnabled() => _useGradientShader;
    public int GetGradientMode() => _gradientMode;

    public void Render(ImDrawDataPtr drawData)
    {
        if (drawData.CmdListsCount == 0) return;

        // Save state
        int prevVAO = GL.GetInteger(GetPName.VertexArrayBinding);
        int prevArrayBuffer = GL.GetInteger(GetPName.ArrayBufferBinding);
        int prevProgram = GL.GetInteger(GetPName.CurrentProgram);
        bool prevBlendEnabled = GL.GetBoolean(GetPName.Blend);
        bool prevScissorTestEnabled = GL.GetBoolean(GetPName.ScissorTest);

        GL.BindVertexArray(_vertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);

        for (int i = 0; i < drawData.CmdListsCount; i++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[i];

            int vertexSize = cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
            if (vertexSize > _vertexBufferSize)
            {
                int newSize = (int)Math.Max(_vertexBufferSize * 1.5f, vertexSize);
                GL.BufferData(BufferTarget.ArrayBuffer, newSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                _vertexBufferSize = newSize;
            }

            int indexSize = cmdList.IdxBuffer.Size * sizeof(ushort);
            if (indexSize > _indexBufferSize)
            {
                int newSize = (int)Math.Max(_indexBufferSize * 1.5f, indexSize);
                GL.BufferData(BufferTarget.ElementArrayBuffer, newSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                _indexBufferSize = newSize;
            }
        }

        var io = ImGui.GetIO();
        Matrix4 mvp = Matrix4.CreateOrthographicOffCenter(0.0f, io.DisplaySize.X, io.DisplaySize.Y, 0.0f, -1.0f, 1.0f);

        int currentShader = _useGradientShader ? _gradientShader : _standardShader;
        int currentProjectionLocation = _useGradientShader ? _gradientShaderProjectionMatrixLocation : _standardShaderProjectionMatrixLocation;
        int currentFontTextureLocation = _useGradientShader ? _gradientShaderFontTextureLocation : _standardShaderFontTextureLocation;

        GL.UseProgram(currentShader);
        GL.UniformMatrix4(currentProjectionLocation, false, ref mvp);
        GL.Uniform1(currentFontTextureLocation, 0);

        if (_useGradientShader)
        {
            GL.Uniform1(_gradientShaderTimeLocation, _time);
            GL.Uniform1(_gradientShaderModeLocation, _gradientMode);
        }

        GL.BindVertexArray(_vertexArray);
        drawData.ScaleClipRects(io.DisplayFramebufferScale);

        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.ScissorTest);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[n];

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(), cmdList.VtxBuffer.Data);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
            GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, cmdList.IdxBuffer.Size * sizeof(ushort), cmdList.IdxBuffer.Data);

            for (int cmdI = 0; cmdI < cmdList.CmdBuffer.Size; cmdI++)
            {
                ImDrawCmdPtr pcmd = cmdList.CmdBuffer[cmdI];
                
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, (int)pcmd.TextureId);

                var clip = pcmd.ClipRect;
                GL.Scissor((int)clip.X, _windowHeight - (int)clip.W, (int)(clip.Z - clip.X), (int)(clip.W - clip.Y));

                GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, 
                    (IntPtr)(pcmd.IdxOffset * sizeof(ushort)), unchecked((int)pcmd.VtxOffset));
            }
        }

        // Restore state
        GL.BindVertexArray(prevVAO);
        GL.BindBuffer(BufferTarget.ArrayBuffer, prevArrayBuffer);
        GL.UseProgram(prevProgram);
        if (prevBlendEnabled) GL.Enable(EnableCap.Blend); else GL.Disable(EnableCap.Blend);
        if (prevScissorTestEnabled) GL.Enable(EnableCap.ScissorTest); else GL.Disable(EnableCap.ScissorTest);
    }

    public void Dispose()
    {
        if (_disposed) return;

        GL.DeleteVertexArray(_vertexArray);
        GL.DeleteBuffer(_vertexBuffer);
        GL.DeleteBuffer(_indexBuffer);
        GL.DeleteTexture(_fontTexture);
        GL.DeleteProgram(_standardShader);
        GL.DeleteProgram(_gradientShader);

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

