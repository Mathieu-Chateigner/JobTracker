using System.ComponentModel.DataAnnotations;

namespace JobTracker.Models;

public enum ApplicationStatus
{
    Candidature,        // 0
    AppelRecruteur,     // 1 — recruiter reached out, no application sent
    EntretienRecruteur, // 2
    AttenteRecruteur,   // 3 — waiting after recruiter interview
    EntretienTech,      // 4
    AttenteTech,        // 5 — waiting after tech interview
    TestTechnique,      // 6
    AttenteTest,        // 7 — waiting after technical test
    EntretienRH,        // 8
    AttenteRH,          // 9 — waiting after HR interview
    Offre,              // 10
    Refuse,             // 11
    SansReponse         // 12
}

public static class StatusLabels
{
    public static string ToFrench(this ApplicationStatus status) => status switch
    {
        ApplicationStatus.Candidature        => "Candidature",
        ApplicationStatus.AppelRecruteur     => "Appel recruteur",
        ApplicationStatus.EntretienRecruteur => "Entretien recruteur",
        ApplicationStatus.AttenteRecruteur   => "En attente (recruteur)",
        ApplicationStatus.EntretienTech      => "Entretien tech",
        ApplicationStatus.AttenteTech        => "En attente (tech)",
        ApplicationStatus.TestTechnique      => "Test technique",
        ApplicationStatus.AttenteTest        => "En attente (test)",
        ApplicationStatus.EntretienRH        => "Entretien RH",
        ApplicationStatus.AttenteRH          => "En attente (RH)",
        ApplicationStatus.Offre              => "Offre",
        ApplicationStatus.Refuse             => "Refusé",
        ApplicationStatus.SansReponse        => "Sans réponse",
        _                                    => status.ToString()
    };
}

public class JobApplication
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string JobTitle { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? JobUrl { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    [Required]
    public DateTime AppliedDate { get; set; } = DateTime.Today;

    public DateTime? LastResponseDate { get; set; }

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Candidature;

    public DateTime? InterviewDate { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [Range(0, 9999999)]
    public int? SalaryExpectation { get; set; }

    [Range(0, 5)]
    public int? Rating { get; set; }

    [MaxLength(500)]
    public string? RefusalMailUrl { get; set; }

    [MaxLength(500)]
    public string? TeamsUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
