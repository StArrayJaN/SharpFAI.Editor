using System;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SharpFAI.Editor.Platform.Android
{
    public class AndroidGraphicsContext : SharpFAI.Editor.Platform.Graphics.IGraphicsContext
    {
        public bool IsClosing { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public string Title { get; set; } = "SharpFAI";
        public OpenTK.Windowing.Common.VSyncMode VSync { get; set; } = OpenTK.Windowing.Common.VSyncMode.On;
        public bool IsVisible { get; set; } = true;
        public bool IsFullscreen { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public void MakeCurrent() { }
        public void SwapBuffers() { }
        public void ProcessEvents() { }
        public void Dispose() { IsClosing = true; }

        public void SetSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public void SetPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public void Show() => IsVisible = true;
        public void Hide() => IsVisible = false;
        public void Close() => IsClosing = true;

        public void RenderFrame(double time)
        {
            // 由子类实现
        }

        public void Load()
        {
            // 由子类实现
        }

        public void Resize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public void KeyInput(Keys key, int scanCode, InputAction action, KeyModifiers modifiers)
        {
            // 由子类实现
        }

        public void MouseMove(double x, double y)
        {
            // 由子类实现
        }

        public void MouseButton(MouseButton button, InputAction action, KeyModifiers modifiers)
        {
            // 由子类实现
        }

        public void MouseScroll(double offsetX, double offsetY)
        {
            // 由子类实现
        }
    }
}
