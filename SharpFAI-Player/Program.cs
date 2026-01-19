using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using SharpFAI_Player.Native;

namespace SharpFAI_Player;

/// <summary>
/// 程序入口点 - 可以选择启动播放器或编辑器
/// Program entry point - Choose to launch player or editor
/// </summary>
public class Program
{
    public const bool launchEditor = true;
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (OperatingSystem.IsWindows())
            {
                NativeAPI.MessageBox(IntPtr.Zero, e.ExceptionObject?.ToString() ?? "Unknown Error","SharpFAI Player", 0);
            }
        };
        NativeWindowSettings gameWindowSettings = new NativeWindowSettings
        {
            Title = "SharpFAI Player",
            Flags = ContextFlags.Default,
            APIVersion = new Version(3, 3),
            Vsync = VSyncMode.Off,
            ClientSize = new OpenTK.Mathematics.Vector2i(1280, 720)
        };
        if (launchEditor)
        {
            // 启动编辑器
            // Launch editor
            Console.WriteLine("启动编辑器模式 Launching Editor Mode...");
            gameWindowSettings.Title = "SharpFAI Editor";
            EditorPlayer editor = new EditorPlayer(GameWindowSettings.Default, gameWindowSettings, null);
            editor.Run();
        }
        else
        {
            // 启动播放器
            // Launch player
            Console.WriteLine("启动播放器模式 Launching Player Mode...");
            gameWindowSettings.Title = "SharpFAI Player";
            MainPlayer player = new MainPlayer(GameWindowSettings.Default, gameWindowSettings, null);
            player.Run(); 
        }
    }
}
