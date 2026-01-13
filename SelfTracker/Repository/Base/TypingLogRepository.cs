using Microsoft.EntityFrameworkCore;
using SelfTracker.Entity.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfTracker.Repository.Base
{
    public class TypingLogRepository
    {
        private readonly QuantifyDbContext _db;
        public TypingLogRepository(QuantifyDbContext db) => _db = db;

        public void Add(TypingLog log) { _db.TypingLogs.Add(log); _db.SaveChanges(); }
        public void Update(TypingLog log) { _db.TypingLogs.Update(log); _db.SaveChanges(); }
        public void Remove(TypingLog log) { _db.TypingLogs.Remove(log); _db.SaveChanges(); }

        public TypingLog? GetById(int id) =>
            _db.TypingLogs.Include(t => t.Process).ThenInclude(p => p.Category).FirstOrDefault(t => t.Id == id);

        public IEnumerable<TypingLog> GetAll() =>
            _db.TypingLogs.Include(t => t.Process).ThenInclude(p => p.Category).ToList();

        public IEnumerable<TypingLog> GetByDate(DateTime date) =>
            _db.TypingLogs.Include(t => t.Process)
                .ThenInclude(p => p.Category)
                .Where(t => t.StartTime.Date == date.Date)
                .ToList();
    }
}
