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

        public async Task<List<TopSellingRouteDto>> GetTopSellingRoutesAsync(int top = 5)
        {
            return await _context.Tickets
                .Where(t => t.RouteId != null && t.Price != null)
                .GroupBy(t => t.Route.RouteName)
                .Select(g => new TopSellingRouteDto
                {
                    RouteName = g.Key,
                    Label = g.Key,
                    TicketsSold = g.Sum(t => t.TicketTypeId == 3 ? 30 : 1),
                    Value = g.Sum(t => t.TicketTypeId == 3 ? 30 : 1),
                    Revenue = g.Sum(t => t.Price ?? 0)
                })
                .OrderByDescending(r => r.TicketsSold)
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

            List<StatisticResult> results;

            switch (range)
            {
                case TimeRange.Daily:
                    var today = now.Date;
                    results = query.AsEnumerable()
                        .Where(e => compiledDateSelector(e).Date == today)
                        .GroupBy(_ => today)
                        .Select(g => new StatisticResult
                        {
                            Label = today.ToString("yyyy-MM-dd"),
                            Value = compiledValueSelector == null ? g.Count() : g.Sum(compiledValueSelector)
                        })
                        .ToList();

                    // Đảm bảo luôn có label hôm nay
                    if (!results.Any())
                    {
                        results.Add(new StatisticResult
                        {
                            Label = today.ToString("yyyy-MM-dd"),
                            Value = 0
                        });
                    }
                    break;

                case TimeRange.Weekly:
                    var last7Days = now.AddDays(-6).Date;
                    var allDays = Enumerable.Range(0, 7).Select(i => last7Days.AddDays(i)).ToList();

                    results = query.AsEnumerable()
                        .Where(e => compiledDateSelector(e).Date >= last7Days)
                        .GroupBy(e => compiledDateSelector(e).Date)
                        .Select(g => new StatisticResult
                        {
                            Label = g.Key.ToString("yyyy-MM-dd"),
                            Value = compiledValueSelector == null ? g.Count() : g.Sum(compiledValueSelector)
                        })
                        .ToList();

                    // Bổ sung các ngày còn thiếu
                    foreach (var day in allDays)
                    {
                        if (!results.Any(r => r.Label == day.ToString("yyyy-MM-dd")))
                        {
                            results.Add(new StatisticResult
                            {
                                Label = day.ToString("yyyy-MM-dd"),
                                Value = 0
                            });
                        }
                    }

                    results = results.OrderBy(r => r.Label).ToList();
                    break;

                case TimeRange.Monthly:
                    var fourWeeksAgo = now.AddDays(-27).Date;
                    var weekNumbers = Enumerable.Range(0, 4)
                        .Select(i => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                            fourWeeksAgo.AddDays(i * 7),
                            CalendarWeekRule.FirstFourDayWeek,
                            DayOfWeek.Monday))
                        .Distinct()
                        .ToList();

                    results = query.AsEnumerable()
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
                        .ToList();

                    // Bổ sung tuần thiếu
                    foreach (var week in weekNumbers)
                    {
                        if (!results.Any(r => r.Label == $"Tuần {week}"))
                        {
                            results.Add(new StatisticResult
                            {
                                Label = $"Tuần {week}",
                                Value = 0
                            });
                        }
                    }

                    results = results.OrderBy(r => r.Label).ToList();
                    break;

                case TimeRange.Last6Months:
                    var sixMonthsAgo = now.AddMonths(-5);
                    var allMonths = Enumerable.Range(0, 6)
                        .Select(i => new { Month = sixMonthsAgo.AddMonths(i).Month, Year = sixMonthsAgo.AddMonths(i).Year })
                        .ToList();

                    results = query.AsEnumerable()
                        .Where(e => compiledDateSelector(e).Date >= new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1))
                        .GroupBy(e => new { compiledDateSelector(e).Year, compiledDateSelector(e).Month })
                        .Select(g => new StatisticResult
                        {
                            Label = $"Tháng {g.Key.Month}/{g.Key.Year}",
                            Value = compiledValueSelector == null ? g.Count() : g.Sum(compiledValueSelector)
                        })
                        .ToList();

                    // Bổ sung tháng thiếu
                    foreach (var m in allMonths)
                    {
                        var label = $"Tháng {m.Month}/{m.Year}";
                        if (!results.Any(r => r.Label == label))
                        {
                            results.Add(new StatisticResult
                            {
                                Label = label,
                                Value = 0
                            });
                        }
                    }

                    results = results.OrderBy(r => r.Label).ToList();
                    break;

                case TimeRange.Yearly:
                    var startOfYear = new DateTime(now.Year, 1, 1);
                    var allYearMonths = Enumerable.Range(1, 12)
                        .Where(m => new DateTime(now.Year, m, 1) <= now)
                        .ToList();

                    results = query.AsEnumerable()
                        .Where(e => compiledDateSelector(e).Date >= startOfYear)
                        .GroupBy(e => compiledDateSelector(e).Month)
                        .Select(g => new StatisticResult
                        {
                            Label = $"Tháng {g.Key}/{now.Year}",
                            Value = compiledValueSelector == null ? g.Count() : g.Sum(compiledValueSelector)
                        })
                        .ToList();

                    // Bổ sung tháng thiếu
                    foreach (var m in allYearMonths)
                    {
                        var label = $"Tháng {m}/{now.Year}";
                        if (!results.Any(r => r.Label == label))
                        {
                            results.Add(new StatisticResult
                            {
                                Label = label,
                                Value = 0
                            });
                        }
                    }

                    results = results.OrderBy(r => r.Label).ToList();
                    break;

                default:
                    results = new List<StatisticResult>();
                    break;
            }

            return Task.FromResult(results);
        }


    }

}
