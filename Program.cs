using Microsoft.EntityFrameworkCore;
using JobTracker.Data;
using JobTracker.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=jobtracker.db"));

builder.Services.AddScoped<JobScraperService>();

var app = builder.Build();

// Auto-create DB, then apply any missing columns for existing DBs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // Safe migrations — ADD COLUMN is idempotent in SQLite if we check first
    var conn = db.Database.GetDbConnection();
    conn.Open();

    var columns = new HashSet<string>();
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "PRAGMA table_info(JobApplications)";
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) columns.Add(reader.GetString(1));
    }

    var alterations = new Dictionary<string, string>
    {
        ["RefusalMailUrl"]   = "ALTER TABLE JobApplications ADD COLUMN RefusalMailUrl TEXT",
        ["InterviewDate"]    = "ALTER TABLE JobApplications ADD COLUMN InterviewDate TEXT",
        ["TeamsUrl"]         = "ALTER TABLE JobApplications ADD COLUMN TeamsUrl TEXT",
        ["Rating"]           = "ALTER TABLE JobApplications ADD COLUMN Rating INTEGER",
        ["SalaryExpectation"] = null!  // type change handled below
    };

    foreach (var (col, sql) in alterations)
    {
        if (!columns.Contains(col) && sql != null)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }

    conn.Close();
}

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Home/Error");

app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
