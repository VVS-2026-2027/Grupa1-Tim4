using ezZkvi.Data;
using ezZkvi.Models;
using ezZkvi.Services;
using ezZkvi.Tests.Infrastruktura;
using Microsoft.EntityFrameworkCore;

namespace ezZkvi.Tests.Services;

/// <summary>
/// Testovi brisanja sadržaja korisnika (kviz sesije sa stavkama, feedback, statistika) prije brisanja naloga.
/// Autor: Amir Džafić (C)
/// </summary>
[TestClass]
public sealed class UserDeletionServiceTests
{
    private ApplicationDbContext _kontekst = null!;

    [TestInitialize]
    public void Postavi()
    {
        _kontekst = TestDbFactory.Kreiraj();
        _kontekst.KvizSesije.AddRange(
            new KvizSesija { ID = 1, StudentId = "u1", TraziBrojPitanja = 1, VremenskoOgranicenje = 3 },
            new KvizSesija { ID = 2, StudentId = "u2", TraziBrojPitanja = 1, VremenskoOgranicenje = 3 });
        _kontekst.KvizSesijaPitanja.AddRange(
            new KvizSesijaPitanje { ID = 1, KvizSesijaId = 1, PitanjeId = 1, RedniBroj = 1 },
            new KvizSesijaPitanje { ID = 2, KvizSesijaId = 2, PitanjeId = 1, RedniBroj = 1 });
        _kontekst.Feedback.AddRange(
            new Feedback { ID = 1, KorisnikId = "u1", Sadrzaj = "Feedback korisnika u1" },
            new Feedback { ID = 2, KorisnikId = "u2", Sadrzaj = "Feedback korisnika u2" });
        _kontekst.StudentStatistike.AddRange(
            new StudentStatistika { Id = 1, KorisnikId = "u1", PredmetId = 1 },
            new StudentStatistika { Id = 2, KorisnikId = "u2", PredmetId = 1 });
        TestDbFactory.Snimi(_kontekst);
    }

    [TestCleanup]
    public void Ocisti()
    {
        _kontekst.Dispose();
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("State verifier")]
    public async Task ObrisiKorisnika_KorisnikSaSadrzajem_BriseSamoNjegovSadrzaj()
    {
        await UserDeletionService.ObrisiKorisnikaSaSadrzajemAsync(_kontekst, "u1");
        await _kontekst.SaveChangesAsync();

        CollectionAssert.AreEqual(new[] { 2 }, await _kontekst.KvizSesije.Select(s => s.ID).ToArrayAsync());
        CollectionAssert.AreEqual(new[] { 2 }, await _kontekst.KvizSesijaPitanja.Select(s => s.ID).ToArrayAsync());
        CollectionAssert.AreEqual(new[] { 2 }, await _kontekst.Feedback.Select(f => f.ID).ToArrayAsync());
        CollectionAssert.AreEqual(new[] { 2 }, await _kontekst.StudentStatistike.Select(s => s.Id).ToArrayAsync());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("State verifier")]
    public async Task ObrisiKorisnika_KorisnikBezSesija_BriseFeedbackIStatistiku()
    {
        _kontekst.Feedback.Add(new Feedback { ID = 3, KorisnikId = "u3", Sadrzaj = "Feedback korisnika u3" });
        _kontekst.StudentStatistike.Add(new StudentStatistika { Id = 3, KorisnikId = "u3", PredmetId = 1 });
        TestDbFactory.Snimi(_kontekst);

        await UserDeletionService.ObrisiKorisnikaSaSadrzajemAsync(_kontekst, "u3");
        await _kontekst.SaveChangesAsync();

        Assert.AreEqual(2, await _kontekst.KvizSesije.CountAsync());
        Assert.AreEqual(2, await _kontekst.Feedback.CountAsync());
        Assert.AreEqual(2, await _kontekst.StudentStatistike.CountAsync());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Boundary verifier")]
    [DataRow("")]
    [DataRow("   ")]
    public async Task ObrisiKorisnika_PrazanId_NistaNeBrise(string korisnikId)
    {
        await UserDeletionService.ObrisiKorisnikaSaSadrzajemAsync(_kontekst, korisnikId);
        await _kontekst.SaveChangesAsync();

        Assert.AreEqual(2, await _kontekst.KvizSesije.CountAsync());
        Assert.AreEqual(2, await _kontekst.Feedback.CountAsync());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Null verifier")]
    public async Task ObrisiKorisnika_NullId_NistaNeBrise()
    {
        await UserDeletionService.ObrisiKorisnikaSaSadrzajemAsync(_kontekst, null!);
        await _kontekst.SaveChangesAsync();

        Assert.AreEqual(2, await _kontekst.StudentStatistike.CountAsync());
    }
}
