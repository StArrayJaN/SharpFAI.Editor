using System;

namespace SharpFAI.Editor.Platform.Input
{
    /// <summary>
    /// 输入管理接口
    /// 处理键盘、鼠标、触摸等输入
    /// </summary>
    public interface IInputManager : IDisposable
    {
        /// <summary>
        /// 检查键盘按键是否被按下
        /// </summary>
        bool IsKeyPressed(Keys key);

        /// <summary>
        /// 获取鼠标位置 (X, Y)
        /// </summary>
        (float X, float Y) GetMousePosition();

        /// <summary>
        /// 检查鼠标按键是否被按下
        /// </summary>
        bool IsMouseButtonPressed(MouseButton button);

        /// <summary>
        /// 获取鼠标滚轮值
        /// </summary>
        float GetMouseScroll();

        /// <summary>
        /// 更新输入状态（每帧调用）
        /// </summary>
        void Update();
    }

    /// <summary>
    /// 键盘按键枚举
    /// </summary>
    public enum Keys
    {
        A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,
        Escape, Enter, Tab, BackSpace, Space,
        Left, Right, Up, Down,
        LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt,
        Delete, Home, End, PageUp, PageDown,
        F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12
    }

    /// <summary>
    /// 鼠标按键枚举
    /// </summary>
    public enum MouseButton
    {
        Left,
        Right,
        Middle
    }
}
