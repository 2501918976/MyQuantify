using Microsoft.EntityFrameworkCore;
using SelfTracker.Entity.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfTracker.Repository.Base
{
    public class CategoryRuleRepository
    {
        private readonly QuantifyDbContext _db;
        public CategoryRuleRepository(QuantifyDbContext db) => _db = db;

        public void Add(CategoryRule rule)
        {
            _db.CategoryRules.Add(rule);
            _db.SaveChanges();
        }

        public void Update(CategoryRule rule)
        {
            _db.CategoryRules.Update(rule);
            _db.SaveChanges();
        }

        public void Remove(CategoryRule rule)
        {
            _db.CategoryRules.Remove(rule);
            _db.SaveChanges();
        }

        public CategoryRule? GetById(int id) =>
            _db.CategoryRules.Include(r => r.Category).FirstOrDefault(r => r.Id == id);

        public IEnumerable<CategoryRule> GetAll() =>
            _db.CategoryRules.Include(r => r.Category).ToList();
    }
}
