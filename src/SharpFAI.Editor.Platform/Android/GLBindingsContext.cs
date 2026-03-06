using System.Runtime.InteropServices;
using OpenTK;

namespace SharpFAI.Editor.Platform.Android;

public class GLBindingsContext : IBindingsContext
{
    [DllImport("dl")]
    static extern IntPtr dlsym(IntPtr handle, string name);
    [DllImport("dl")]
    static extern IntPtr dlopen(string fileName, int flags);
    private static IntPtr handle;
    private const int RTLD_LAZY = 1;
    private const int RTLD_NOW = 2;
    static GLBindingsContext()
    {
        handle = dlopen("libGLESv3.so", RTLD_NOW);
    }
    public IntPtr GetProcAddress(string procName)
    {
        return dlsym(handle, procName);
    }
}