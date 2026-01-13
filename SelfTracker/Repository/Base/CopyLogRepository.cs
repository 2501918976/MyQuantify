using Microsoft.EntityFrameworkCore;
using SelfTracker.Entity.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfTracker.Repository.Base
{
    public class CopyLogRepository
    {
        private readonly QuantifyDbContext _db;
        public CopyLogRepository(QuantifyDbContext db) => _db = db;

        public void Add(CopyLog log) { _db.CopyLogs.Add(log); _db.SaveChanges(); }
        public void Update(CopyLog log) { _db.CopyLogs.Update(log); _db.SaveChanges(); }
        public void Remove(CopyLog log) { _db.CopyLogs.Remove(log); _db.SaveChanges(); }

        public CopyLog? GetById(int id) =>
            _db.CopyLogs.Include(c => c.Process).ThenInclude(p => p.Category).FirstOrDefault(c => c.Id == id);

        public IEnumerable<CopyLog> GetAll() =>
            _db.CopyLogs.Include(c => c.Process).ThenInclude(p => p.Category).ToList();

        public IEnumerable<CopyLog> GetByDate(DateTime date) =>
            _db.CopyLogs.Include(c => c.Process).ThenInclude(p => p.Category)
                .Where(c => c.StartTime.Date == date.Date)
                .ToList();
    }
}
