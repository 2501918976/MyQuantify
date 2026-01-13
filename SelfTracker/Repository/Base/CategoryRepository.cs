using Microsoft.EntityFrameworkCore;
using SelfTracker.Entity.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfTracker.Repository.Base
{
    public class CategoryRepository
    {
        private readonly QuantifyDbContext _db;
        public CategoryRepository(QuantifyDbContext db) => _db = db;

        public void Add(Category category)
        {
            _db.Categories.Add(category);
            _db.SaveChanges();
        }

        public void Update(Category category)
        {
            _db.Categories.Update(category);
            _db.SaveChanges();
        }

        public void Remove(Category category)
        {
            _db.Categories.Remove(category);
            _db.SaveChanges();
        }

        public Category? GetById(int id) =>
            _db.Categories.Include(c => c.Rules).FirstOrDefault(c => c.Id == id);

        public IEnumerable<Category> GetAll() =>
            _db.Categories.Include(c => c.Rules).ToList();
    }
}
