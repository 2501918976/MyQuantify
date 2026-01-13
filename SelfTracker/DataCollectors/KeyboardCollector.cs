using SelfTracker.Entity.Base;
using SelfTracker.Repository.Base;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SelfTracker.Controllers
{
    /// <summary>
    /// 键盘监听控制器：采用内存缓冲模式，定时由 DataCollector 触发入库
    /// </summary>
    public class KeyboardController : IDisposable
    {
        private readonly TypingLogRepository _typingRepo;
        private SystemStateLog _currentSession;
        private int? _currentProcessId;

        // --- 内存缓冲区 ---
        private int _tempKeyCount = 0;
        private DateTime _periodStartTime = DateTime.Now;

        private IntPtr _hookId = IntPtr.Zero;
        private LowLevelKeyboardProc _proc;

        public KeyboardController(TypingLogRepository typingRepo, SystemStateLog currentSession, int? currentProcessId = null)
        {
            _typingRepo = typingRepo;
            _currentSession = currentSession;
            _currentProcessId = currentProcessId;
        }

        public void Start()
        {
            if (_hookId != IntPtr.Zero) return;
            _proc = HookCallback;
            _hookId = SetHook(_proc);
            _periodStartTime = DateTime.Now;
        }

        public void Stop()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
                _proc = null;
            }
        }

        public void SetCurrentSession(SystemStateLog session) => _currentSession = session;

        public void SetCurrentProcess(int processId) => _currentProcessId = processId;

        /// <summary>
        /// 执行数据冲刷：由 DataCollector 定时调用
        /// 将内存中累加的击键次数生成一条 TypingLog
        /// </summary>
        public void Flush()
        {
            if (_tempKeyCount <= 0)
            {
                _periodStartTime = DateTime.Now; // 重置统计起点
                return;
            }

            int countToSave = _tempKeyCount;
            _tempKeyCount = 0; // 立即清空计数

            var now = DateTime.Now;
            var log = new TypingLog
            {
                StartTime = _periodStartTime,
                EndTime = now,
                Duration = (int)(now - _periodStartTime).TotalSeconds,
                KeyCount = countToSave,
                SystemStateLogId = _currentSession.Id,
                // 如果没有进程 ID，则尝试使用 0 或指向一个“未知进程”的记录
                ProcessInfoId = _currentProcessId ?? 0
            };

            _typingRepo.Add(log);
            _periodStartTime = now; // 更新下一阶段的起点
        }

        #region 键盘钩子逻辑

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule!;
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            const int WM_KEYDOWN = 0x0100;
            const int WM_SYSKEYDOWN = 0x0104;

            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                // 仅在内存中自增，不直接操作数据库，保证了 Hook 的极致响应速度
                _tempKeyCount++;
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void Dispose() => Stop();

        private const int WH_KEYBOARD_LL = 13;
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion
    }
}