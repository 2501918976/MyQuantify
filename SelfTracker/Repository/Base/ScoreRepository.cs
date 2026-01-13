using SelfTracker.Entity.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfTracker.Repository.Base
{
    public class ScoreRepository
    {
        private readonly QuantifyDbContext _db;

        public ScoreRepository(QuantifyDbContext db) => _db = db;

        // 添加得分记录
        public void Add(Score score)
        {
            _db.Scores.Add(score);
            _db.SaveChanges();
        }

        // 更新得分记录
        public void Update(Score score)
        {
            _db.Scores.Update(score);
            _db.SaveChanges();
        }

        // 获取特定日期的得分记录 (因为 Time 包含时间，所以用 .Date 比较)
        public Score? GetByDate(DateTime date)
        {
            return _db.Scores
                .FirstOrDefault(s => s.Time.Date == date.Date);
        }

        // 获取最新的得分记录 (用于 today.html 首页显示)
        public Score? GetLatestScore()
        {
            return _db.Scores
                .OrderByDescending(s => s.Time)
                .FirstOrDefault();
        }

        // 【推荐使用】保存或更新今日得分：防止同一天出现多条记录
        public void AddOrUpdateDailyScore(int efficiencyScore)
        {
            var today = DateTime.Today;
            var existingScore = GetByDate(today);

            if (existingScore != null)
            {
                // 更新今日已存在的得分
                existingScore.EfficiencyScore = efficiencyScore;
                existingScore.LastUpdated = DateTime.Now;
                Update(existingScore);
            }
            else
            {
                // 创建今日新记录
                var newScore = new Score
                {
                    Time = today,
                    EfficiencyScore = efficiencyScore,
                    LastUpdated = DateTime.Now
                };
                Add(newScore);
            }
        }

        // 获取最近一段时间的得分趋势 (可用于画折线图)
        public IEnumerable<Score> GetTrend(int days)
        {
            return _db.Scores
                .Where(s => s.Time >= DateTime.Today.AddDays(-days))
                .OrderBy(s => s.Time)
                .ToList();
        }
    }
}
