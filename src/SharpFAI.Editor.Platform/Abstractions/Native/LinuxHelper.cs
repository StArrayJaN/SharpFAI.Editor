using System.Diagnostics;
using System.IO;

namespace SharpFAI.Editor.Platform.Native;

public class LinuxHelper
{
    public static bool InstallPackage(string package)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = "sudo";
        startInfo.Arguments = $"apt install {package}";
        startInfo.UseShellExecute = true;
        return false;
    }
    private enum PackageManager
    {
        Apt,
        Pacman,
        None
    }

    private static PackageManager GetPackageManager()
    {
        if (Directory.Exists("/etc/apt"))
        {
            return PackageManager.Apt;
        }

        if (Directory.Exists("/etc/pacman"))
        {
            return PackageManager.Pacman;
        }
        return PackageManager.None;
    }

    private static string GetInstallCommand()
    {
        PackageManager manager = GetPackageManager();
        switch (manager)
        {
            case PackageManager.Apt:
                return "apt install";
            case PackageManager.Pacman:
                return "pacman -S";
            default:
                return "";
        }
    }
}

