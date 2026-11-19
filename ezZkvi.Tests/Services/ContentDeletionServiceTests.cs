using ezZkvi.Data;
using ezZkvi.Models;
using ezZkvi.Services;
using ezZkvi.Tests.Controllers;
using ezZkvi.Tests.Infrastruktura;
using Microsoft.EntityFrameworkCore;

namespace ezZkvi.Tests.Services;

/// <summary>
/// Testovi kaskadnog brisanja sadržaja (predmet -> oblasti -> pitanja -> odgovori, sesije, feedback, statistika).
/// Servis samo označava entitete za brisanje; SaveChanges poziva kontroler, pa ga i test poziva poslije servisa.
/// Autor: Imran Čustović (B)
/// </summary>
[TestClass]
public sealed class ContentDeletionServiceTests
{
    private ApplicationDbContext _kontekst = null!;

    // Sesije: 1 = predmet 1/oblast 1 (pitanje 1), 2 = bez predmeta, ali sadrži pitanje 2,
    //         3 = predmet 2/oblast 3 (pitanje 3), 4 = bez predmeta, oblast 2, bez stavki
    [TestInitialize]
    public void Postavi()
    {
        _kontekst = SadrzajTestSeed.Kontekst();
        _kontekst.KvizSesije.AddRange(
            new KvizSesija { ID = 1, TraziBrojPitanja = 1, VremenskoOgranicenje = 3, PredmetId = 1, OblastId = 1, StudentId = "s1" },
            new KvizSesija { ID = 2, TraziBrojPitanja = 1, VremenskoOgranicenje = 3, PredmetId = null, OblastId = null, StudentId = "s1" },
            new KvizSesija { ID = 3, TraziBrojPitanja = 1, VremenskoOgranicenje = 3, PredmetId = 2, OblastId = 3, StudentId = "s1" },
            new KvizSesija { ID = 4, TraziBrojPitanja = 1, VremenskoOgranicenje = 3, PredmetId = null, OblastId = 2, StudentId = "s1" });
        _kontekst.KvizSesijaPitanja.AddRange(
            new KvizSesijaPitanje { ID = 1, KvizSesijaId = 1, PitanjeId = 1, RedniBroj = 1 },
            new KvizSesijaPitanje { ID = 2, KvizSesijaId = 2, PitanjeId = 2, RedniBroj = 1 },
            new KvizSesijaPitanje { ID = 3, KvizSesijaId = 3, PitanjeId = 3, RedniBroj = 1 });
        _kontekst.Feedback.AddRange(
            new Feedback { ID = 1, Sadrzaj = "Greška u pitanju o matrici", PredmetId = 1 },
            new Feedback { ID = 2, Sadrzaj = "Greška u pitanju o sili", PredmetId = 2 });
        _kontekst.StudentStatistike.AddRange(
            new StudentStatistika { Id = 1, KorisnikId = "s1", PredmetId = 1, BrojKvizova = 1 },
            new StudentStatistika { Id = 2, KorisnikId = "s1", PredmetId = 2, BrojKvizova = 1 });
        TestDbFactory.Snimi(_kontekst);
    }

    [TestCleanup]
    public void Ocisti()
    {
        _kontekst.Dispose();
    }

    // ---------- ObrisiPredmetSaSadrzajemAsync ----------

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [Priority(1)]
    [TestCategory("State verifier")]
    public async Task ObrisiPredmet_PredmetSaSadrzajem_BriseOblastiPitanjaIOdgovore()
    {
        await ContentDeletionService.ObrisiPredmetSaSadrzajemAsync(_kontekst, 1);
        await _kontekst.SaveChangesAsync();

        CollectionAssert.AreEqual(new[] { 3 }, await _kontekst.Oblast.Select(o => o.Id).ToArrayAsync());
        CollectionAssert.AreEqual(new[] { 3 }, await _kontekst.Pitanje.Select(p => p.Id).ToArrayAsync());
        Assert.AreEqual(4, await _kontekst.Odgovor.CountAsync());
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task ObrisiPredmet_PredmetSaSadrzajem_BriseSesijeDirektnoKrozOblastIKrozPitanja()
    {
        await ContentDeletionService.ObrisiPredmetSaSadrzajemAsync(_kontekst, 1);
        await _kontekst.SaveChangesAsync();

        CollectionAssert.AreEqual(new[] { 3 }, await _kontekst.KvizSesije.Select(s => s.ID).ToArrayAsync());
        CollectionAssert.AreEqual(new[] { 3 }, await _kontekst.KvizSesijaPitanja.Select(s => s.ID).ToArrayAsync());
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task ObrisiPredmet_PredmetSaSadrzajem_BriseFeedbackIStatistikuSamoTogPredmeta()
    {
        await ContentDeletionService.ObrisiPredmetSaSadrzajemAsync(_kontekst, 1);
        await _kontekst.SaveChangesAsync();

        CollectionAssert.AreEqual(new[] { 2 }, await _kontekst.Feedback.Select(f => f.ID).ToArrayAsync());
        CollectionAssert.AreEqual(new[] { 2 }, await _kontekst.StudentStatistike.Select(s => s.Id).ToArrayAsync());
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task ObrisiPredmet_NeBriseSamPredmet()
    {
        await ContentDeletionService.ObrisiPredmetSaSadrzajemAsync(_kontekst, 1);
        await _kontekst.SaveChangesAsync();

        Assert.AreEqual(3, await _kontekst.Predmet.CountAsync());
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task ObrisiPredmet_PredmetBezSadrzaja_NistaDrugoNeBrise()
    {
        await ContentDeletionService.ObrisiPredmetSaSadrzajemAsync(_kontekst, 3);
        await _kontekst.SaveChangesAsync();

        Assert.AreEqual(3, await _kontekst.Oblast.CountAsync());
        Assert.AreEqual(3, await _kontekst.Pitanje.CountAsync());
        Assert.AreEqual(4, await _kontekst.KvizSesije.CountAsync());
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Exception verifier")]
    [ExpectedException(typeof(ObjectDisposedException))]
    public async Task ObrisiPredmet_BazaNijeDostupna_BacaObjectDisposedException()
    {
        _kontekst.Dispose();

        await ContentDeletionService.ObrisiPredmetSaSadrzajemAsync(_kontekst, 1);
    }

    // ---------- ObrisiOblastSaSadrzajemAsync ----------

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task ObrisiOblast_OblastSaPitanjem_BrisePitanjeOdgovoreISesijuOblasti()
    {
        await ContentDeletionService.ObrisiOblastSaSadrzajemAsync(_kontekst, 1);
        await _kontekst.SaveChangesAsync();

        CollectionAssert.AreEqual(new[] { 2, 3 }, await _kontekst.Pitanje.OrderBy(p => p.Id).Select(p => p.Id).ToArrayAsync());
        Assert.AreEqual(0, await _kontekst.Odgovor.CountAsync(o => o.PitanjeId == 1));
        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, await _kontekst.KvizSesije.OrderBy(s => s.ID).Select(s => s.ID).ToArrayAsync());
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task ObrisiOblast_SesijeVezaneKrozOblastIKrozPitanje_BriseObje()
    {
        await ContentDeletionService.ObrisiOblastSaSadrzajemAsync(_kontekst, 2);
        await _kontekst.SaveChangesAsync();

        CollectionAssert.AreEqual(new[] { 1, 3 }, await _kontekst.KvizSesije.OrderBy(s => s.ID).Select(s => s.ID).ToArrayAsync());
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task ObrisiOblast_OblastBezPitanjaISesija_NistaNeBrise()
    {
        _kontekst.Oblast.Add(new Oblast { Id = 9, Naziv = "Prazna", PredmetId = 1 });
        TestDbFactory.Snimi(_kontekst);

        await ContentDeletionService.ObrisiOblastSaSadrzajemAsync(_kontekst, 9);
        await _kontekst.SaveChangesAsync();

        Assert.AreEqual(3, await _kontekst.Pitanje.CountAsync());
        Assert.AreEqual(4, await _kontekst.KvizSesije.CountAsync());
        Assert.AreEqual(4, await _kontekst.Oblast.CountAsync());
    }

    // ---------- ObrisiPitanjeSaSadrzajemAsync ----------

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task ObrisiPitanje_PitanjeUSesiji_BriseStavkeIOdgovoreAliNeISesiju()
    {
        await ContentDeletionService.ObrisiPitanjeSaSadrzajemAsync(_kontekst, 2);
        await _kontekst.SaveChangesAsync();

        Assert.IsNull(await _kontekst.Pitanje.FindAsync(2));
        Assert.AreEqual(0, await _kontekst.Odgovor.CountAsync(o => o.PitanjeId == 2));
        Assert.AreEqual(0, await _kontekst.KvizSesijaPitanja.CountAsync(s => s.PitanjeId == 2));
        Assert.AreEqual(4, await _kontekst.KvizSesije.CountAsync());
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task ObrisiPitanje_NepostojecePitanje_NistaNeBrise()
    {
        await ContentDeletionService.ObrisiPitanjeSaSadrzajemAsync(_kontekst, 999);
        await _kontekst.SaveChangesAsync();

        Assert.AreEqual(3, await _kontekst.Pitanje.CountAsync());
        Assert.AreEqual(12, await _kontekst.Odgovor.CountAsync());
    }
}
