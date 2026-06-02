using System.Security.Cryptography;
using System.Text.Json;
using SidInBeurs.Models;

namespace SidInBeurs.Data;

public sealed class AppRepository
{
    private readonly string _dbPath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    public AppRepository(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _dbPath = configuration["DatabasePath"] ?? Path.Combine(environment.ContentRootPath, "App_Data", "sidin-database.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        if (!File.Exists(_dbPath))
        {
            SaveUnsafe(Seed());
        }
    }

    public async Task<AppDatabase> GetAsync()
    {
        await _lock.WaitAsync();
        try { return LoadUnsafe(); }
        finally { _lock.Release(); }
    }

    public async Task SaveAsync(AppDatabase db)
    {
        await _lock.WaitAsync();
        try { SaveUnsafe(db); }
        finally { _lock.Release(); }
    }

    public async Task<AppUser?> ValidateUserAsync(string email, string password)
    {
        var db = await GetAsync();
        var user = db.Users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && u.IsActive);
        return user is not null && VerifyPassword(password, user.PasswordHash) ? user : null;
    }

    public async Task<string> CreateUserAsync(string email, string displayName, string role)
    {
        var db = await GetAsync();
        var existing = db.Users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.DisplayName = displayName;
            existing.Role = role;
            existing.InviteToken = Guid.NewGuid().ToString("N");
            existing.IsActive = false;
            existing.PasswordHash = "";
        }
        else
        {
            db.Users.Add(new AppUser
            {
                Email = email,
                DisplayName = displayName,
                Role = role,
                InviteToken = Guid.NewGuid().ToString("N")
            });
        }
        await SaveAsync(db);
        return db.Users.Single(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)).InviteToken;
    }

    public async Task<bool> CompleteInviteAsync(string token, string password)
    {
        var db = await GetAsync();
        var user = db.Users.FirstOrDefault(u => u.InviteToken == token);
        if (user is null) return false;
        user.PasswordHash = HashPassword(password);
        user.InviteToken = "";
        user.IsActive = true;
        await SaveAsync(db);
        return true;
    }

    public async Task UpsertVisitorRegistrationAsync(Visitor visitor, Guid fairId, List<Guid> programIds, string notes)
    {
        var db = await GetAsync();
        var existingVisitor = db.Visitors.FirstOrDefault(v => v.VisitorCode.Equals(visitor.VisitorCode, StringComparison.OrdinalIgnoreCase));
        if (existingVisitor is null)
        {
            existingVisitor = visitor;
            db.Visitors.Add(existingVisitor);
        }
        else
        {
            existingVisitor.FirstName = visitor.FirstName;
            existingVisitor.LastName = visitor.LastName;
            existingVisitor.BirthDate = visitor.BirthDate;
            existingVisitor.CurrentSchool = visitor.CurrentSchool;
            existingVisitor.CurrentStudyArea = visitor.CurrentStudyArea;
        }

        var registration = db.Registrations.FirstOrDefault(r => r.VisitorId == existingVisitor.Id && r.FairId == fairId);
        if (registration is null)
        {
            registration = new InterestRegistration { VisitorId = existingVisitor.Id, FairId = fairId };
            db.Registrations.Add(registration);
        }
        registration.ProgramIds = programIds.Distinct().ToList();
        registration.Notes = notes ?? "";
        registration.RegisteredAt = DateTimeOffset.UtcNow;
        await SaveAsync(db);
    }

    public async Task DeleteVisitorsAsync(IEnumerable<Guid> ids)
    {
        var idSet = ids.ToHashSet();
        var db = await GetAsync();
        db.Visitors.RemoveAll(v => idSet.Contains(v.Id));
        db.Registrations.RemoveAll(r => idSet.Contains(r.VisitorId));
        await SaveAsync(db);
    }

    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 120_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string stored)
    {
        var parts = stored.Split('.');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 120_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private AppDatabase LoadUnsafe() => JsonSerializer.Deserialize<AppDatabase>(File.ReadAllText(_dbPath), _json) ?? new AppDatabase();
    private void SaveUnsafe(AppDatabase db) => File.WriteAllText(_dbPath, JsonSerializer.Serialize(db, _json));

    private static AppDatabase Seed()
    {
        var fairs = new List<Fair>
        {
            new() { Name = "SID-in Antwerpen", City = "Antwerpen", Date = new DateOnly(2025, 1, 16) },
            new() { Name = "SID-in Gent", City = "Gent", Date = new DateOnly(2025, 1, 23) },
            new() { Name = "SID-in Kortrijk", City = "Kortrijk", Date = new DateOnly(2025, 2, 6) }
        };
        var programs = new List<ProgramInterest>
        {
            new() { Name = "Toegepaste Informatica", Faculty = "Technologie" },
            new() { Name = "Verpleegkunde", Faculty = "Gezondheidszorg" },
            new() { Name = "Bedrijfsmanagement", Faculty = "Business" },
            new() { Name = "Lager Onderwijs", Faculty = "Onderwijs" },
            new() { Name = "Sociaal Werk", Faculty = "Mens en Welzijn" }
        };
        var visitors = new List<Visitor>
        {
            new() { VisitorCode = "SID-1001", FirstName = "Emma", LastName = "Claes", BirthDate = new DateOnly(2007, 4, 12), CurrentSchool = "Sint-Jozefscollege", CurrentStudyArea = "Economie" },
            new() { VisitorCode = "SID-1002", FirstName = "Noah", LastName = "Peeters", BirthDate = new DateOnly(2006, 11, 3), CurrentSchool = "GO! Atheneum", CurrentStudyArea = "Wetenschappen" },
            new() { VisitorCode = "SID-1003", FirstName = "Lina", LastName = "Vermeulen", BirthDate = new DateOnly(2007, 8, 27), CurrentSchool = "VTI Brugge", CurrentStudyArea = "IT en Netwerken" }
        };
        var users = new List<AppUser>
        {
            new() { Email = "admin@sidin.local", DisplayName = "Admin SID-in", Role = Roles.Admin, PasswordHash = HashPassword("Admin123!"), IsActive = true },
            new() { Email = "expo@sidin.local", DisplayName = "Exposant Demo", Role = Roles.Exhibitor, PasswordHash = HashPassword("Expo123!"), IsActive = true },
            new() { Email = "marketing@sidin.local", DisplayName = "Marketing Demo", Role = Roles.Marketing, PasswordHash = HashPassword("Marketing123!"), IsActive = true },
            new() { Email = "teamlead@sidin.local", DisplayName = "Teamlead Demo", Role = Roles.TeamLead, PasswordHash = HashPassword("Lead123!"), IsActive = true }
        };
        return new AppDatabase
        {
            Fairs = fairs,
            Programs = programs,
            Visitors = visitors,
            Users = users,
            Registrations =
            [
                new() { VisitorId = visitors[0].Id, FairId = fairs[0].Id, ProgramIds = [programs[0].Id, programs[2].Id], Notes = "Wil info over campus Brugge." },
                new() { VisitorId = visitors[1].Id, FairId = fairs[1].Id, ProgramIds = [programs[0].Id], Notes = "Interesse in avondtraject." },
                new() { VisitorId = visitors[2].Id, FairId = fairs[2].Id, ProgramIds = [programs[1].Id, programs[4].Id], Notes = "Twijfelt nog tussen zorg en sociaal werk." }
            ]
        };
    }
}
