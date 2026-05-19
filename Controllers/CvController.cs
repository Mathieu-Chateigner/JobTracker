using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Controllers;

public class CvController : Controller
{
    private readonly IWebHostEnvironment _env;

    public CvController(IWebHostEnvironment env)
    {
        _env = env;
    }

    // GET: /Cv
    public IActionResult Index()
    {
        var cvPath = Path.Combine(_env.WebRootPath, "cv", "cv.pdf");
        ViewBag.HasCv = System.IO.File.Exists(cvPath);
        return View();
    }

    // POST: /Cv/Upload
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Aucun fichier sélectionné.";
            return RedirectToAction(nameof(Index));
        }

        if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Le fichier doit être au format PDF.";
            return RedirectToAction(nameof(Index));
        }

        var cvDir = Path.Combine(_env.WebRootPath, "cv");
        Directory.CreateDirectory(cvDir);
        var cvPath = Path.Combine(cvDir, "cv.pdf");

        using var stream = new FileStream(cvPath, FileMode.Create);
        await file.CopyToAsync(stream);

        TempData["Success"] = "CV mis à jour avec succès.";
        return RedirectToAction(nameof(Index));
    }
}
