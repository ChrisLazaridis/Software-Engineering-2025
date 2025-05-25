using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ScottPlot;
using SoftEng2025.Data;
using SoftEng2025.Models;

namespace SoftEng2025.Pages.Entrepreneur
{
    public class StatisticsModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public StatisticsModel(ApplicationDbContext db) => _db = db;

        [BindProperty(SupportsGet = true)]
        public int RestaurantId { get; set; }

        public string RestaurantName { get; set; }

        // initial GET renders the page and stores name/id
        public async Task<IActionResult> OnGetAsync(int id)
        {
            var r = await _db.Restaurants
                             .AsNoTracking()
                             .FirstOrDefaultAsync(x => x.RestaurantId == id);
            if (r == null) return NotFound();

            RestaurantId = id;
            RestaurantName = r.Name;
            return Page();
        }

        // /Entrepreneur/Statistics?handler=ReviewCountChart&id=123
        public FileResult OnGetReviewCountChart(int id)
        {
            DateTime today = DateTime.UtcNow.Date;
            DateTime from = today.AddDays(-29);

            // gather counts per day
            var countsByDay = _db.Reviews
                .Where(rv => rv.RestaurantId == id
                          && rv.CreatedAt.Date >= from
                          && rv.CreatedAt.Date <= today)
                .AsEnumerable()
                .GroupBy(rv => rv.CreatedAt.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            // build a 30-day window of Xs and Ys
            var dates = Enumerable.Range(0, 30)
                                   .Select(i => from.AddDays(i))
                                   .ToArray();
            double[] xs = dates.Select(d => d.ToOADate()).ToArray();
            double[] counts = dates.Select(d => (double)(countsByDay.TryGetValue(d, out var c) ? c : 0))
                                   .ToArray();

            // plot and manually set each bar’s Position
            var plt = new ScottPlot.Plot();
            var barSeries = plt.Add.Bars(counts);
            for (int i = 0; i < barSeries.Bars.Count; i++)
                barSeries.Bars[i].Position = xs[i];  // set X location per-bar :contentReference[oaicite:2]{index=2}

            plt.Axes.DateTimeTicksBottom();
            plt.Axes.SetLimits(xs.First(), xs.Last(), 0, counts.Max());

            // export PNG bytes
            byte[] imageBytes = plt.GetImageBytes(700, 300, ImageFormat.Png);
            return File(imageBytes, "image/png");
        }

        // /Entrepreneur/Statistics?handler=RatingTrendChart&id=123
        public FileResult OnGetRatingTrendChart(int id)
        {
            DateTime today = DateTime.UtcNow.Date;
            DateTime from = today.AddDays(-29);

            var avgByDay = _db.Reviews
                .Where(rv => rv.RestaurantId == id
                          && rv.CreatedAt.Date >= from
                          && rv.CreatedAt.Date <= today)
                .AsEnumerable()
                .GroupBy(rv => rv.CreatedAt.Date)
                .ToDictionary(
                    g => g.Key,
                    g => g.Average(rv => (double?)(rv.Average ?? 0)) ?? 0
                );

            var dates = Enumerable.Range(0, 30)
                                  .Select(i => from.AddDays(i))
                                  .ToArray();
            double[] xs = dates.Select(d => d.ToOADate()).ToArray();
            double[] avgs = dates.Select(d => avgByDay.TryGetValue(d, out var a) ? a : 0)
                                 .ToArray();

            var plt = new ScottPlot.Plot();
            plt.Add.Scatter(xs, avgs);
            plt.Axes.DateTimeTicksBottom();
            plt.Axes.SetLimits(xs.First(), xs.Last(), 0, 5);

            byte[] imageBytes = plt.GetImageBytes(700, 300, ImageFormat.Png);
            return File(imageBytes, "image/png");
        }
    }
}
