using SelfTracker.Entity.Base;
using SelfTracker.Repository.Base;
using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace SelfTracker.Controllers
{
    /// <summary>
    /// 剪贴板监听控制器：监听复制操作并在内存累计，定时 Flush 到数据库
    /// </summary>
    public class CopyController : IDisposable
    {
        private readonly CopyLogRepository _copyRepo;
        private SystemStateLog _currentSession;
        private HwndSource? _hwndSource;

        // --- 内存缓冲区 ---
        private int _tempCopyCount = 0;

        public CopyController(CopyLogRepository copyRepo, SystemStateLog currentSession)
        {
            _copyRepo = copyRepo;
            _currentSession = currentSession;
        }

        public void Start()
        {
            if (_hwndSource != null) return;

            var parameters = new HwndSourceParameters("CopyControllerListener")
            {
                Width = 0,
                Height = 0,
                PositionX = 0,
                PositionY = 0,
                WindowStyle = 0x800000 // WS_OVERLAPPED
            };

            _hwndSource = new HwndSource(parameters);
            _hwndSource.AddHook(WndProc);
            AddClipboardFormatListener(_hwndSource.Handle);
        }

        public void Stop()
        {
            if (_hwndSource == null) return;

            RemoveClipboardFormatListener(_hwndSource.Handle);
            _hwndSource.RemoveHook(WndProc);
            _hwndSource.Dispose();
            _hwndSource = null;
        }

        public void SetCurrentSession(SystemStateLog session) => _currentSession = session;

        /// <summary>
        /// 执行数据冲刷：将这段时间内的复制次数写入数据库
        /// </summary>
        public void Flush()
        {
            if (_tempCopyCount <= 0) return;

            int countToSave = _tempCopyCount;
            _tempCopyCount = 0;

            var now = DateTime.Now;
            var log = new CopyLog
            {
                StartTime = now,
                EndTime = now,
                Duration = 0,
                CopyCount = countToSave,
                SystemStateLogId = _currentSession.Id,
                ProcessInfoId = null // 如有需要可关联当前进程
            };

            _copyRepo.Add(log);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_CLIPBOARDUPDATE = 0x031D;
            if (msg == WM_CLIPBOARDUPDATE)
            {
                _tempCopyCount++; // 收到系统通知，仅累加计数
            }
            return IntPtr.Zero;
        }

        public void Dispose() => Stop();

        #region Win32 API
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
        #endregion
    }
}