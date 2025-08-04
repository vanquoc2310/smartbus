using Microsoft.EntityFrameworkCore;
using SmartBus_BusinessObjects.Models;
using SmartBus_BusinessObjects.DTOS;
using System.Globalization;
using System.Linq.Expressions;
using System.Linq;
using Microsoft.EntityFrameworkCore.SqlServer;


namespace Backend_SmartBus.Services
{
    public class StatisticService
    {
        private readonly SmartBusContext _context;

        public StatisticService(SmartBusContext context)
        {
            _context = context;
        }

        public async Task<List<StatisticResult>> GetUserStatisticsAsync(StatisticFilterRequest request)
        {
            var query = _context.Users.Where(u => u.CreatedAt != null);
            return await GroupByTimeRangeAsync(query, u => u.CreatedAt!.Value, request.Range);
        }

        public async Task<List<StatisticResult>> GetTicketStatisticsAsync(StatisticFilterRequest request)
        {
            var query = _context.Tickets.Where(t => t.IssuedAt != null);
            return await GroupByTimeRangeAsync(query, t => t.IssuedAt!.Value, request.Range);
        }

        public async Task<List<StatisticResult>> GetRevenueStatisticsAsync(StatisticFilterRequest request)
        {
            var query = _context.Tickets.Where(t => t.IssuedAt != null && t.Price != null);
            return await GroupByTimeRangeAsync(query, t => t.IssuedAt!.Value, request.Range, t => t.Price ?? 0);
        }

        public async Task<List<StatisticResult>> GetTopSellingRoutesAsync(int top = 5)
        {
            return await _context.Tickets
                .Where(t => t.RouteId != null)
                .GroupBy(t => t.Route.RouteName)
                .Select(g => new StatisticResult
                {
                    Label = g.Key,
                    Value = g.Count()
                })
                .OrderByDescending(r => r.Value)
                .Take(top)
                .ToListAsync();
        }

        private Task<List<StatisticResult>> GroupByTimeRangeAsync<T>(
    IQueryable<T> query,
    Expression<Func<T, DateTime>> dateSelector,
    TimeRange range,
    Expression<Func<T, decimal>>? valueSelector = null)
        {
            var compiledDateSelector = dateSelector.Compile();
            var compiledValueSelector = valueSelector?.Compile();
            var now = DateTime.UtcNow;

            switch (range)
            {
                case TimeRange.Daily:
                    var today = now.Date;
                    return Task.FromResult(
                        query.AsEnumerable()
                            .Where(e => compiledDateSelector(e).Date == today)
                            .GroupBy(_ => today)
                            .Select(g => new StatisticResult
                            {
                                Label = today.ToString("yyyy-MM-dd"),
                                Value = compiledValueSelector == null ? g.Count() : g.Sum(compiledValueSelector)
                            })
                            .ToList()
                    );

                case TimeRange.Weekly:
                    var last7Days = now.AddDays(-6).Date;
                    return Task.FromResult(
                        query.AsEnumerable()
                            .Where(e => compiledDateSelector(e).Date >= last7Days)
                            .GroupBy(e => compiledDateSelector(e).Date)
                            .Select(g => new StatisticResult
                            {
                                Label = g.Key.ToString("yyyy-MM-dd"),
                                Value = compiledValueSelector == null ? g.Count() : g.Sum(compiledValueSelector)
                            })
                            .OrderByDescending(r => r.Label)
                            .ToList()
                    );

                case TimeRange.Monthly:
                    var fourWeeksAgo = now.AddDays(-27).Date; // 4 tuần ~ 28 ngày
                    return Task.FromResult(
                        query.AsEnumerable()
                            .Where(e => compiledDateSelector(e).Date >= fourWeeksAgo)
                            .GroupBy(e =>
                            {
                                var date = compiledDateSelector(e);
                                var ci = CultureInfo.CurrentCulture;
                                return ci.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                            })
                            .Select(g => new StatisticResult
                            {
                                Label = $"Tuần {g.Key}",
                                Value = compiledValueSelector == null ? g.Count() : g.Sum(compiledValueSelector)
                            })
                            .OrderByDescending(r => r.Label)
                            .ToList()
                    );

                case TimeRange.Last6Months:
                    var sixMonthsAgo = now.AddMonths(-5); // Bao gồm cả tháng hiện tại
                    return Task.FromResult(
                        query.AsEnumerable()
                            .Where(e => compiledDateSelector(e).Date >= new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1))
                            .GroupBy(e => new { compiledDateSelector(e).Year, compiledDateSelector(e).Month })
                            .Select(g => new StatisticResult
                            {
                                Label = $"Tháng {g.Key.Month}/{g.Key.Year}",
                                Value = compiledValueSelector == null ? g.Count() : g.Sum(compiledValueSelector)
                            })
                            .OrderByDescending(r => r.Label)
                            .ToList()
                    );

                case TimeRange.Yearly:
                    var startOfYear = new DateTime(now.Year, 1, 1);
                    return Task.FromResult(
                        query.AsEnumerable()
                            .Where(e => compiledDateSelector(e).Date >= startOfYear)
                            .GroupBy(e => compiledDateSelector(e).Month)
                            .Select(g => new StatisticResult
                            {
                                Label = $"Tháng {g.Key}/{now.Year}",
                                Value = compiledValueSelector == null ? g.Count() : g.Sum(compiledValueSelector)
                            })
                            .OrderByDescending(r => r.Label)
                            .ToList()
                    );

                default:
                    return Task.FromResult(new List<StatisticResult>());
            }
        }


    }

}
