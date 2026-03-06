using System;
using SharpFAI.Editor.Platform;
using SharpFAI.Editor.Platform.Graphics;
using SharpFAI.Editor.Platform.Audio;

namespace SharpFAI.Editor.Core
{
    /// <summary>
    /// SharpFAI 应用程序基类
    /// 管理图形上下文、音频和主循环
    /// </summary>
    public class SharpFAIApplication : IDisposable
    {
        protected readonly string Version = "0.1.0";
        protected IGraphicsContext GraphicsContext;
        protected IAudioProvider AudioProvider;
        public ApplicationMode CurrentMode { get; private set; }
        protected bool IsRunning = true;

        public enum ApplicationMode
        {
            /// <summary>仅播放器模式</summary>
            PlayerOnly,
            /// <summary>仅编辑器模式</summary>
            EditorOnly,
            /// <summary>编辑器+播放器模式</summary>
            Combined
        }

        public SharpFAIApplication(ApplicationMode mode = ApplicationMode.EditorOnly)
        {
            CurrentMode = mode;
        }

        /// <summary>
        /// 初始化应用程序（使用外部提供的平台实现）
        /// </summary>
        /// <param name="graphicsContext">图形上下文</param>
        /// <param name="audioProvider">音频提供者</param>
        public virtual void Initialize(IGraphicsContext graphicsContext, IAudioProvider audioProvider)
        {
            GraphicsContext = graphicsContext ?? throw new ArgumentNullException(nameof(graphicsContext));
            AudioProvider = audioProvider ?? throw new ArgumentNullException(nameof(audioProvider));

            // 根据模式初始化 UI
            InitializeUI();
        }

        /// <summary>
        /// 初始化应用程序（向后兼容，使用默认实现）
        /// </summary>
        [Obsolete("使用 Initialize(IGraphicsContext, IAudioProvider) 替代，让各平台自行提供实现")]
        public virtual void Initialize()
        {
            throw new NotSupportedException("请使用 Initialize(IGraphicsContext, IAudioProvider) 方法，让各平台自行提供实现");
        }

        /// <summary>
        /// 根据应用模式初始化 UI
        /// </summary>
        protected virtual void InitializeUI()
        {
            switch (CurrentMode)
            {
                case ApplicationMode.PlayerOnly:
                    Console.WriteLine("[UI] Initializing Player mode...");
                    break;
                case ApplicationMode.EditorOnly:
                    Console.WriteLine("[UI] Initializing Editor mode...");
                    break;
                case ApplicationMode.Combined:
                    Console.WriteLine("[UI] Initializing Combined mode (Editor + Player)...");
                    break;
            }
        }

        /// <summary>
        /// 运行应用程序（需要先调用 Initialize 方法）
        /// </summary>
        public virtual void Run()
        {
            if (GraphicsContext == null || AudioProvider == null)
            {
                throw new InvalidOperationException("必须先调用 Initialize(IGraphicsContext, IAudioProvider) 方法初始化应用程序");
            }

            // Desktop 特定的运行方式：使用 GameWindow.Run()
            #if WINDOWS || LINUX || MACOS
            if (GraphicsContext is dynamic gameWindow)
            {
                gameWindow.Run();
            }
            #endif

            // Android 会有不同的事件循环方式
        }

        /// <summary>
        /// 运行应用程序（向后兼容，已过时）
        /// </summary>
        [Obsolete("使用 Run() 替代，但需要先调用 Initialize(IGraphicsContext, IAudioProvider)")]
        public virtual void RunWithDefaultPlatform()
        {
            throw new NotSupportedException("请使用 Initialize(IGraphicsContext, IAudioProvider) 和 Run() 方法，让各平台自行提供实现");
        }

        protected virtual void GraphicsLoad()
        {
            Console.WriteLine($"[SharpFAI] Application loaded. Mode: {CurrentMode}");
        }

        protected virtual void GraphicsRender(double time)
        {
            // 子类或 UI 系统实现渲染逻辑
            // TODO: ImGUI 渲染
        }

        protected virtual void GraphicsResize(int width, int height)
        {
            // 处理窗口大小改变
        }

        protected virtual void KeyInput(OpenTK.Windowing.GraphicsLibraryFramework.Keys key, int scanCode, OpenTK.Windowing.GraphicsLibraryFramework.InputAction action, OpenTK.Windowing.GraphicsLibraryFramework.KeyModifiers modifiers)
        {
            // 处理键盘输入
        }

        protected virtual void MouseMove(double x, double y)
        {
            // 处理鼠标移动
        }

        protected virtual void MouseButton(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton button, OpenTK.Windowing.GraphicsLibraryFramework.InputAction action, OpenTK.Windowing.GraphicsLibraryFramework.KeyModifiers modifiers)
        {
            // 处理鼠标按钮
        }

        protected virtual void MouseScroll(double offsetX, double offsetY)
        {
            // 处理鼠标滚轮
        }

        public virtual void Dispose()
        {
            GraphicsContext?.Dispose();
            AudioProvider?.Dispose();
        }
    }
}
