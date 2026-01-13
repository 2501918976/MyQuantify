using Microsoft.EntityFrameworkCore;
using SelfTracker.Entity.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfTracker.Repository.Base
{
    public class ProcessInfoRepository
    {
        private readonly QuantifyDbContext _db;
        public ProcessInfoRepository(QuantifyDbContext db) => _db = db;

        public void Add(ProcessInfo process)
        {
            _db.Processes.Add(process);
            _db.SaveChanges();
        }

        public void Update(ProcessInfo process)
        {
            _db.Processes.Update(process);
            _db.SaveChanges();
        }

        public void Remove(ProcessInfo process)
        {
            _db.Processes.Remove(process);
            _db.SaveChanges();
        }

        public ProcessInfo? GetById(int id) =>
            _db.Processes.Include(p => p.Category).FirstOrDefault(p => p.Id == id);

        public IEnumerable<ProcessInfo> GetAll() =>
            _db.Processes.Include(p => p.Category).ToList();
    }
}
