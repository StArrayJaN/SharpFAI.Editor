using System;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SharpFAI.Editor.Platform.Desktop
{
    /// <summary>
    /// Desktop 平台的图形上下文实现 (Windows/Linux/macOS)
    /// 使用 OpenTK GameWindow 作为底层
    /// </summary>
    public class DesktopGraphicsContext : GameWindow, Graphics.IGraphicsContext
    {
        public DesktopGraphicsContext(string title = "SharpFAI", int width = 1280, int height = 720)
            : base(GameWindowSettings.Default, new NativeWindowSettings
            {
                Title = title,
                ClientSize = (width, height),
                WindowBorder = WindowBorder.Fixed,
                Vsync = VSyncMode.On,
            })
        {
        }

        bool Graphics.IGraphicsContext.IsClosing => base.IsExiting;

        int Graphics.IGraphicsContext.Width => Size.X;
        int Graphics.IGraphicsContext.Height => Size.Y;

        string Graphics.IGraphicsContext.Title
        {
            get => Title;
            set => Title = value;
        }

        VSyncMode Graphics.IGraphicsContext.VSync
        {
            get => VSync;
            set => VSync = value;
        }

        bool Graphics.IGraphicsContext.IsVisible
        {
            get => IsVisible;
            set => IsVisible = value;
        }

        bool Graphics.IGraphicsContext.IsFullscreen
        {
            get => WindowState == WindowState.Fullscreen;
            set => WindowState = value ? WindowState.Fullscreen : WindowState.Normal;
        }

        int Graphics.IGraphicsContext.X
        {
            get => Location.X;
            set => Location = (value, Location.Y);
        }

        int Graphics.IGraphicsContext.Y
        {
            get => Location.Y;
            set => Location = (Location.X, value);
        }

        void Graphics.IGraphicsContext.MakeCurrent() => base.MakeCurrent();
        void Graphics.IGraphicsContext.SwapBuffers() => base.SwapBuffers();
        void Graphics.IGraphicsContext.ProcessEvents() { }

        void Graphics.IGraphicsContext.SetSize(int width, int height) => Size = (width, height);
        void Graphics.IGraphicsContext.SetPosition(int x, int y) => Location = (x, y);
        void Graphics.IGraphicsContext.Show() => IsVisible = true;
        void Graphics.IGraphicsContext.Hide() => IsVisible = false;
        void Graphics.IGraphicsContext.Close() => Close();

        void Graphics.IGraphicsContext.RenderFrame(double time)
        {
            // 由子类实现
        }

        void Graphics.IGraphicsContext.Load()
        {
            // 由子类实现
        }

        void Graphics.IGraphicsContext.Resize(int width, int height)
        {
            // 由子类实现
        }

        void Graphics.IGraphicsContext.KeyInput(Keys key, int scanCode, InputAction action, KeyModifiers modifiers)
        {
            // 由子类实现
        }

        void Graphics.IGraphicsContext.MouseMove(double x, double y)
        {
            // 由子类实现
        }

        void Graphics.IGraphicsContext.MouseButton(MouseButton button, InputAction action, KeyModifiers modifiers)
        {
            // 由子类实现
        }

        void Graphics.IGraphicsContext.MouseScroll(double offsetX, double offsetY)
        {
            // 由子类实现
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            ((Graphics.IGraphicsContext)this).RenderFrame(args.Time);
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            ((Graphics.IGraphicsContext)this).Load();
        }

        protected override void OnResize(ResizeEventArgs args)
        {
            base.OnResize(args);
            ((Graphics.IGraphicsContext)this).Resize(args.Size.X, args.Size.Y);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);
            ((Graphics.IGraphicsContext)this).KeyInput(e.Key, e.ScanCode, InputAction.Press, e.Modifiers);
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            base.OnKeyUp(e);
            ((Graphics.IGraphicsContext)this).KeyInput(e.Key, e.ScanCode, InputAction.Release, e.Modifiers);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);
            ((Graphics.IGraphicsContext)this).MouseMove(e.X, e.Y);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            ((Graphics.IGraphicsContext)this).MouseButton(e.Button, InputAction.Press, e.Modifiers);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            ((Graphics.IGraphicsContext)this).MouseButton(e.Button, InputAction.Release, e.Modifiers);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            ((Graphics.IGraphicsContext)this).MouseScroll(e.OffsetX, e.OffsetY);
        }
    }
}
