using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace JobTracker.Services;

public class JobInfoResult
{
    public bool Success { get; set; }
    public string? JobTitle { get; set; }
    public string? CompanyName { get; set; }
    public string? Location { get; set; }
    public string? Error { get; set; }
}

public class JobScraperService
{
    private readonly ILogger<JobScraperService> _logger;

    public JobScraperService(ILogger<JobScraperService> logger)
    {
        _logger = logger;
    }

    public async Task<JobInfoResult> FetchAsync(string url)
    {
        IPlaywright? playwright = null;
        IBrowser? browser = null;

        try
        {
            playwright = await Playwright.CreateAsync();

            browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[]
                {
                    "--no-sandbox",
                    "--disable-blink-features=AutomationControlled"
                }
            });

            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                            "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
                Locale = "fr-FR",
                ViewportSize = new ViewportSize { Width = 1280, Height = 900 }
            });

            // Mask webdriver fingerprint
            await context.AddInitScriptAsync(
                "Object.defineProperty(navigator, 'webdriver', { get: () => undefined })");

            var page = await context.NewPageAsync();

            await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 20000
            });

            // Give JS a moment to render dynamic content
            await page.WaitForTimeoutAsync(2000);

            var html = await page.ContentAsync();
            var result = new JobInfoResult { Success = true };

            // --- Strategy 1: JSON-LD structured data ---
            var jsonLdHandles = await page.QuerySelectorAllAsync("script[type='application/ld+json']");
            foreach (var handle in jsonLdHandles)
            {
                try
                {
                    var text = await handle.InnerTextAsync();
                    if (string.IsNullOrWhiteSpace(text)) continue;

                    var json = JsonDocument.Parse(text);
                    var root = json.RootElement;

                    if (root.TryGetProperty("@type", out var type) && type.GetString() == "JobPosting")
                    {
                        if (root.TryGetProperty("title", out var title))
                            result.JobTitle = title.GetString()?.Trim();

                        if (root.TryGetProperty("hiringOrganization", out var org))
                            if (org.TryGetProperty("name", out var orgName))
                                result.CompanyName = orgName.GetString()?.Trim();

                        if (root.TryGetProperty("jobLocation", out var loc))
                        {
                            var locEl = loc.ValueKind == JsonValueKind.Array ? loc[0] : loc;
                            if (locEl.TryGetProperty("address", out var addr))
                            {
                                var parts = new List<string>();
                                if (addr.TryGetProperty("addressLocality", out var city) && !string.IsNullOrEmpty(city.GetString()))
                                    parts.Add(city.GetString()!);
                                if (addr.TryGetProperty("addressRegion", out var region) && !string.IsNullOrEmpty(region.GetString()))
                                    parts.Add(region.GetString()!);
                                if (addr.TryGetProperty("addressCountry", out var country) && !string.IsNullOrEmpty(country.GetString()))
                                    parts.Add(country.GetString()!);
                                if (parts.Count > 0)
                                    result.Location = string.Join(", ", parts);
                            }
                        }

                        if (!string.IsNullOrEmpty(result.JobTitle))
                            return result;
                    }
                }
                catch (JsonException) { }
            }

            // --- Strategy 2: Indeed data-testid attributes (rendered DOM) ---
            result.JobTitle ??= await GetTextAsync(page,
                "[data-testid='jobsearch-JobInfoHeader-title']",
                "h1.jobTitle",
                "h1[class*='jobTitle']");

            result.CompanyName ??= await GetTextAsync(page,
                "[data-testid='inlineHeader-companyName'] a",
                "[data-testid='inlineHeader-companyName']",
                "span[data-testid='company-name']",
                "[data-testid='companyName']");

            result.Location ??= await GetTextAsync(page,
                "[data-testid='inlineHeader-companyLocation']",
                "[data-testid='jobsearch-JobInfoHeader-companyLocation']",
                "div[data-testid='job-location']");

            // --- Strategy 3: og:title / page title fallback ---
            if (string.IsNullOrEmpty(result.JobTitle) && string.IsNullOrEmpty(result.CompanyName))
            {
                var ogTitle = await page.GetAttributeAsync("meta[property='og:title']", "content")
                           ?? await page.TitleAsync();
                if (!string.IsNullOrEmpty(ogTitle))
                    ParseIndeedTitle(ogTitle, result);
            }

            result.Success = !string.IsNullOrEmpty(result.JobTitle) || !string.IsNullOrEmpty(result.CompanyName);
            if (!result.Success) result.Error = "no_data";

            return result;
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Timeout fetching {Url}", url);
            return new JobInfoResult { Success = false, Error = "timeout" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching {Url}", url);
            return new JobInfoResult { Success = false, Error = "unexpected" };
        }
        finally
        {
            if (browser != null) await browser.CloseAsync();
            playwright?.Dispose();
        }
    }

    // Try multiple selectors in order, return first match
    private static async Task<string?> GetTextAsync(IPage page, params string[] selectors)
    {
        foreach (var selector in selectors)
        {
            try
            {
                var el = await page.QuerySelectorAsync(selector);
                if (el == null) continue;
                var text = (await el.InnerTextAsync())?.Trim();
                if (!string.IsNullOrEmpty(text)) return text;
            }
            catch { }
        }
        return null;
    }

    // "Job Title - Company - City | Indeed"  or  "Job Title chez Company - City | Indeed"
    private static void ParseIndeedTitle(string raw, JobInfoResult result)
    {
        var clean = Regex.Replace(raw, @"\s*\|.*$", "").Trim();
        clean = Regex.Replace(clean, @"\s*[–—]\s*Indeed.*$", "", RegexOptions.IgnoreCase).Trim();

        var parts = Regex.Split(clean, @"\s+(?:-|chez)\s+");

        if (parts.Length >= 1 && string.IsNullOrEmpty(result.JobTitle))
            result.JobTitle = parts[0].Trim();
        if (parts.Length >= 2 && string.IsNullOrEmpty(result.CompanyName))
            result.CompanyName = parts[1].Trim();
        if (parts.Length >= 3 && string.IsNullOrEmpty(result.Location))
            result.Location = parts[2].Trim();
    }
}
