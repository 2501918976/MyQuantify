using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using MyQuantifyApp.DataCollector.Models;
using MyQuantifyApp.DataCollector.Storage;
using MyQuantifyApp.DataCollector.Utilities;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MyQuantifyApp.DataCollector.Services
{
    /// <summary>
    /// 使用键盘钩子，每次按键直接保存到数据库，不做聚合。
    /// </summary>
    public class TypingCountService : BackgroundService
    {
        private readonly IDbContextFactory<ActivityDbContext> _dbContextFactory;

        private IntPtr _hookHandle;
        private PInvokeHelper.HookProc _hookDelegate;
        private Thread _messageLoopThread;

        public TypingCountService(IDbContextFactory<ActivityDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
            _hookDelegate = HookCallback; // 避免GC
        }

        // =======================================================
        // IHostedService 生命周期
        // =======================================================
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("[TypingCountService] 初始化...");

            // 预热数据库
            using var db = _dbContextFactory.CreateDbContext();
            db.Database.CanConnect();

            InstallKeyboardHook();
            await base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("[TypingCountService] 停止服务...");

            if (_hookHandle != IntPtr.Zero)
            {
                PInvokeHelper.UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
                Console.WriteLine("[TypingCountService] 键盘钩子已卸载。");
            }

            if (_messageLoopThread != null && _messageLoopThread.IsAlive)
            {
                PInvokeHelper.PostThreadMessage(
                    (uint)_messageLoopThread.ManagedThreadId,
                    PInvokeHelper.WM_QUIT,
                    IntPtr.Zero,
                    IntPtr.Zero
                );
            }

            return base.StopAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 这里不再有定时器，只是挂钩子
            return Task.CompletedTask;
        }

        // =======================================================
        // 钩子相关
        // =======================================================
        private void InstallKeyboardHook()
        {
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                _hookHandle = PInvokeHelper.SetWindowsHookEx(
                    PInvokeHelper.WH_KEYBOARD_LL,
                    _hookDelegate,
                    PInvokeHelper.GetModuleHandle(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName),
                    0
                );
            }

            if (_hookHandle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                Console.WriteLine($"[TypingCountService] ❌ 键盘钩子安装失败! Win32错误码: {errorCode}");
                // 建议添加常见错误码解析
                if (errorCode == 5) Console.WriteLine("错误原因: 需要管理员权限");
            }
            else
            {
                Console.WriteLine("[TypingCountService] ✅ 键盘钩子安装成功。");

                // 修改消息循环线程启动方式
                _messageLoopThread = new Thread(MessageLoop);
                _messageLoopThread.SetApartmentState(ApartmentState.STA); // 增加 STA 设置
                _messageLoopThread.IsBackground = true;
                _messageLoopThread.Start();

            }
        }

        private void MessageLoop()
        {
            Console.WriteLine("[消息循环] 线程启动"); // 增加启动日志
            try
            {
                while (true)
                {
                    PInvokeHelper.MSG msg;
                    int result = PInvokeHelper.GetMessage(out msg, IntPtr.Zero, 0, 0);
                    Console.WriteLine($"[消息循环] 收到消息: {msg.message}"); // 消息监控
                    if (result == 0) break;
                    if (result == -1)
                    {
                        Console.WriteLine($"[消息循环] 错误代码: {Marshal.GetLastWin32Error()}");
                        continue;
                    }
                    PInvokeHelper.TranslateMessage(ref msg);
                    PInvokeHelper.DispatchMessage(ref msg);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[消息循环] 异常: {ex}");
            }
        }


        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                Console.WriteLine($"[HookCallback] 收到事件: nCode={nCode}, wParam={(uint)wParam}");
                if ((int)wParam == PInvokeHelper.WM_KEYDOWN)
                {
                    var hookStruct = (PInvokeHelper.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(
                        lParam, typeof(PInvokeHelper.KBDLLHOOKSTRUCT));

                    Console.WriteLine($"键盘按下: vkCode={hookStruct.vkCode}");

                    if (IsTypingKey((int)hookStruct.vkCode))
                    {
                        Console.WriteLine($"✅ 有效按键: vkCode={hookStruct.vkCode}");
                        Task.Run(() => SaveKeyPressAsync());
                    }
                }
            }
            return PInvokeHelper.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }


        private bool IsTypingKey(int vkCode)
        {
            //// 排除功能键
            //if (vkCode >= 0x10 && vkCode <= 0x12 || // Shift/Ctrl/Alt
            //    vkCode >= 0x70 && vkCode <= 0x7B)   // F1-F12
            //    return false;

            return true;
        }

        // 修改保存方法增加异步等待
        private async Task SaveKeyPressAsync()
        {
            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync();
                var activeSession = await db.ActivitySessions
                    .AsNoTracking()  // 提升查询性能
                    .OrderByDescending(s => s.StartTime)
                    .FirstOrDefaultAsync();

                if (activeSession == null)
                {
                    Console.WriteLine("[TypingCountService] ⚠ 没有活动会话，按键未保存。");
                    return;
                }

                db.TypingCounts.Add(new TypingCount
                {
                    Timestamp = DateTime.UtcNow,
                    KeyPressCount = 1,
                    ActivitySessionId = activeSession.Id
                });

                await db.SaveChangesAsync();
                Console.WriteLine("[TypingCountService] 已保存一次按键。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TypingCountService] 保存异常: {ex}");
            }
        }

    }
}
