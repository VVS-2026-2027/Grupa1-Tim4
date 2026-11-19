using System.Collections;
using ezZkvi.Controllers;
using ezZkvi.Data;
using ezZkvi.Models;
using ezZkvi.Tests.Infrastruktura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ezZkvi.Tests.Controllers;

/// <summary>
/// Testovi feedbacka: student šalje prijavu greške/prijedlog, admin i moderator ga pregledaju i obrađuju.
/// Moderator vidi samo feedback za svoje predmete i feedback bez predmeta.
/// Autor: Amir Džafić (C)
/// </summary>
[TestClass]
public sealed class FeedbackControllerTests
{
    private const string AdminId = "admin-1";
    private const string ModeratorId = "mod-1";
    private const string StudentId = "s1";

    private ApplicationDbContext _kontekst = null!;

    // Feedback 1: predmet 1 (mod-1), na čekanju, prijava greške, autor s1
    // Feedback 2: predmet 2 (mod-2), odobren, prijedlog, autor s2 (nije u bazi korisnika)
    // Feedback 3: bez predmeta, na čekanju, prijedlog, bez autora
    [TestInitialize]
    public void Postavi()
    {
        _kontekst = TestDbFactory.Kreiraj();
        _kontekst.Users.Add(new Korisnik { Id = StudentId, UserName = "ana@etf.ba" });
        _kontekst.Predmet.AddRange(
            new Predmet { Id = 1, Naziv = "Matematika", KreatorId = ModeratorId },
            new Predmet { Id = 2, Naziv = "Fizika", KreatorId = "mod-2" });
        _kontekst.Feedback.AddRange(
            new Feedback { ID = 1, PredmetId = 1, Status = StatusFeedbacka.NA_CEKANJU, TipFeedbacka = TipFeedbacka.PRIJAVA_GRESKE,
                Sadrzaj = "Pitanje o matrici ima dva tačna odgovora.", KorisnikId = StudentId, DatumSlanja = DateTime.UtcNow.AddDays(-2) },
            new Feedback { ID = 2, PredmetId = 2, Status = StatusFeedbacka.ODOBREN, TipFeedbacka = TipFeedbacka.PRIJEDLOG_PITANJA,
                Sadrzaj = "Dodajte pitanje o Newtonovim zakonima.", KorisnikId = "s2", DatumSlanja = DateTime.UtcNow.AddDays(-1) },
            new Feedback { ID = 3, PredmetId = null, Status = StatusFeedbacka.NA_CEKANJU, TipFeedbacka = TipFeedbacka.PRIJEDLOG_PITANJA,
                Sadrzaj = "Dodajte tamni način prikaza.", KorisnikId = null, DatumSlanja = DateTime.UtcNow });
        TestDbFactory.Snimi(_kontekst);
    }

    [TestCleanup]
    public void Ocisti()
    {
        _kontekst.Dispose();
    }

    private FeedbackController Kontroler(string korisnikId = AdminId, string uloga = "Admin")
    {
        return KontrolerHelper.SaKorisnikom(new FeedbackController(_kontekst), korisnikId, uloga);
    }

    private FeedbackController Moderator() => Kontroler(ModeratorId, "Moderator");

    /// <summary>Feedback u kojem student pokušava sam postaviti status ODOBREN (kontroler to mora ignorisati).</summary>
    private static Feedback FeedbackKojiPokusavaPostavitiStatus() => new()
    {
        Sadrzaj = "Pitanje o derivaciji nema tačan odgovor.",
        PredmetId = 1,
        TipFeedbacka = TipFeedbacka.PRIJAVA_GRESKE,
        Status = StatusFeedbacka.ODOBREN
    };

    private static List<Feedback> ListaIz(IActionResult rezultat) =>
        Rezultat.Kao<List<Feedback>>(Rezultat.Kao<ViewResult>(rezultat).Model);

    // ---------- Index ----------

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("State verifier")]
    public async Task Index_Admin_VidiSavFeedbackOdNajnovijeg()
    {
        var kontroler = Kontroler();

        var lista = ListaIz(await kontroler.Index());

        CollectionAssert.AreEqual(new[] { 3, 2, 1 }, lista.Select(f => f.ID).ToArray());
        Assert.AreEqual(2, kontroler.ViewData["Neobradjenih"]);
        Assert.AreEqual(1, kontroler.ViewData["Obradjenih"]);
        Assert.AreEqual(2, kontroler.ViewData["Prijedloga"]);
        Assert.AreEqual(1, kontroler.ViewData["PrijavaGresaka"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task Index_Admin_PrikazujeAutoreKojiPostojeUBazi()
    {
        var kontroler = Kontroler();

        await kontroler.Index();

        var autori = Rezultat.Kao<Dictionary<string, string?>>(kontroler.ViewData["Autori"]);
        Assert.AreEqual(1, autori.Count);
        Assert.AreEqual("ana@etf.ba", autori[StudentId]);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Utility verifier")]
    public async Task Index_Moderator_VidiFeedbackSvojihPredmetaIBezPredmeta()
    {
        var lista = ListaIz(await Moderator().Index());

        CollectionAssert.AreEqual(new[] { 3, 1 }, lista.Select(f => f.ID).ToArray());
    }

    // ---------- Details / Edit (GET) / Delete (GET) ----------

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Null verifier")]
    public async Task Details_BezId_VracaNotFound()
    {
        Assert.IsInstanceOfType(await Kontroler().Details(null), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public async Task Details_NepostojeciFeedback_VracaNotFound()
    {
        Assert.IsInstanceOfType(await Kontroler().Details(99), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public async Task Details_ModeratorTudjegPredmeta_VracaForbid()
    {
        Assert.IsInstanceOfType(await Moderator().Details(2), typeof(ForbidResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public async Task Details_KorisnikBezUlogeAdminIliModerator_VracaForbid()
    {
        Assert.IsInstanceOfType(await Kontroler(StudentId, "Student").Details(1), typeof(ForbidResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    [DataRow(1)]
    [DataRow(3)]
    public async Task Details_ModeratorSvogIliOpstegFeedbacka_VracaFeedback(int id)
    {
        var rezultat = await Moderator().Details(id);

        Assert.AreEqual(id, Rezultat.Kao<Feedback>(Rezultat.Kao<ViewResult>(rezultat).Model).ID);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task Details_Admin_VracaFeedbackSaPredmetom()
    {
        var rezultat = await Kontroler().Details(2);

        Assert.AreEqual("Fizika", Rezultat.Kao<Feedback>(Rezultat.Kao<ViewResult>(rezultat).Model).Predmet!.Naziv);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Null verifier")]
    public async Task EditGet_BezId_VracaNotFound()
    {
        Assert.IsInstanceOfType(await Kontroler().Edit((int?)null), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public async Task EditGet_NepostojeciFeedback_VracaNotFound()
    {
        Assert.IsInstanceOfType(await Kontroler().Edit((int?)99), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public async Task EditGet_ModeratorTudjegPredmeta_VracaForbid()
    {
        Assert.IsInstanceOfType(await Moderator().Edit((int?)2), typeof(ForbidResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task EditGet_ModeratorSvogPredmeta_VracaFeedback()
    {
        var rezultat = await Moderator().Edit((int?)1);

        Assert.AreEqual(1, Rezultat.Kao<Feedback>(Rezultat.Kao<ViewResult>(rezultat).Model).ID);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Null verifier")]
    public async Task DeleteGet_BezId_VracaNotFound()
    {
        Assert.IsInstanceOfType(await Kontroler().Delete(null), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public async Task DeleteGet_NepostojeciFeedback_VracaNotFound()
    {
        Assert.IsInstanceOfType(await Kontroler().Delete(99), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public async Task DeleteGet_ModeratorTudjegPredmeta_VracaForbid()
    {
        Assert.IsInstanceOfType(await Moderator().Delete(2), typeof(ForbidResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task DeleteGet_Admin_VracaFeedbackZaPotvrdu()
    {
        var rezultat = await Kontroler().Delete(3);

        Assert.AreEqual(3, Rezultat.Kao<Feedback>(Rezultat.Kao<ViewResult>(rezultat).Model).ID);
    }

    // ---------- Create ----------

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("State verifier")]
    public async Task CreateGet_NudiSvePredmete()
    {
        var kontroler = Kontroler(StudentId, "Student");

        var rezultat = await kontroler.Create();

        Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
        Assert.AreEqual(2, Rezultat.Kao<IList>(kontroler.ViewData["Predmeti"]).Count);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task CreatePost_NevalidanModel_NeSpasavaFeedback()
    {
        var kontroler = Kontroler(StudentId, "Student");
        kontroler.ModelState.AddModelError("Sadrzaj", "Sadržaj feedbacka je obavezan.");

        var rezultat = await kontroler.Create(new Feedback { Sadrzaj = "" });

        Assert.AreEqual(nameof(FeedbackController.Create), Rezultat.Kao<RedirectToActionResult>(rezultat).ActionName);
        Assert.AreEqual("Feedback nije poslan. Provjeri da si unio/la tekst.", kontroler.TempData["Error"]);
        Assert.AreEqual(3, await _kontekst.Feedback.CountAsync());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task CreatePost_NepostojeciPredmet_NeSpasavaFeedback()
    {
        var kontroler = Kontroler(StudentId, "Student");

        await kontroler.Create(new Feedback { Sadrzaj = "Greška u pitanju broj 5.", PredmetId = 99 });

        Assert.AreEqual("Odabrani predmet nije validan.", kontroler.TempData["Error"]);
        Assert.AreEqual(3, await _kontekst.Feedback.CountAsync());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [Priority(1)]
    [TestCategory("State verifier")]
    public async Task CreatePost_IspravanFeedback_SpasavaSaStatusomNaCekanjuIAutorom()
    {
        var kontroler = Kontroler(StudentId, "Student");
        var prije = DateTime.UtcNow;

        await kontroler.Create(FeedbackKojiPokusavaPostavitiStatus());

        var spaseni = await _kontekst.Feedback.SingleAsync(f => f.Sadrzaj == "Pitanje o derivaciji nema tačan odgovor.");
        Assert.AreEqual(StatusFeedbacka.NA_CEKANJU, spaseni.Status);
        Assert.AreEqual(StudentId, spaseni.KorisnikId);
        Assert.IsTrue(spaseni.DatumSlanja >= prije);
        Assert.AreEqual("1", kontroler.TempData["FeedbackPoslan"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("State verifier")]
    public async Task CreatePost_FeedbackBezPredmeta_Spasava()
    {
        await Kontroler(StudentId, "Student").Create(new Feedback { Sadrzaj = "Općeniti prijedlog za aplikaciju." });

        Assert.AreEqual(4, await _kontekst.Feedback.CountAsync());
    }

    // ---------- Edit (POST) ----------

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public async Task EditPost_RazlicitiId_VracaNotFound()
    {
        Assert.IsInstanceOfType(await Kontroler().Edit(1, new Feedback { ID = 2 }), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public async Task EditPost_NepostojeciFeedback_VracaNotFound()
    {
        Assert.IsInstanceOfType(await Kontroler().Edit(99, new Feedback { ID = 99 }), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public async Task EditPost_ModeratorTudjegPredmeta_VracaForbid()
    {
        Assert.IsInstanceOfType(await Moderator().Edit(2, new Feedback { ID = 2, Status = StatusFeedbacka.ODBIJEN }), typeof(ForbidResult));
        Assert.AreEqual(StatusFeedbacka.ODOBREN, (await _kontekst.Feedback.FindAsync(2))!.Status);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("State verifier")]
    public async Task EditPost_ModeratorSvogPredmeta_MijenjaSamoStatus()
    {
        var rezultat = await Moderator().Edit(1, new Feedback { ID = 1, Status = StatusFeedbacka.ODBIJEN, Sadrzaj = "Pokušaj izmjene sadržaja" });

        var feedback = (await _kontekst.Feedback.FindAsync(1))!;
        Assert.AreEqual(nameof(FeedbackController.Index), Rezultat.Kao<RedirectToActionResult>(rezultat).ActionName);
        Assert.AreEqual(StatusFeedbacka.ODBIJEN, feedback.Status);
        Assert.AreEqual("Pitanje o matrici ima dva tačna odgovora.", feedback.Sadrzaj);
    }

    // ---------- DeleteConfirmed ----------

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public async Task DeleteConfirmed_NepostojeciFeedback_VracaNotFound()
    {
        Assert.IsInstanceOfType(await Kontroler().DeleteConfirmed(99), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public async Task DeleteConfirmed_ModeratorTudjegPredmeta_VracaForbid()
    {
        Assert.IsInstanceOfType(await Moderator().DeleteConfirmed(2), typeof(ForbidResult));
        Assert.AreEqual(3, await _kontekst.Feedback.CountAsync());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("State verifier")]
    public async Task DeleteConfirmed_Admin_BriseFeedback()
    {
        await Kontroler().DeleteConfirmed(2);

        Assert.IsNull(await _kontekst.Feedback.FindAsync(2));
        Assert.AreEqual(2, await _kontekst.Feedback.CountAsync());
    }

    // ---------- PrihvatiFeedback / OdbijFeedback ----------

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public async Task PrihvatiFeedback_NepostojeciFeedback_VracaNotFound()
    {
        Assert.IsInstanceOfType(await Kontroler().PrihvatiFeedback(99), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public async Task PrihvatiFeedback_ModeratorTudjegPredmeta_VracaForbid()
    {
        Assert.IsInstanceOfType(await Moderator().PrihvatiFeedback(2), typeof(ForbidResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("State verifier")]
    public async Task PrihvatiFeedback_ModeratorSvogPredmeta_PostavljaStatusOdobren()
    {
        var kontroler = Moderator();

        await kontroler.PrihvatiFeedback(1);

        Assert.AreEqual(StatusFeedbacka.ODOBREN, (await _kontekst.Feedback.FindAsync(1))!.Status);
        Assert.AreEqual("Feedback je prihvaćen.", kontroler.TempData["Success"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public async Task OdbijFeedback_NepostojeciFeedback_VracaNotFound()
    {
        Assert.IsInstanceOfType(await Kontroler().OdbijFeedback(99), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public async Task OdbijFeedback_ModeratorTudjegPredmeta_VracaForbid()
    {
        Assert.IsInstanceOfType(await Moderator().OdbijFeedback(2), typeof(ForbidResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("State verifier")]
    public async Task OdbijFeedback_Admin_PostavljaStatusOdbijen()
    {
        var kontroler = Kontroler();

        await kontroler.OdbijFeedback(3);

        Assert.AreEqual(StatusFeedbacka.ODBIJEN, (await _kontekst.Feedback.FindAsync(3))!.Status);
        Assert.AreEqual("Feedback je odbijen.", kontroler.TempData["Success"]);
    }
}
