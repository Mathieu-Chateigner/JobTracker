using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Models;

namespace JobTracker.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) { _db = db; }

    public async Task<IActionResult> Index()
    {
        var apps = await _db.JobApplications.ToListAsync();

        var stats = new DashboardStats
        {
            Total = apps.Count,
            InProgress = apps.Count(a => a.Status is ApplicationStatus.AppelRecruteur
                                                   or ApplicationStatus.EntretienRecruteur
                                                   or ApplicationStatus.AttenteRecruteur
                                                   or ApplicationStatus.EntretienTech
                                                   or ApplicationStatus.AttenteTech
                                                   or ApplicationStatus.TestTechnique
                                                   or ApplicationStatus.AttenteTest
                                                   or ApplicationStatus.EntretienRH
                                                   or ApplicationStatus.AttenteRH),
            Offers = apps.Count(a => a.Status == ApplicationStatus.Offre),
            Refused = apps.Count(a => a.Status == ApplicationStatus.Refuse),
            NoResponse = apps.Count(a => a.Status == ApplicationStatus.SansReponse),
            RecentApplications = apps.OrderByDescending(a => a.AppliedDate).Take(5).ToList(),
            StatusBreakdown = apps.GroupBy(a => a.Status)
                                  .ToDictionary(g => g.Key, g => g.Count())
        };

        return View(stats);
    }
}

public class DashboardStats
{
    public int Total { get; set; }
    public int InProgress { get; set; }
    public int Offers { get; set; }
    public int Refused { get; set; }
    public int NoResponse { get; set; }
    public List<JobApplication> RecentApplications { get; set; } = new();
    public Dictionary<ApplicationStatus, int> StatusBreakdown { get; set; } = new();
}
