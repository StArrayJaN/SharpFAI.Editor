using System;
using SharpFAI.Editor.Core;
using SharpFAI.Editor.Platform.Desktop;

namespace SharpFAI
{
    class Program
    {
        static void Main(string[] args)
        {
            var mode = ParseMode(args);
            Console.WriteLine($"SharpFAI - macOS");
            Console.WriteLine($"Mode: {mode}\n");

            // macOS 平台特定的实现
            var graphicsContext = new DesktopGraphicsContext("SharpFAI - macOS", 1280, 720);
            var audioProvider = new DesktopAudioProvider();

            using (var app = new SharpFAIApplication(mode))
            {
                // 使用平台特定的实现初始化应用程序
                app.Initialize(graphicsContext, audioProvider);
                app.Run();
            }
        }

        static SharpFAIApplication.ApplicationMode ParseMode(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg.StartsWith("--mode="))
                {
                    return arg.Substring("--mode=".Length).ToLowerInvariant() switch
                    {
                        "player" => SharpFAIApplication.ApplicationMode.PlayerOnly,
                        "editor" => SharpFAIApplication.ApplicationMode.EditorOnly,
                        _ => SharpFAIApplication.ApplicationMode.EditorOnly
                    };
                }
            }
            return SharpFAIApplication.ApplicationMode.EditorOnly;
        }
    }
}
