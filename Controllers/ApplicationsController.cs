using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using JobTracker.Data;
using JobTracker.Models;
using JobTracker.Services;

namespace JobTracker.Controllers;

public class ApplicationsController : Controller
{
    private readonly AppDbContext _db;
    private readonly JobScraperService _scraper;

    public ApplicationsController(AppDbContext db, JobScraperService scraper)
    {
        _db = db;
        _scraper = scraper;
    }

    // GET: /Applications/FetchJobInfo?url=...  (AJAX)
    [HttpGet]
    public async Task<IActionResult> FetchJobInfo(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest(new { error = "missing_url" });

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
            return BadRequest(new { error = "invalid_url" });

        var result = await _scraper.FetchAsync(url);
        return Ok(result);
    }

    // GET: /Applications/Export
    [HttpGet]
    public async Task<IActionResult> Export()
    {
        var apps = await _db.JobApplications.OrderBy(a => a.CreatedAt).ToListAsync();

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(apps, options);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var filename = $"jobtracker-export-{DateTime.Today:yyyy-MM-dd}.json";

        return File(bytes, "application/json", filename);
    }

    // POST: /Applications/Import
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Aucun fichier sélectionné.";
            return RedirectToAction(nameof(Index));
        }

        if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Le fichier doit être au format JSON.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            using var stream = file.OpenReadStream();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var imported = await JsonSerializer.DeserializeAsync<List<JobApplication>>(stream, options);

            if (imported == null || imported.Count == 0)
            {
                TempData["Error"] = "Le fichier est vide ou invalide.";
                return RedirectToAction(nameof(Index));
            }

            // Get existing IDs to avoid duplicates
            var existingIds = await _db.JobApplications.Select(a => a.Id).ToHashSetAsync();

            int added = 0, skipped = 0;
            foreach (var app in imported)
            {
                if (existingIds.Contains(app.Id))
                {
                    skipped++;
                    continue;
                }
                // Reset ID so SQLite auto-assigns a new one if needed
                app.Id = 0;
                app.UpdatedAt = DateTime.UtcNow;
                _db.JobApplications.Add(app);
                added++;
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = skipped > 0
                ? $"{added} candidature(s) importée(s), {skipped} ignorée(s) (déjà présentes)."
                : $"{added} candidature(s) importée(s) avec succès.";
        }
        catch (JsonException)
        {
            TempData["Error"] = "Fichier JSON invalide ou corrompu.";
        }
        catch (Exception)
        {
            TempData["Error"] = "Une erreur est survenue lors de l'import.";
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: /Applications
    public async Task<IActionResult> Index(string? search, ApplicationStatus? status, string sort = "date")
    {
        // Auto-transition 1: past interview dates → corresponding waiting status
        var pastInterviews = await _db.JobApplications
            .Where(a => a.InterviewDate < DateTime.Today && (
                a.Status == ApplicationStatus.EntretienRecruteur ||
                a.Status == ApplicationStatus.EntretienTech      ||
                a.Status == ApplicationStatus.TestTechnique      ||
                a.Status == ApplicationStatus.EntretienRH))
            .ToListAsync();

        foreach (var a in pastInterviews)
        {
            a.Status = a.Status switch
            {
                ApplicationStatus.EntretienRecruteur => ApplicationStatus.AttenteRecruteur,
                ApplicationStatus.EntretienTech      => ApplicationStatus.AttenteTech,
                ApplicationStatus.TestTechnique      => ApplicationStatus.AttenteTest,
                ApplicationStatus.EntretienRH        => ApplicationStatus.AttenteRH,
                _                                    => a.Status
            };
            a.InterviewDate = null;
            a.UpdatedAt = DateTime.UtcNow;
        }

        // Auto-transition 2: Candidature with no response after 3 weeks → SansReponse
        var threeWeeksAgo = DateTime.Today.AddDays(-21);
        var staleApplications = await _db.JobApplications
            .Where(a => a.Status == ApplicationStatus.Candidature && a.AppliedDate <= threeWeeksAgo)
            .ToListAsync();

        foreach (var a in staleApplications)
        {
            a.Status = ApplicationStatus.SansReponse;
            a.LastResponseDate = a.AppliedDate.AddDays(21);
            a.UpdatedAt = DateTime.UtcNow;
        }

        if (pastInterviews.Count > 0 || staleApplications.Count > 0)
            await _db.SaveChangesAsync();

        var query = _db.JobApplications.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.CompanyName.Contains(search) || a.JobTitle.Contains(search));

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var list = await query.ToListAsync();

        list = sort switch
        {
            "company" => list.OrderBy(a => a.CompanyName).ToList(),
            "status"  => list.OrderBy(a => a.Status).ThenBy(a => a.InterviewDate ?? DateTime.MaxValue).ThenByDescending(a => a.AppliedDate).ToList(),
            _         => list
                .OrderBy(a => a.InterviewDate.HasValue && a.InterviewDate.Value >= DateTime.Today ? 0 : 1)  // upcoming events first
                .ThenBy(a => a.InterviewDate.HasValue && a.InterviewDate.Value >= DateTime.Today ? a.InterviewDate.Value : DateTime.MaxValue)  // nearest first
                .ThenBy(a => StatusSortOrder(a.Status))
                .ThenBy(a => a.InterviewDate ?? DateTime.MaxValue)
                .ThenByDescending(a => a.AppliedDate)
                .ToList()
        };

        ViewBag.Search = search;
        ViewBag.StatusFilter = status;
        ViewBag.Sort = sort;

        return View(list);
    }

    private static int StatusSortOrder(ApplicationStatus s) => s switch
    {
        ApplicationStatus.Offre              => 0,
        ApplicationStatus.AttenteRH          => 1,
        ApplicationStatus.EntretienRH        => 2,
        ApplicationStatus.AttenteTest        => 3,
        ApplicationStatus.TestTechnique      => 4,
        ApplicationStatus.AttenteTech        => 5,
        ApplicationStatus.EntretienTech      => 6,
        ApplicationStatus.AttenteRecruteur   => 7,
        ApplicationStatus.EntretienRecruteur => 8,
        ApplicationStatus.AppelRecruteur     => 9,
        ApplicationStatus.Candidature        => 10,
        ApplicationStatus.Refuse             => 11,
        ApplicationStatus.SansReponse        => 12,
        _                                    => 99
    };

    // GET: /Applications/Create
    public IActionResult Create() => View(new JobApplication { AppliedDate = DateTime.Today });

    // POST: /Applications/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(JobApplication model)
    {
        if (!ModelState.IsValid) return View(model);
        model.CreatedAt = DateTime.UtcNow;
        model.UpdatedAt = DateTime.UtcNow;
        _db.JobApplications.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Candidature chez {model.CompanyName} ajoutée !";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Applications/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var app = await _db.JobApplications.FindAsync(id);
        if (app == null) return NotFound();
        return View(app);
    }

    // POST: /Applications/Edit/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, JobApplication model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        model.UpdatedAt = DateTime.UtcNow;
        _db.JobApplications.Update(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Candidature mise à jour.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Applications/Delete/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var app = await _db.JobApplications.FindAsync(id);
        if (app != null)
        {
            _db.JobApplications.Remove(app);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Candidature chez {app.CompanyName} supprimée.";
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: /Applications/UpdateInterviewDate  (AJAX)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateInterviewDate(int id, DateTime? interviewDate)
    {
        var app = await _db.JobApplications.FindAsync(id);
        if (app == null) return NotFound();
        app.InterviewDate = interviewDate;
        app.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok();
    }

    // POST: /Applications/QuickStatus  (AJAX)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickStatus(int id, ApplicationStatus status)
    {
        var app = await _db.JobApplications.FindAsync(id);
        if (app == null) return NotFound();
        app.Status = status;
        app.UpdatedAt = DateTime.UtcNow;

        // AppelRecruteur: record today as the event date automatically
        if (status == ApplicationStatus.AppelRecruteur)
            app.InterviewDate = DateTime.Today;
        // Manually setting an interview status: clear old date so auto-transition doesn't re-fire
        else if (status is ApplicationStatus.EntretienRecruteur or ApplicationStatus.EntretienTech
                         or ApplicationStatus.TestTechnique     or ApplicationStatus.EntretienRH)
            app.InterviewDate = null;
        // Waiting/terminal statuses: clear date so the entry doesn't stay pinned at the top of the sort
        else if (status is ApplicationStatus.AttenteRecruteur or ApplicationStatus.AttenteTech
                        or ApplicationStatus.AttenteTest      or ApplicationStatus.AttenteRH
                        or ApplicationStatus.Refuse           or ApplicationStatus.SansReponse)
            app.InterviewDate = null;

        if (status is not ApplicationStatus.Candidature and not ApplicationStatus.AppelRecruteur)
            app.LastResponseDate = DateTime.Today;

        await _db.SaveChangesAsync();
        return Ok();
    }
}
