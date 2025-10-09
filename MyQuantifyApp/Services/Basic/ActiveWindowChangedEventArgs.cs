using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyQuantifyApp.Services.Basic
{
    public class ActiveWindowChangedEventArgs : EventArgs
    {
        public ActiveWindowChangedEventArgs(string title, string processName, string filePath)
        {
            Title = title;
            ProcessName = processName;
            FilePath = filePath;
        }

        /// <summary>
        /// 窗口标题
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// 进程名称（例如：chrome.exe, explorer.exe）
        /// </summary>
        public string ProcessName { get; }

        /// <summary>
        /// 进程可执行文件的完整路径
        /// </summary>
        public string FilePath { get; }
    }
}
