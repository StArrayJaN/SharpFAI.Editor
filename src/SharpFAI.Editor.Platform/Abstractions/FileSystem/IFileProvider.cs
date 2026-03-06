using System;
using System.Collections.Generic;

namespace SharpFAI.Editor.Platform.FileSystem
{
    /// <summary>
    /// 文件系统提供者接口
    /// 处理各平台的路径差异和文件操作
    /// </summary>
    public interface IFileProvider : IDisposable
    {
        /// <summary>
        /// 获取应用程序目录
        /// </summary>
        string GetApplicationDirectory();

        /// <summary>
        /// 获取用户数据目录（可写）
        /// </summary>
        string GetUserDataDirectory();

        /// <summary>
        /// 规范化路径（处理路径分隔符等）
        /// </summary>
        string NormalizePath(string path);

        /// <summary>
        /// 判断文件是否存在
        /// </summary>
        bool FileExists(string path);

        /// <summary>
        /// 判断目录是否存在
        /// </summary>
        bool DirectoryExists(string path);

        /// <summary>
        /// 读取文件
        /// </summary>
        byte[] ReadFile(string path);

        /// <summary>
        /// 写入文件
        /// </summary>
        void WriteFile(string path, byte[] data);

        /// <summary>
        /// 获取目录中的文件列表
        /// </summary>
        IEnumerable<string> GetFiles(string directory, string searchPattern = "*");

        /// <summary>
        /// 删除文件
        /// </summary>
        void DeleteFile(string path);

        /// <summary>
        /// 创建目录
        /// </summary>
        void CreateDirectory(string path);
    }
}
