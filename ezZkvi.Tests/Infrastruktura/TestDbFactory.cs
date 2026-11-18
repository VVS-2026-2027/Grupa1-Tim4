using ezZkvi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ezZkvi.Tests.Infrastruktura;

/// <summary>
/// Zamjenski objekat (Fake) za bazu podataka: svaki test dobija svoju, praznu
/// EF Core InMemory bazu, pa testovi ne zavise jedan od drugog niti od PostgreSQL-a.
/// </summary>
public static class TestDbFactory
{
    public static ApplicationDbContext Kreiraj()
    {
        var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"ezZkvi-test-{Guid.NewGuid()}")
            // InMemory provider ne podržava transakcije; AdministratorController.RejectUser ih koristi.
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(opcije);
    }

    /// <summary>Snimi seed podatke i očisti change tracker, da kontroler čita "svježe" podatke.</summary>
    public static void Snimi(ApplicationDbContext kontekst)
    {
        kontekst.SaveChanges();
        kontekst.ChangeTracker.Clear();
    }
}
