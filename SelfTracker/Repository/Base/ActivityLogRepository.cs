using Microsoft.EntityFrameworkCore;
using SelfTracker.Entity.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfTracker.Repository.Base
{
    public class ActivityLogRepository
    {
        private readonly QuantifyDbContext _db;
        public ActivityLogRepository(QuantifyDbContext db) => _db = db;

        public void Add(ActivityLog log)
        {
            _db.ActivityLogs.Add(log);
            _db.SaveChanges();
        }

        public void Update(ActivityLog log)
        {
            _db.ActivityLogs.Update(log);
            _db.SaveChanges();
        }

        public void Remove(ActivityLog log)
        {
            _db.ActivityLogs.Remove(log);
            _db.SaveChanges();
        }

        public ActivityLog? GetById(int id) =>
            _db.ActivityLogs
                .Include(a => a.Process)
                .ThenInclude(p => p.Category)
                .Include(a => a.Process)
                .FirstOrDefault(a => a.Id == id);

        public IEnumerable<ActivityLog> GetAll() =>
            _db.ActivityLogs
                .Include(a => a.Process)
                .ThenInclude(p => p.Category)
                .ToList();

        public IEnumerable<ActivityLog> GetByDate(DateTime date) =>
            _db.ActivityLogs
                .Include(a => a.Process)
                .ThenInclude(p => p.Category)
                .Where(a => a.StartTime.Date == date.Date)
                .ToList();
    }
}
