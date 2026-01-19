using System.Runtime.InteropServices;

namespace SharpFAI_Player.Native;

public class NativeAPI
{
    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool GetOpenFileName(ref OpenFileName ofn);
    
    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool GetSaveFileName(ref OpenFileName ofn);
    
    //MessageBox
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct OpenFileName
    {
        public int lStructSize;
        public IntPtr hwndOwner;
        public IntPtr hInstance;
        public string lpstrFilter;
        public string lpstrCustomFilter;
        public int nMaxCustFilter;
        public int nFilterIndex;
        public string lpstrFile;
        public int nMaxFile;
        public string lpstrFileTitle;
        public int nMaxFileTitle;
        public string lpstrInitialDir;
        public string lpstrTitle;
        public int Flags;
        public short nFileOffset;
        public short nFileExtension;
        public string lpstrDefExt;
        public IntPtr lCustData;
        public IntPtr lpfnHook;
        public string lpTemplateName;
        public IntPtr pvReserved;
        public int dwReserved;
        public int FlagsEx;
    }
    
    /// name = "Advanced"
    /// Filter = new List<string>() { "*.txt", "*.exe" }
    /// 转为 (Advanced(*.txt,*.exe)\0)
    public struct FileFilter
    {
        public string Name;
        public List<string> Filter;
        public bool IncludeAllFiles;
    }
    
    public static string OpenFileDialog(FileFilter filter)
    {
        if (OperatingSystem.IsWindows())
        {
            OpenFileName ofn = new OpenFileName();
            ofn.lStructSize = Marshal.SizeOf(ofn);
            string extensions = string.Join(";", filter.Filter);
            string win32Result = $"{filter.Name}({extensions})\0{extensions}\0";
            if (filter.IncludeAllFiles)
            {
                win32Result += "All Files(*.*)\0*.*\0";
            }
            
            ofn.lpstrFilter = win32Result;
            ofn.lpstrFile = new string(new char[256]);
            ofn.nMaxFile = ofn.lpstrFile.Length;
            ofn.lpstrFileTitle = new string(new char[64]);
            ofn.nMaxFileTitle = ofn.lpstrFileTitle.Length;
            ofn.lpstrTitle = "选择一个文件";
        
            if (GetOpenFileName(ref ofn))
            {
                return ofn.lpstrFile;
            }
        }

        if (OperatingSystem.IsLinux())
        {
            // 初始化GTK（如果尚未初始化）
            if (!Native.GTKFileDialog.DialogInit())
            {
                Console.Error.WriteLine("无法初始化GTK文件对话框");
                return String.Empty;
            }
            
            List<string> extensionsWithoutWildcard = new List<string>();
            foreach (string ext in filter.Filter)
            {
                if (ext.StartsWith("*.") || ext.StartsWith("."))
                {
                    extensionsWithoutWildcard.Add(ext.Substring(ext.StartsWith("*.") ? 2 : 1));
                }
                else
                {
                    extensionsWithoutWildcard.Add(ext);
                }
            }
            
            string extensions = string.Join(",", extensionsWithoutWildcard);
            string linuxResult = $"{filter.Name};{extensions}";
            
            // 如果需要包含 All Files 过滤器，则添加它
            if (filter.IncludeAllFiles)
            {
                linuxResult += "|All Files;*";
            }

            Console.WriteLine($"Opening file dialog with filter: {linuxResult}");
            
            var paths = GTKFileDialog.DialogOpenFilePanel("选择一个文件", "", linuxResult, false);
            return paths;
        }
        
        return String.Empty;
    }
    
    public static string SaveFileDialog(FileFilter filter, string defaultFileName = "")
    {
        if (OperatingSystem.IsWindows())
        {
            OpenFileName ofn = new OpenFileName();
            ofn.lStructSize = Marshal.SizeOf(ofn);
            string extensions = string.Join(";", filter.Filter);
            string win32Result = $"{filter.Name}({extensions})\0{extensions}\0";
            if (filter.IncludeAllFiles)
            {
                win32Result += "All Files(*.*)\0*.*\0";
            }
            
            ofn.lpstrFilter = win32Result;
            
            // 设置默认文件名
            string fileBuffer = defaultFileName;
            if (fileBuffer.Length < 256)
            {
                fileBuffer = fileBuffer.PadRight(256, '\0');
            }
            ofn.lpstrFile = fileBuffer;
            ofn.nMaxFile = ofn.lpstrFile.Length;
            
            ofn.lpstrFileTitle = new string(new char[64]);
            ofn.nMaxFileTitle = ofn.lpstrFileTitle.Length;
            ofn.lpstrTitle = "保存文件";
            
            // 设置默认扩展名（取第一个过滤器的扩展名）
            if (filter.Filter.Count > 0)
            {
                string firstExt = filter.Filter[0].Replace("*.", "");
                ofn.lpstrDefExt = firstExt;
            }
            
            // OFN_OVERWRITEPROMPT = 0x00000002 (提示覆盖)
            // OFN_PATHMUSTEXIST = 0x00000800 (路径必须存在)
            ofn.Flags = 0x00000002 | 0x00000800;
        
            if (GetSaveFileName(ref ofn))
            {
                return ofn.lpstrFile.TrimEnd('\0');
            }
        }

        if (OperatingSystem.IsLinux())
        {
            // 初始化GTK（如果尚未初始化）
            if (!GTKFileDialog.DialogInit())
            {
                Console.Error.WriteLine("无法初始化GTK文件对话框");
                return String.Empty;
            }
            
            List<string> extensionsWithoutWildcard = new List<string>();
            foreach (string ext in filter.Filter)
            {
                if (ext.StartsWith("*.") || ext.StartsWith("."))
                {
                    extensionsWithoutWildcard.Add(ext.Substring(ext.StartsWith("*.") ? 2 : 1));
                }
                else
                {
                    extensionsWithoutWildcard.Add(ext);
                }
            }
            
            string extensions = string.Join(",", extensionsWithoutWildcard);
            string linuxResult = $"{filter.Name};{extensions}";
            
            // 如果需要包含 All Files 过滤器，则添加它
            if (filter.IncludeAllFiles)
            {
                linuxResult += "|All Files;*";
            }

            Console.WriteLine($"Opening save file dialog with filter: {linuxResult}");
            
            // 获取默认目录
            string directory = "";
            if (!string.IsNullOrEmpty(defaultFileName))
            {
                directory = Path.GetDirectoryName(defaultFileName) ?? "";
                defaultFileName = Path.GetFileName(defaultFileName);
            }
            
            var paths = GTKFileDialog.DialogSaveFilePanel("保存文件", directory, defaultFileName, linuxResult);
            return paths;
        }
        
        return String.Empty;
    }
}