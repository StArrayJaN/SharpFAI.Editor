using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;

namespace SharpFAI.Editor.Platform.Native
{
    public static class GTKFileDialog
    {
        private static bool _isInitialized = false;
        private static object _lockObj = new object();

        // GTK 和 GLib 导入
        [DllImport("libgtk-3.so.0", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool gtk_init_check(IntPtr argc, IntPtr argv);

        [DllImport("libgdk-3.so.0", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gdk_display_get_default();

        [DllImport("libgdk-3.so.0", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gdk_display_open([MarshalAs(UnmanagedType.LPUTF8Str)] string display_name);

        [DllImport("libgtk-3.so.0", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gtk_file_chooser_dialog_new(
            IntPtr title,
            IntPtr parent,
            GtkFileChooserAction action,
            IntPtr first_button_text,
            GtkResponseType first_button_response,
            IntPtr second_button_text,
            GtkResponseType second_button_response,
            IntPtr terminator);

        [DllImport("libgtk-3.so.0", CallingConvention = CallingConvention.Cdecl)]
        private static extern void gtk_file_chooser_set_select_multiple(IntPtr chooser, bool select_multiple);

        [DllImport("libgtk-3.so.0", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool gtk_file_chooser_set_current_folder(IntPtr chooser, IntPtr filename);

        [DllImport("libgtk-3.so.0", CallingConvention = CallingConvention.Cdecl)]
        private static extern void gtk_file_chooser_set_current_name(IntPtr chooser, IntPtr name);

        [DllImport("libgtk-3.so.0", CallingConvention = CallingConvention.Cdecl)]
        private static extern void gtk_file_chooser_set_do_overwrite_confirmation(IntPtr chooser, bool do_overwrite_confirmation);

        [DllImport("libgtk-3.so.0", CallingConvention = CallingConvention.Cdecl)]
        private static extern int gtk_dialog_run(IntPtr dialog);

        [DllImport("libgtk-3.so.0", CallingConvention = CallingConvention.Cdecl)]
        private static extern void gtk_widget_destroy(IntPtr widget);

        [DllImport("libgtk-3.so.0", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool gtk_events_pending();

        [DllImport("libgtk-3.so.0", CallingConvention = CallingConvention.Cdecl)]
        private static extern void gtk_main_iteration();

        [DllImport("libgtk-3.so.0", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gtk_file_chooser_get_filename(IntPtr chooser);

        [DllImport("libgtk-3.so.0", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gtk_file_chooser_get_filenames(IntPtr chooser);

        [DllImport("libglib-2.0.so.0", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr g_slist_nth_data(IntPtr list, uint n);

        [DllImport("libglib-2.0.so.0", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint g_slist_length(IntPtr list);

        [DllImport("libglib-2.0.so.0", CallingConvention = CallingConvention.Cdecl)]
        private static extern void g_slist_free(IntPtr list);

        [DllImport("libglib-2.0.so.0", CallingConvention = CallingConvention.Cdecl)]
        private static extern void g_free(IntPtr mem);

        [DllImport("libgtk-3.so.0", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gtk_file_filter_new();

        [DllImport("libgtk-3.so.0", CallingConvention = CallingConvention.Cdecl)]
        private static extern void gtk_file_filter_set_name(IntPtr filter, IntPtr name);

        [DllImport("libgtk-3.so.0", CallingConvention = CallingConvention.Cdecl)]
        private static extern void gtk_file_filter_add_pattern(IntPtr filter, IntPtr pattern);

        [DllImport("libgtk-3.so.0", CallingConvention = CallingConvention.Cdecl)]
        private static extern void gtk_file_chooser_add_filter(IntPtr chooser, IntPtr filter);

        // 枚举定义
        private enum GtkFileChooserAction
        {
            GTK_FILE_CHOOSER_ACTION_OPEN = 0,
            GTK_FILE_CHOOSER_ACTION_SAVE = 1,
            GTK_FILE_CHOOSER_ACTION_SELECT_FOLDER = 2,
            GTK_FILE_CHOOSER_ACTION_CREATE_FOLDER = 3
        }

        private enum GtkResponseType
        {
            GTK_RESPONSE_NONE = -1,
            GTK_RESPONSE_REJECT = -2,
            GTK_RESPONSE_ACCEPT = -3,
            GTK_RESPONSE_DELETE_EVENT = -4,
            GTK_RESPONSE_OK = -5,
            GTK_RESPONSE_CANCEL = -6,
            GTK_RESPONSE_CLOSE = -7,
            GTK_RESPONSE_YES = -8,
            GTK_RESPONSE_NO = -9,
            GTK_RESPONSE_APPLY = -10,
            GTK_RESPONSE_HELP = -11
        }

        /// <summary>
        /// 辅助方法：将C#字符串转换为UTF-8 IntPtr
        /// </summary>
        private static IntPtr StringToUtf8(string str)
        {
            if (string.IsNullOrEmpty(str))
                return IntPtr.Zero;

            byte[] bytes = Encoding.UTF8.GetBytes(str);
            IntPtr ptr = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            Marshal.WriteByte(ptr, bytes.Length, 0); // null terminator
            return ptr;
        }

        /// <summary>
        /// 辅助方法：从UTF-8 IntPtr读取字符串
        /// </summary>
        private static string Utf8ToString(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return string.Empty;

            int len = 0;
            while (Marshal.ReadByte(ptr, len) != 0)
                len++;

            if (len == 0)
                return string.Empty;

            byte[] bytes = new byte[len];
            Marshal.Copy(ptr, bytes, 0, len);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// 初始化GTK，必须在使用对话框前调用
        /// </summary>
        public static bool DialogInit()
        {
            lock (_lockObj)
            {
                if (_isInitialized)
                    return true;

                try
                {
                    // 尝试初始化GTK
                    if (!gtk_init_check(IntPtr.Zero, IntPtr.Zero))
                    {
                        Console.Error.WriteLine("GTK初始化失败");
                        string displayVariable = Environment.GetEnvironmentVariable("DISPLAY");
                        if (string.IsNullOrEmpty(displayVariable))
                        {
                            Console.Error.WriteLine("请检查DISPLAY环境变量");
                        }
                        IntPtr display = gdk_display_open(displayVariable);
                        
                        if (display == IntPtr.Zero)
                        {
                            Console.Error.WriteLine($"无法打开显示{displayVariable}");
                            return false;
                        }

                        // 再次尝试初始化
                        if (!gtk_init_check(IntPtr.Zero, IntPtr.Zero))
                        {
                            Console.Error.WriteLine("GTK二次初始化失败");
                            return false;
                        }
                    }

                    // 验证显示连接
                    IntPtr defaultDisplay = gdk_display_get_default();
                    if (defaultDisplay == IntPtr.Zero)
                    {
                        Console.Error.WriteLine("警告: 无法获取默认显示");
                    }

                    _isInitialized = true;
                    Console.WriteLine("GTK初始化成功");
                    return true;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"GTK初始化异常: {ex.Message}");
                    Console.Error.WriteLine($"堆栈: {ex.StackTrace}");
                    return false;
                }
            }
        }

        /// <summary>
        /// 打开文件选择对话框
        /// </summary>
        public static string DialogOpenFilePanel(string title, string directory, string extension, bool multiselect)
        {
            if (!_isInitialized && !DialogInit())
            {
                Console.Error.WriteLine("GTK未初始化");
                return string.Empty;
            }

            return GTKOpenPanel(title, directory, extension, multiselect, 
                GtkFileChooserAction.GTK_FILE_CHOOSER_ACTION_OPEN);
        }

        /// <summary>
        /// 打开文件夹选择对话框
        /// </summary>
        public static string DialogOpenFolderPanel(string title, string directory, bool multiselect)
        {
            if (!_isInitialized && !DialogInit())
            {
                Console.Error.WriteLine("GTK未初始化");
                return string.Empty;
            }

            return GTKOpenPanel(title, directory, "", multiselect, 
                GtkFileChooserAction.GTK_FILE_CHOOSER_ACTION_SELECT_FOLDER);
        }

        /// <summary>
        /// 保存文件对话框
        /// </summary>
        public static string DialogSaveFilePanel(string title, string directory, string defaultName, string filters)
        {
            if (!_isInitialized && !DialogInit())
            {
                Console.Error.WriteLine("GTK未初始化");
                return string.Empty;
            }

            return GTKSavePanel(title, directory, defaultName, filters);
        }

        private static string GTKOpenPanel(string title, string directory, string extensions, 
            bool multiselect, GtkFileChooserAction action)
        {
            IntPtr titlePtr = IntPtr.Zero;
            IntPtr cancelPtr = IntPtr.Zero;
            IntPtr openPtr = IntPtr.Zero;
            IntPtr directoryPtr = IntPtr.Zero;
            IntPtr dialog = IntPtr.Zero;

            try
            {
                titlePtr = StringToUtf8(title);
                cancelPtr = StringToUtf8("_取消");
                openPtr = StringToUtf8("_打开");

                dialog = gtk_file_chooser_dialog_new(
                    titlePtr,
                    IntPtr.Zero,
                    action,
                    cancelPtr,
                    GtkResponseType.GTK_RESPONSE_CANCEL,
                    openPtr,
                    GtkResponseType.GTK_RESPONSE_ACCEPT,
                    IntPtr.Zero);

                if (dialog == IntPtr.Zero)
                {
                    Console.Error.WriteLine("无法创建对话框");
                    return string.Empty;
                }

                gtk_file_chooser_set_select_multiple(dialog, multiselect);

                if (!string.IsNullOrEmpty(directory))
                {
                    directoryPtr = StringToUtf8(directory);
                    gtk_file_chooser_set_current_folder(dialog, directoryPtr);
                }

                GTKSetFilters(extensions, dialog);

                int res = gtk_dialog_run(dialog);

                if (res == (int)GtkResponseType.GTK_RESPONSE_ACCEPT)
                {
                    if (multiselect)
                    {
                        IntPtr filenames = gtk_file_chooser_get_filenames(dialog);
                        if (filenames != IntPtr.Zero)
                        {
                            uint length = g_slist_length(filenames);
                            List<string> fileList = new List<string>();

                            for (uint i = 0; i < length; i++)
                            {
                                IntPtr data = g_slist_nth_data(filenames, i);
                                if (data != IntPtr.Zero)
                                {
                                    string filename = Utf8ToString(data);
                                    if (!string.IsNullOrEmpty(filename))
                                    {
                                        fileList.Add(filename);
                                    }
                                    g_free(data);
                                }
                            }

                            g_slist_free(filenames);

                            return string.Join("\u001C", fileList); // ASCII 28分隔符
                        }
                    }
                    else
                    {
                        IntPtr filenamePtr = gtk_file_chooser_get_filename(dialog);
                        if (filenamePtr != IntPtr.Zero)
                        {
                            string result = Utf8ToString(filenamePtr);
                            g_free(filenamePtr);
                            return result;
                        }
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"对话框错误: {ex.Message}");
                return string.Empty;
            }
            finally
            {
                if (dialog != IntPtr.Zero)
                {
                    gtk_widget_destroy(dialog);
                    
                    while (gtk_events_pending())
                        gtk_main_iteration();
                }

                if (titlePtr != IntPtr.Zero) Marshal.FreeHGlobal(titlePtr);
                if (cancelPtr != IntPtr.Zero) Marshal.FreeHGlobal(cancelPtr);
                if (openPtr != IntPtr.Zero) Marshal.FreeHGlobal(openPtr);
                if (directoryPtr != IntPtr.Zero) Marshal.FreeHGlobal(directoryPtr);
            }
        }

        private static string GTKSavePanel(string title, string directory, string defaultName, string filters)
        {
            IntPtr titlePtr = IntPtr.Zero;
            IntPtr cancelPtr = IntPtr.Zero;
            IntPtr savePtr = IntPtr.Zero;
            IntPtr directoryPtr = IntPtr.Zero;
            IntPtr defaultNamePtr = IntPtr.Zero;
            IntPtr dialog = IntPtr.Zero;

            try
            {
                titlePtr = StringToUtf8(title ?? "保存文件");
                cancelPtr = StringToUtf8("_取消");
                savePtr = StringToUtf8("_保存");

                dialog = gtk_file_chooser_dialog_new(
                    titlePtr,
                    IntPtr.Zero,
                    GtkFileChooserAction.GTK_FILE_CHOOSER_ACTION_SAVE,
                    cancelPtr,
                    GtkResponseType.GTK_RESPONSE_CANCEL,
                    savePtr,
                    GtkResponseType.GTK_RESPONSE_ACCEPT,
                    IntPtr.Zero);

                if (dialog == IntPtr.Zero)
                {
                    Console.Error.WriteLine("无法创建保存对话框");
                    return string.Empty;
                }

                gtk_file_chooser_set_do_overwrite_confirmation(dialog, true);

                if (!string.IsNullOrEmpty(defaultName))
                {
                    defaultNamePtr = StringToUtf8(defaultName);
                    gtk_file_chooser_set_current_name(dialog, defaultNamePtr);
                }

                if (!string.IsNullOrEmpty(directory))
                {
                    directoryPtr = StringToUtf8(directory);
                    gtk_file_chooser_set_current_folder(dialog, directoryPtr);
                }

                GTKSetFilters(filters, dialog);

                int res = gtk_dialog_run(dialog);

                if (res == (int)GtkResponseType.GTK_RESPONSE_ACCEPT)
                {
                    IntPtr filenamePtr = gtk_file_chooser_get_filename(dialog);
                    if (filenamePtr != IntPtr.Zero)
                    {
                        string result = Utf8ToString(filenamePtr);
                        g_free(filenamePtr);
                        return result;
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"保存对话框错误: {ex.Message}");
                return string.Empty;
            }
            finally
            {
                if (dialog != IntPtr.Zero)
                {
                    gtk_widget_destroy(dialog);
                    
                    while (gtk_events_pending())
                        gtk_main_iteration();
                }

                if (titlePtr != IntPtr.Zero) Marshal.FreeHGlobal(titlePtr);
                if (cancelPtr != IntPtr.Zero) Marshal.FreeHGlobal(cancelPtr);
                if (savePtr != IntPtr.Zero) Marshal.FreeHGlobal(savePtr);
                if (directoryPtr != IntPtr.Zero) Marshal.FreeHGlobal(directoryPtr);
                if (defaultNamePtr != IntPtr.Zero) Marshal.FreeHGlobal(defaultNamePtr);
            }
        }

        private static void GTKSetFilters(string extensions, IntPtr dialog)
        {
            if (string.IsNullOrEmpty(extensions))
                return;

            try
            {
                // 格式: "关卡文件;adofai|All Files;*"
                string[] filterGroups = extensions.Split('|');

                foreach (string filterGroup in filterGroups)
                {
                    if (string.IsNullOrWhiteSpace(filterGroup))
                        continue;

                    IntPtr filter = gtk_file_filter_new();
                    if (filter == IntPtr.Zero)
                        continue;

                    string trimmedGroup = filterGroup.Trim();
                    bool hasName = !trimmedGroup.StartsWith(";");
                    
                    string[] parts = trimmedGroup.Split(new[] { ';' }, 2);

                    if (hasName && parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                    {
                        IntPtr namePtr = StringToUtf8(parts[0]);
                        gtk_file_filter_set_name(filter, namePtr);
                        Marshal.FreeHGlobal(namePtr);
                    }

                    string extensionsPart = hasName && parts.Length > 1 ? parts[1] : 
                                           (!hasName && parts.Length > 0 ? parts[0] : "");

                    if (!string.IsNullOrWhiteSpace(extensionsPart))
                    {
                        string[] exts = extensionsPart.Split(',');
                        foreach (string ext in exts)
                        {
                            string trimmedExt = ext.Trim();
                            if (string.IsNullOrEmpty(trimmedExt))
                                continue;

                            string pattern = trimmedExt.StartsWith("*") ? trimmedExt : "*." + trimmedExt;
                            IntPtr patternPtr = StringToUtf8(pattern);
                            gtk_file_filter_add_pattern(filter, patternPtr);
                            Marshal.FreeHGlobal(patternPtr);
                        }
                    }

                    gtk_file_chooser_add_filter(dialog, filter);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"设置过滤器错误: {ex.Message}");
            }
        }
    }
}