using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace SelfTracker.DataCollectors
{
    /// <summary>
    /// 剪贴板监听器：通过监听 Windows 消息来探测用户是否执行了复制/剪切操作
    /// </summary>
    public class ClipboardCollector : IDisposable
    {
        // 当剪贴板内容发生变化时触发的事件（仅作为信号，不传递具体内容以保护隐私/提高性能）
        public event Action OnClipboardChanged;

        // HwndSource 是 WPF 中用于接收窗口消息的关键类（它可以创建一个不可见的窗口来挂载钩子）
        private HwndSource _hwndSource;

        /// <summary>
        /// 启动剪贴板监听
        /// </summary>
        public void Start()
        {
            if (_hwndSource != null) return;

            // 配置一个不可见的辅助窗口参数
            var parameters = new HwndSourceParameters("ClipboardListener")
            {
                Width = 0,
                Height = 0,
                PositionX = 0,
                PositionY = 0,
                WindowStyle = 0x800000 // WS_OVERLAPPED: 标准窗口样式
            };

            // 创建窗口资源
            _hwndSource = new HwndSource(parameters);

            // 向窗口添加“钩子”函数（WndProc），用于拦截并处理系统发来的消息
            _hwndSource.AddHook(WndProc);

            // 【关键 API】调用 Windows 底层函数，将该窗口句柄添加到剪贴板格式监听器列表中
            // 之后系统只要有复制动作，就会发消息给这个 Handle
            AddClipboardFormatListener(_hwndSource.Handle);
        }

        /// <summary>
        /// 窗口消息处理函数（WndProc）
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Windows 定义的“剪贴板更新”消息 ID
            const int WM_CLIPBOARDUPDATE = 0x031D;

            if (msg == WM_CLIPBOARDUPDATE)
            {
                // 核心逻辑：这里仅捕捉“更新”这一行为。
                // 相比于直接读取剪贴板文字，这种方式可以避免跨进程读取可能导致的 UI 卡顿或隐私合规问题。
                OnClipboardChanged?.Invoke();
            }

            // 返回 Zero 表示按照标准方式继续处理其他可能的窗口消息
            return IntPtr.Zero;
        }

        /// <summary>
        /// 停止监听并释放资源
        /// </summary>
        public void Stop()
        {
            if (_hwndSource != null)
            {
                // 从系统监听列表中移除该窗口句柄
                RemoveClipboardFormatListener(_hwndSource.Handle);

                // 移除消息钩子
                _hwndSource.RemoveHook(WndProc);

                // 销毁窗口资源
                _hwndSource.Dispose();
                _hwndSource = null;
            }
        }

        /// <summary>
        /// 实现 IDisposable 接口，确保在对象销毁时正确关闭监听
        /// </summary>
        public void Dispose() => Stop();

        #region Win32 API 导入

        // AddClipboardFormatListener: 将窗口放置在剪贴板格式监听链中
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        // RemoveClipboardFormatListener: 从监听链中移除窗口
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        #endregion
    }
}