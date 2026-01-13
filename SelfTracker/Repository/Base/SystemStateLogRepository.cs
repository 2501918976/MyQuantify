using Microsoft.EntityFrameworkCore;
using SelfTracker.Entity.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfTracker.Repository.Base
{
    public class SystemStateLogRepository
    {
        private readonly QuantifyDbContext _db;
        public SystemStateLogRepository(QuantifyDbContext db) => _db = db;

        public void Add(SystemStateLog log) { _db.SystemStateLogs.Add(log); _db.SaveChanges(); }
        public void Update(SystemStateLog log) { _db.SystemStateLogs.Update(log); _db.SaveChanges(); }
        public void Remove(SystemStateLog log) { _db.SystemStateLogs.Remove(log); _db.SaveChanges(); }

        public SystemStateLog? GetById(int id) =>
            _db.SystemStateLogs
                .Include(s => s.ActivityLogs)
                .Include(s => s.TypingLogs)
                .Include(s => s.CopyLogs)
                .FirstOrDefault(s => s.Id == id);

        public IEnumerable<SystemStateLog> GetAll() =>
            _db.SystemStateLogs
                .Include(s => s.ActivityLogs)
                .Include(s => s.TypingLogs)
                .Include(s => s.CopyLogs)
                .ToList();

        public IEnumerable<SystemStateLog> GetByDate(DateTime date) =>
            _db.SystemStateLogs
                .Include(s => s.ActivityLogs)
                .Include(s => s.TypingLogs)
                .Include(s => s.CopyLogs)
                .Where(s => s.StartTime.Date == date.Date)
                .ToList();
    }
}
