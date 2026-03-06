using System;

namespace SharpFAI.Editor.Platform
{
    /// <summary>
    /// 平台工厂类（已禁用）
    /// 各平台应自行实现平台特定的功能
    /// </summary>
    [Obsolete("PlatformFactory 已禁用，各平台应自行实现平台特定的功能")]
    public static class PlatformFactory
    {
        /// <summary>
        /// 创建图形上下文（已禁用）
        /// </summary>
        [Obsolete("PlatformFactory 已禁用，请在各平台自行创建图形上下文")]
        public static Graphics.IGraphicsContext CreateGraphicsContext(string title = "SharpFAI", int width = 1280, int height = 720)
        {
            throw new NotSupportedException("PlatformFactory 已禁用，请在各平台自行创建图形上下文。例如：new Desktop.DesktopGraphicsContext(title, width, height)");
        }

        /// <summary>
        /// 创建音频提供者（已禁用）
        /// </summary>
        [Obsolete("PlatformFactory 已禁用，请在各平台自行创建音频提供者")]
        public static Audio.IAudioProvider CreateAudioProvider()
        {
            throw new NotSupportedException("PlatformFactory 已禁用，请在各平台自行创建音频提供者。例如：new Desktop.DesktopAudioProvider()");
        }
    }
}
