using ezZkvi.Controllers;
using ezZkvi.Data;
using ezZkvi.Models;
using ezZkvi.Tests.Infrastruktura;
using ezZkvi.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ezZkvi.Tests.Controllers;

/// <summary>
/// Testovi moderatorskog dashboarda i preusmjeravanja na stranice sadržaja i feedbacka.
/// Autor: Amir Džafić (C)
/// </summary>
[TestClass]
public sealed class ModeratorControllerTests
{
    private const string ModeratorId = "mod-1";

    private ApplicationDbContext _kontekst = null!;

    [TestInitialize]
    public void Postavi()
    {
        _kontekst = TestDbFactory.Kreiraj();
        _kontekst.Predmet.AddRange(
            new Predmet { Id = 1, Naziv = "Matematika", KreatorId = ModeratorId },
            new Predmet { Id = 2, Naziv = "Fizika", KreatorId = "mod-2" },
            new Predmet { Id = 3, Naziv = "Statistika", KreatorId = ModeratorId });
        _kontekst.Pitanje.AddRange(
            new Pitanje { Id = 1, TekstPitanja = "Šta je matrica?", PredmetId = 1, OblastId = 1 },
            new Pitanje { Id = 2, TekstPitanja = "Šta je determinanta?", PredmetId = 1, OblastId = 1 },
            new Pitanje { Id = 3, TekstPitanja = "Šta je sila?", PredmetId = 2, OblastId = 2 });
        _kontekst.Feedback.AddRange(
            new Feedback { ID = 1, PredmetId = 1, Status = StatusFeedbacka.NA_CEKANJU, Sadrzaj = "Greška u matrici", DatumSlanja = DateTime.UtcNow.AddHours(-3) },
            new Feedback { ID = 2, PredmetId = 2, Status = StatusFeedbacka.NA_CEKANJU, Sadrzaj = "Greška u sili", DatumSlanja = DateTime.UtcNow.AddHours(-2) },
            new Feedback { ID = 3, PredmetId = null, Status = StatusFeedbacka.NA_CEKANJU, Sadrzaj = "Opći prijedlog", DatumSlanja = DateTime.UtcNow.AddHours(-1) },
            new Feedback { ID = 4, PredmetId = 1, Status = StatusFeedbacka.ODOBREN, Sadrzaj = "Prijedlog pitanja", DatumSlanja = DateTime.UtcNow });
        TestDbFactory.Snimi(_kontekst);
    }

    [TestCleanup]
    public void Ocisti()
    {
        _kontekst.Dispose();
    }

    private ModeratorController Kontroler(string korisnikId = ModeratorId, string uloga = "Moderator")
    {
        return KontrolerHelper.SaKorisnikom(new ModeratorController(_kontekst), korisnikId, uloga);
    }

    // ---------- Dashboard ----------

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("State verifier")]
    public async Task Dashboard_Moderator_BrojiSamoSadrzajSvojihPredmeta()
    {
        var kontroler = Kontroler();

        var rezultat = await kontroler.Dashboard();

        Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
        Assert.AreEqual(2, kontroler.ViewData["BrojPitanja"]);
        Assert.AreEqual(2, kontroler.ViewData["BrojPredmeta"]);
        Assert.AreEqual(2, kontroler.ViewData["NeobradeniFeedback"]);
        Assert.AreEqual(1, kontroler.ViewData["ObradeniFeedback"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task Dashboard_Moderator_PrikazujeNajnovijiNeobradjeniIObradjeniFeedback()
    {
        var kontroler = Kontroler();

        await kontroler.Dashboard();

        var neobradjeni = Rezultat.Kao<List<FeedbackItem>>(kontroler.ViewData["NajnovijiFeedback"]);
        var obradjeni = Rezultat.Kao<List<FeedbackItem>>(kontroler.ViewData["ObradjeniFeedback"]);
        CollectionAssert.AreEqual(new[] { "Opći prijedlog", "Greška u matrici" }, neobradjeni.Select(f => f.Sadrzaj).ToArray());
        CollectionAssert.AreEqual(new[] { "Prijedlog pitanja" }, obradjeni.Select(f => f.Sadrzaj).ToArray());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task Dashboard_Moderator_SortiraPredmetePoBrojuPitanja()
    {
        var kontroler = Kontroler();

        await kontroler.Dashboard();

        var predmeti = Rezultat.Kao<List<PredmetAktivnostItem>>(kontroler.ViewData["PitanjaPoPredmetu"]);
        CollectionAssert.AreEqual(new[] { "Matematika", "Statistika" }, predmeti.Select(p => p.Naziv).ToArray());
        CollectionAssert.AreEqual(new[] { 2, 0 }, predmeti.Select(p => p.BrojPitanja).ToArray());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("State verifier")]
    public async Task Dashboard_Admin_VidiSavSadrzaj()
    {
        var kontroler = Kontroler("admin-1", "Admin");

        await kontroler.Dashboard();

        Assert.AreEqual(3, kontroler.ViewData["BrojPitanja"]);
        Assert.AreEqual(3, kontroler.ViewData["BrojPredmeta"]);
        Assert.AreEqual(3, kontroler.ViewData["NeobradeniFeedback"]);
    }

    // ---------- Content / Feedback / Index ----------

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    [DataRow(null, null, null, null, "questions")]
    [DataRow(null, "pretraga", null, null, "subjects")]
    [DataRow("algebra", null, null, null, "areas")]
    [DataRow(null, null, 1, null, "areas")]
    [DataRow(null, "pretraga", null, "areas", "areas")]
    public void Content_BezEksplicitnogTaba_BiraTabPremaPretrazi(
        string? areaSearch, string? subjectSearch, int? areaPredmetId, string? tab, string ocekivaniTab)
    {
        var rezultat = Kontroler().Content(null, null, null, null, areaSearch, areaPredmetId, subjectSearch, tab);

        var preusmjerenje = Rezultat.Kao<RedirectToActionResult>(rezultat);
        Assert.AreEqual("Content", preusmjerenje.ControllerName);
        Assert.AreEqual("Index", preusmjerenje.ActionName);
        Assert.AreEqual(ocekivaniTab, preusmjerenje.RouteValues!["tab"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public void Content_ProsljedjujeParametrePretrage()
    {
        var rezultat = Kontroler().Content("matrica", 1, 2, Tezina.TESKO, null, null, null, "questions");

        var vrijednosti = Rezultat.Kao<RedirectToActionResult>(rezultat).RouteValues!;
        Assert.AreEqual("matrica", vrijednosti["search"]);
        Assert.AreEqual(1, vrijednosti["predmetId"]);
        Assert.AreEqual(Tezina.TESKO, vrijednosti["tezina"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public void Feedback_PreusmjeravaNaListuFeedbacka()
    {
        var preusmjerenje = Rezultat.Kao<RedirectToActionResult>(Kontroler().Feedback());

        Assert.AreEqual("Feedback", preusmjerenje.ControllerName);
        Assert.AreEqual("Index", preusmjerenje.ActionName);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public void Index_Admin_PreusmjeravaNaSadrzaj()
    {
        Assert.AreEqual("Content", Rezultat.Kao<RedirectToActionResult>(Kontroler("admin-1", "Admin").Index()).ControllerName);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public void Index_Moderator_PreusmjeravaNaDashboard()
    {
        Assert.AreEqual(nameof(ModeratorController.Dashboard), Rezultat.Kao<RedirectToActionResult>(Kontroler().Index()).ActionName);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public void Details_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Details("x"), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public void CreateGet_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Create(), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public void CreatePost_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Create(new Moderator()), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public void EditGet_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Edit("x"), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public void EditPost_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Edit("x", new Moderator()), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public void DeleteGet_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Delete("x"), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public void DeleteConfirmed_VracaForbid() => Assert.IsInstanceOfType(Kontroler().DeleteConfirmed(1), typeof(ForbidResult));
}
