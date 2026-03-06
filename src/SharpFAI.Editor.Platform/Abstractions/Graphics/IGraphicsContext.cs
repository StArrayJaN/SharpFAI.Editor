using System;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SharpFAI.Editor.Platform.Graphics
{
    /// <summary>
    /// 统一的图形上下文接口
    /// 负责处理平台特定的窗口和 OpenGL 上下文
    /// </summary>
    public interface IGraphicsContext : IDisposable
    {
        /// <summary>
        /// 使该上下文成为当前线程的上下文
        /// </summary>
        void MakeCurrent();

        /// <summary>
        /// 交换前后缓冲区
        /// </summary>
        void SwapBuffers();

        /// <summary>
        /// 处理待处理的事件
        /// </summary>
        void ProcessEvents();

        /// <summary>
        /// 获取窗口是否正在关闭
        /// </summary>
        bool IsClosing { get; }

        /// <summary>
        /// 获取窗口宽度
        /// </summary>
        int Width { get; }

        /// <summary>
        /// 获取窗口高度
        /// </summary>
        int Height { get; }

        /// <summary>
        /// 获取窗口标题
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// 获取或设置垂直同步模式
        /// </summary>
        VSyncMode VSync { get; set; }

        /// <summary>
        /// 获取或设置窗口是否可见
        /// </summary>
        bool IsVisible { get; set; }

        /// <summary>
        /// 获取或设置窗口是否全屏
        /// </summary>
        bool IsFullscreen { get; set; }

        /// <summary>
        /// 获取或设置窗口位置 (X坐标)
        /// </summary>
        int X { get; set; }

        /// <summary>
        /// 获取或设置窗口位置 (Y坐标)
        /// </summary>
        int Y { get; set; }

        /// <summary>
        /// 设置窗口大小
        /// </summary>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        void SetSize(int width, int height);

        /// <summary>
        /// 设置窗口位置
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        void SetPosition(int x, int y);

        /// <summary>
        /// 显示窗口
        /// </summary>
        void Show();

        /// <summary>
        /// 隐藏窗口
        /// </summary>
        void Hide();

        /// <summary>
        /// 关闭窗口
        /// </summary>
        void Close();

        /// <summary>
        /// 渲染帧回调方法 - 由平台实现调用
        /// </summary>
        /// <param name="time">帧时间（秒）</param>
        void RenderFrame(double time);

        /// <summary>
        /// 加载完成回调方法 - 由平台实现调用
        /// </summary>
        void Load();

        /// <summary>
        /// 窗口大小改变回调方法 - 由平台实现调用
        /// </summary>
        /// <param name="width">新的窗口宽度</param>
        /// <param name="height">新的窗口高度</param>
        void Resize(int width, int height);

        /// <summary>
        /// 键盘按键回调方法 - 由平台实现调用
        /// </summary>
        /// <param name="key">按键代码</param>
        /// <param name="scanCode">扫描码</param>
        /// <param name="action">按键动作</param>
        /// <param name="modifiers">修饰键</param>
        void KeyInput(Keys key, int scanCode, InputAction action, KeyModifiers modifiers);

        /// <summary>
        /// 鼠标移动回调方法 - 由平台实现调用
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        void MouseMove(double x, double y);

        /// <summary>
        /// 鼠标按钮回调方法 - 由平台实现调用
        /// </summary>
        /// <param name="button">按钮</param>
        /// <param name="action">按钮动作</param>
        /// <param name="modifiers">修饰键</param>
        void MouseButton(MouseButton button, InputAction action, KeyModifiers modifiers);

        /// <summary>
        /// 鼠标滚轮回调方法 - 由平台实现调用
        /// </summary>
        /// <param name="offsetX">水平滚动偏移</param>
        /// <param name="offsetY">垂直滚动偏移</param>
        void MouseScroll(double offsetX, double offsetY);
    }
}
