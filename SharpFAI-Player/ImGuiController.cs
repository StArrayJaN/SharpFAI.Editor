using System.Runtime.CompilerServices;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SharpFAI_Player;

/// <summary>
/// ImGui controller for OpenTK integration
/// ImGui 的 OpenTK 集成控制器
/// </summary>
public class ImGuiController : IDisposable
{
    private bool _disposed;
    private int _vertexArray;
    private int _vertexBuffer;
    private int _vertexBufferSize;
    private int _indexBuffer;
    private int _indexBufferSize;
    private int _fontTexture;
    private int _shader;
    private int _shaderFontTextureLocation;
    private int _shaderProjectionMatrixLocation;
    
    private int _gradientShader;
    private int _gradientShaderFontTextureLocation;
    private int _gradientShaderProjectionMatrixLocation;
    private int _gradientShaderTimeLocation;
    private int _gradientShaderModeLocation;

    private int _windowWidth;
    private int _windowHeight;

    private System.Numerics.Vector2 _scaleFactor = System.Numerics.Vector2.One;
    
    private float _time = 0f;
    private bool _useGradientShader = true; // Default to false for safety
    private int _gradientMode = 3; // 0=horizontal, 1=vertical, 2=diagonal, 3=rainbow wave

    public ImGuiController(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;

        IntPtr context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);

        var io = ImGui.GetIO();
        
        // Load Source Han Sans font for Chinese support
        // 加载思源黑体以支持中文显示
        string fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "SourceHanSansSC-Regular-2.otf");
        if (File.Exists(fontPath))
        {
            io.Fonts.AddFontFromFileTTF(fontPath, 18.0f, null, io.Fonts.GetGlyphRangesChineseFull());
        }
        else
        {
            // Fallback to default font if Source Han Sans is not found
            io.Fonts.AddFontDefault();
            Console.WriteLine($"Warning: Font file not found at {fontPath}, using default font");
        }
        
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        CreateDeviceResources();
        SetKeyMappings();

        SetPerFrameImGuiData(1f / 60f);

        // Don't call ImGui.NewFrame() here - it will be called in Update()
    }

    public void WindowResized(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
    }

    public void CreateDeviceResources()
    {
        // Create vertex array
        _vertexArray = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArray);

        // Create buffers
        _vertexBufferSize = 10000;
        _indexBufferSize = 2000;

        _vertexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        _indexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        RecreateFontDeviceTexture();

        // Create shader
        string vertexSource = @"#version 330 core
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

        string fragmentSource = @"#version 330 core
uniform sampler2D in_fontTexture;
in vec4 color;
in vec2 texCoord;
out vec4 outputColor;
void main()
{
    outputColor = color * texture(in_fontTexture, texCoord);
}";

        _shader = CreateProgram("ImGui", vertexSource, fragmentSource);
        _shaderProjectionMatrixLocation = GL.GetUniformLocation(_shader, "projection_matrix");
        _shaderFontTextureLocation = GL.GetUniformLocation(_shader, "in_fontTexture");

        // Create gradient shader from Resources folder
        string gradientVertPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "text_gradient.vert");
        string gradientFragPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "text_gradient.frag");
        
        if (File.Exists(gradientVertPath) && File.Exists(gradientFragPath))
        {
            string gradientVertSource = File.ReadAllText(gradientVertPath);
            string gradientFragSource = File.ReadAllText(gradientFragPath);
            
            _gradientShader = CreateProgram("ImGuiGradient", gradientVertSource, gradientFragSource);
            _gradientShaderProjectionMatrixLocation = GL.GetUniformLocation(_gradientShader, "projection_matrix");
            _gradientShaderFontTextureLocation = GL.GetUniformLocation(_gradientShader, "in_fontTexture");
            _gradientShaderTimeLocation = GL.GetUniformLocation(_gradientShader, "time");
            _gradientShaderModeLocation = GL.GetUniformLocation(_gradientShader, "gradientMode");
        }
        else
        {
            Console.WriteLine("Warning: Gradient shader files not found, using default shader");
            _useGradientShader = false;
        }

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
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);

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
            string info = GL.GetProgramInfoLog(program);
            Console.WriteLine($"GL.LinkProgram had errors for {name}:\n{info}");
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
            string info = GL.GetShaderInfoLog(shader);
            Console.WriteLine($"GL.CompileShader for {name} [{type}] had errors:\n{info}");
        }

        return shader;
    }

    private void SetKeyMappings()
    {
        // KeyMap is deprecated in newer ImGui versions
        // Input handling is now done through AddKeyEvent
        // This method is kept for compatibility but does nothing
    }

    public void Update(GameWindow wnd, float deltaSeconds)
    {
        _time += deltaSeconds;
        SetPerFrameImGuiData(deltaSeconds);
        UpdateImGuiInput(wnd);

        // Start a new ImGui frame (must be called before any ImGui commands)
        try
        {
            ImGui.NewFrame();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ImGui.NewFrame exception: {ex.Message}");
        }
    }
    
    public void SetGradientMode(int mode)
    {
        _gradientMode = mode;
    }
    
    public void ToggleGradientShader()
    {
        _useGradientShader = !_useGradientShader;
    }
    
    public bool IsGradientShaderEnabled()
    {
        return _useGradientShader;
    }
    
    public int GetGradientMode()
    {
        return _gradientMode;
    }

    private void SetPerFrameImGuiData(float deltaSeconds)
    {
        var io = ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2(
            _windowWidth / _scaleFactor.X,
            _windowHeight / _scaleFactor.Y);
        io.DisplayFramebufferScale = _scaleFactor;
        io.DeltaTime = deltaSeconds;
    }

    private void UpdateImGuiInput(GameWindow wnd)
    {
        var io = ImGui.GetIO();

        var mouseState = wnd.MouseState;
        var keyboardState = wnd.KeyboardState;

        io.MouseDown[0] = mouseState[MouseButton.Left];
        io.MouseDown[1] = mouseState[MouseButton.Right];
        io.MouseDown[2] = mouseState[MouseButton.Middle];

        var screenPoint = new Vector2i((int)mouseState.X, (int)mouseState.Y);
        io.MousePos = new System.Numerics.Vector2(screenPoint.X, screenPoint.Y);

        // 正确处理按键事件：发送按下和释放事件
        // Properly handle key events: send both press and release events
        foreach (Keys key in Enum.GetValues(typeof(Keys)))
        {
            if (key == Keys.Unknown)
            {
                continue;
            }
            
            ImGuiKey imguiKey = ConvertKey(key);
            if (imguiKey == ImGuiKey.None)
            {
                continue;
            }
            
            bool isDown = keyboardState.IsKeyDown(key);
            io.AddKeyEvent(imguiKey, isDown);
        }

        io.KeyCtrl = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
        io.KeyAlt = keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt);
        io.KeyShift = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
        io.KeySuper = keyboardState.IsKeyDown(Keys.LeftSuper) || keyboardState.IsKeyDown(Keys.RightSuper);
    }

    private ImGuiKey ConvertKey(Keys key)
    {
        return key switch
        {
            Keys.Tab => ImGuiKey.Tab,
            Keys.Left => ImGuiKey.LeftArrow,
            Keys.Right => ImGuiKey.RightArrow,
            Keys.Up => ImGuiKey.UpArrow,
            Keys.Down => ImGuiKey.DownArrow,
            Keys.PageUp => ImGuiKey.PageUp,
            Keys.PageDown => ImGuiKey.PageDown,
            Keys.Home => ImGuiKey.Home,
            Keys.End => ImGuiKey.End,
            Keys.Insert => ImGuiKey.Insert,
            Keys.Delete => ImGuiKey.Delete,
            Keys.Backspace => ImGuiKey.Backspace,
            Keys.Space => ImGuiKey.Space,
            Keys.Enter => ImGuiKey.Enter,
            Keys.Escape => ImGuiKey.Escape,
            Keys.A => ImGuiKey.A,
            Keys.C => ImGuiKey.C,
            Keys.V => ImGuiKey.V,
            Keys.X => ImGuiKey.X,
            Keys.Y => ImGuiKey.Y,
            Keys.Z => ImGuiKey.Z,
            _ => ImGuiKey.None
        };
    }

    public void PressChar(char keyChar)
    {
        ImGui.GetIO().AddInputCharacter(keyChar);
    }

    public void MouseScroll(Vector2 offset)
    {
        var io = ImGui.GetIO();
        io.MouseWheel = offset.Y;
        io.MouseWheelH = offset.X;
    }

    public void Render()
    {
        try
        {
            ImGui.Render();
            RenderImDrawData(ImGui.GetDrawData());
        }
        catch (Exception ex)
        {
            // Handle ImGui rendering exceptions gracefully
            Console.WriteLine($"ImGui.Render exception: {ex.Message}");
        }
    }

    private void RenderImDrawData(ImDrawDataPtr drawData)
    {
        if (drawData.CmdListsCount == 0)
        {
            return;
        }

        // Get intial state
        int prevVAO = GL.GetInteger(GetPName.VertexArrayBinding);
        int prevArrayBuffer = GL.GetInteger(GetPName.ArrayBufferBinding);
        int prevProgram = GL.GetInteger(GetPName.CurrentProgram);
        bool prevBlendEnabled = GL.GetBoolean(GetPName.Blend);
        bool prevScissorTestEnabled = GL.GetBoolean(GetPName.ScissorTest);
        int prevBlendEquationRgb = GL.GetInteger(GetPName.BlendEquationRgb);
        int prevBlendEquationAlpha = GL.GetInteger(GetPName.BlendEquationAlpha);
        int prevBlendFuncSrcRgb = GL.GetInteger(GetPName.BlendSrcRgb);
        int prevBlendFuncSrcAlpha = GL.GetInteger(GetPName.BlendSrcAlpha);
        int prevBlendFuncDstRgb = GL.GetInteger(GetPName.BlendDstRgb);
        int prevBlendFuncDstAlpha = GL.GetInteger(GetPName.BlendDstAlpha);
        bool prevCullFaceEnabled = GL.GetBoolean(GetPName.CullFace);
        bool prevDepthTestEnabled = GL.GetBoolean(GetPName.DepthTest);
        int prevActiveTexture = GL.GetInteger(GetPName.ActiveTexture);
        GL.ActiveTexture(TextureUnit.Texture0);
        int prevTexture2D = GL.GetInteger(GetPName.TextureBinding2D);
        Span<int> prevScissorBox = stackalloc int[4];
        unsafe
        {
            fixed (int* iptr = &prevScissorBox[0])
            {
                GL.GetInteger(GetPName.ScissorBox, iptr);
            }
        }

        // Bind the element buffer (thru the VAO) so that we can resize it.
        GL.BindVertexArray(_vertexArray);
        // Bind the vertex buffer so that we can resize it.
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

        // Setup orthographic projection matrix into our constant buffer
        var io = ImGui.GetIO();
        Matrix4 mvp = Matrix4.CreateOrthographicOffCenter(
            0.0f,
            io.DisplaySize.X,
            io.DisplaySize.Y,
            0.0f,
            -1.0f,
            1.0f);

        // Choose shader based on settings
        int currentShader = (_useGradientShader && _gradientShader != 0) ? _gradientShader : _shader;
        int currentProjectionLocation = (_useGradientShader && _gradientShader != 0) ? _gradientShaderProjectionMatrixLocation : _shaderProjectionMatrixLocation;
        int currentFontTextureLocation = (_useGradientShader && _gradientShader != 0) ? _gradientShaderFontTextureLocation : _shaderFontTextureLocation;

        GL.UseProgram(currentShader);
        GL.UniformMatrix4(currentProjectionLocation, false, ref mvp);
        GL.Uniform1(currentFontTextureLocation, 0);
        
        // Set gradient shader uniforms if using gradient shader
        if (_useGradientShader && _gradientShader != 0)
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

        // Render command lists
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
                if (pcmd.UserCallback != IntPtr.Zero)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, (int)pcmd.TextureId);

                    // We do _windowHeight - (int)clip.W instead of (int)clip.Y because gl has flipped Y when it comes to these coordinates
                    var clip = pcmd.ClipRect;
                    GL.Scissor((int)clip.X, _windowHeight - (int)clip.W, (int)(clip.Z - clip.X), (int)(clip.W - clip.Y));

                    if ((io.BackendFlags & ImGuiBackendFlags.RendererHasVtxOffset) != 0)
                    {
                        GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (IntPtr)(pcmd.IdxOffset * sizeof(ushort)), unchecked((int)pcmd.VtxOffset));
                    }
                    else
                    {
                        GL.DrawElements(BeginMode.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (int)pcmd.IdxOffset * sizeof(ushort));
                    }
                }
            }
        }

        // Reset state
        GL.BindVertexArray(prevVAO);
        GL.BindBuffer(BufferTarget.ArrayBuffer, prevArrayBuffer);
        GL.UseProgram(prevProgram);
        GL.BlendEquationSeparate((BlendEquationMode)prevBlendEquationRgb, (BlendEquationMode)prevBlendEquationAlpha);
        GL.BlendFuncSeparate(
            (BlendingFactorSrc)prevBlendFuncSrcRgb,
            (BlendingFactorDest)prevBlendFuncDstRgb,
            (BlendingFactorSrc)prevBlendFuncSrcAlpha,
            (BlendingFactorDest)prevBlendFuncDstAlpha);
        if (prevBlendEnabled) GL.Enable(EnableCap.Blend); else GL.Disable(EnableCap.Blend);
        if (prevDepthTestEnabled) GL.Enable(EnableCap.DepthTest); else GL.Disable(EnableCap.DepthTest);
        if (prevCullFaceEnabled) GL.Enable(EnableCap.CullFace); else GL.Disable(EnableCap.CullFace);
        if (prevScissorTestEnabled) GL.Enable(EnableCap.ScissorTest); else GL.Disable(EnableCap.ScissorTest);
        GL.ActiveTexture((TextureUnit)prevActiveTexture);
        GL.BindTexture(TextureTarget.Texture2D, prevTexture2D);
        GL.Scissor(prevScissorBox[0], prevScissorBox[1], prevScissorBox[2], prevScissorBox[3]);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        GL.DeleteVertexArray(_vertexArray);
        GL.DeleteBuffer(_vertexBuffer);
        GL.DeleteBuffer(_indexBuffer);
        GL.DeleteTexture(_fontTexture);
        GL.DeleteProgram(_shader);
        
        if (_gradientShader != 0)
        {
            GL.DeleteProgram(_gradientShader);
        }

        _disposed = true;
    }
}
