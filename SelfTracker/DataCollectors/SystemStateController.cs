using Microsoft.Win32;
using SelfTracker.Entity.Base;
using SelfTracker.Repository;
using SelfTracker.Repository.Base;
using System;
using System.Runtime.InteropServices;

namespace SelfTracker.Controllers
{
    /// <summary>
    /// 系统状态控制器，统一管理用户行为状态（AFK/ActiveUsing）和设备状态（PowerSession/Sleep/Hibernate）
    /// </summary>
    public class SystemStateController : IDisposable
    {
        private readonly QuantifyDbContext _db;

        private SystemStateLog? _currentUserStateLog;
        private SystemStateLog? _currentDeviceStateLog;
        private readonly TimeSpan _afkThreshold;

        public SystemStateController(QuantifyDbContext db, TimeSpan? afkThreshold = null)
        {
            _db = db;
            _afkThreshold = afkThreshold ?? TimeSpan.FromMinutes(5);

            // 订阅系统电源事件
            SystemEvents.PowerModeChanged += OnPowerModeChanged;

            // 初始化设备状态为 PowerSession
            StartDeviceState(SystemStateType.PowerSession);
        }

        /// <summary>
        /// 定时检查用户状态，建议每 10 秒或 30 秒调用一次
        /// </summary>
        public void CheckUserState()
        {
            DateTime now = DateTime.Now;

            // 如果设备处于 Sleep/Hibernate，不更新用户状态
            if (_currentDeviceStateLog == null)
                return;

            if (_currentDeviceStateLog.Type == SystemStateType.Sleep ||
                _currentDeviceStateLog.Type == SystemStateType.Hibernate)
            {
                EndUserState(now);
                return;
            }

            bool isAfk = IsUserAFK();
            SystemStateType userState = isAfk ? SystemStateType.AFK : SystemStateType.ActiveUsing;

            if (_currentUserStateLog == null || _currentUserStateLog.Type != userState)
            {
                EndUserState(now);

                // 开始新用户状态
                _currentUserStateLog = new SystemStateLog
                {
                    Type = userState,
                    StartTime = now,
                    DeviceName = Environment.MachineName,
                    SessionKey = Guid.NewGuid().ToString()
                };
                _db.SystemStateLogs.Add(_currentUserStateLog);
                _db.SaveChanges();
            }
            else
            {
                // 持续更新持续时间
                _currentUserStateLog.EndTime = now;
                _currentUserStateLog.Duration = (int)(now - _currentUserStateLog.StartTime).TotalSeconds;
                _db.SystemStateLogs.Update(_currentUserStateLog);
                _db.SaveChanges();
            }
        }

        #region 用户空闲检测（原 AFKCollector 功能）

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        /// <summary>
        /// 判断当前用户是否处于 AFK
        /// </summary>
        private bool IsUserAFK()
        {
            LASTINPUTINFO info = new LASTINPUTINFO();
            info.cbSize = (uint)Marshal.SizeOf(info);

            if (!GetLastInputInfo(ref info))
                return false;

            uint idleTicks = unchecked((uint)Environment.TickCount - info.dwTime);
            TimeSpan idleTime = TimeSpan.FromMilliseconds(idleTicks);

            return idleTime >= _afkThreshold;
        }

        #endregion

        #region 设备状态处理

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            DateTime now = DateTime.Now;

            switch (e.Mode)
            {
                case PowerModes.Suspend:
                    StartDeviceState(SystemStateType.Sleep, now);
                    EndUserState(now);
                    break;
                case PowerModes.Resume:
                    StartDeviceState(SystemStateType.PowerSession, now);
                    break;
            }
        }

        private void StartDeviceState(SystemStateType type, DateTime? startTime = null)
        {
            DateTime now = startTime ?? DateTime.Now;

            // 结束旧设备状态
            if (_currentDeviceStateLog != null)
            {
                _currentDeviceStateLog.EndTime = now;
                _currentDeviceStateLog.Duration = (int)(now - _currentDeviceStateLog.StartTime).TotalSeconds;
                _db.SystemStateLogs.Update(_currentDeviceStateLog);
                _db.SaveChanges();
            }

            // 开始新设备状态
            _currentDeviceStateLog = new SystemStateLog
            {
                Type = type,
                StartTime = now,
                DeviceName = Environment.MachineName,
                SessionKey = Guid.NewGuid().ToString()
            };
            _db.SystemStateLogs.Add(_currentDeviceStateLog);
            _db.SaveChanges();
        }

        #endregion

        #region 用户状态处理

        private void EndUserState(DateTime now)
        {
            if (_currentUserStateLog != null)
            {
                _currentUserStateLog.EndTime = now;
                _currentUserStateLog.Duration = (int)(now - _currentUserStateLog.StartTime).TotalSeconds;
                _db.SystemStateLogs.Update(_currentUserStateLog);
                _db.SaveChanges();
                _currentUserStateLog = null;
            }
        }

        #endregion

        public void Dispose()
        {
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        }

        /// <summary>
        /// 获取当前的系统状态记录（优先返回用户状态，如 ActiveUsing/AFK；若无，则返回设备状态）
        /// </summary>
        public SystemStateLog GetCurrentSession()
        {
            // 优先返回用户行为状态（因为打字、窗口记录更应该关联到“使用中”或“挂机”状态）
            if (_currentUserStateLog != null)
            {
                return _currentUserStateLog;
            }

            // 如果没有用户状态（比如刚启动），返回设备级状态
            if (_currentDeviceStateLog != null)
            {
                return _currentDeviceStateLog;
            }

            // 理论上不应该运行到这里，但为了健壮性，返回一个临时的
            return new SystemStateLog
            {
                Type = SystemStateType.Unknown,
                StartTime = DateTime.Now,
                SessionKey = "temp_key"
            };
        }
    }
}
