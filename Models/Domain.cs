using System.ComponentModel.DataAnnotations;

namespace SidInBeurs.Models;

public static class Roles
{
    public const string Exhibitor = "Beursexposant";
    public const string Marketing = "Marketing medewerker";
    public const string TeamLead = "Marketing teamlead";
    public const string Admin = "Administrator";
}

public sealed class AppDatabase
{
    public List<Visitor> Visitors { get; set; } = [];
    public List<ProgramInterest> Programs { get; set; } = [];
    public List<Fair> Fairs { get; set; } = [];
    public List<InterestRegistration> Registrations { get; set; } = [];
    public List<AppUser> Users { get; set; } = [];
}

public sealed class Visitor
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public string VisitorCode { get; set; } = "";
    [Required] public string FirstName { get; set; } = "";
    [Required] public string LastName { get; set; } = "";
    [DataType(DataType.Date)] public DateOnly BirthDate { get; set; }
    [Required] public string CurrentSchool { get; set; } = "";
    [Required] public string CurrentStudyArea { get; set; } = "";
}

public sealed class ProgramInterest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Faculty { get; set; } = "";
}

public sealed class Fair
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string City { get; set; } = "";
    public DateOnly Date { get; set; }
}

public sealed class InterestRegistration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VisitorId { get; set; }
    public Guid FairId { get; set; }
    public List<Guid> ProgramIds { get; set; } = [];
    public string Notes { get; set; } = "";
    public DateTimeOffset RegisteredAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = Roles.Marketing;
    public string PasswordHash { get; set; } = "";
    public string InviteToken { get; set; } = "";
    public bool IsActive { get; set; }
}

public sealed record DashboardRow(Visitor Visitor, Fair Fair, InterestRegistration Registration);
public sealed record CountRow(ProgramInterest Program, int Total, Dictionary<string, int> PerFair);
