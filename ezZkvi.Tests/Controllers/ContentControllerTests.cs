using ezZkvi.Controllers;
using ezZkvi.Data;
using ezZkvi.Models;
using ezZkvi.Tests.Infrastruktura;
using ezZkvi.ViewModels;
using Microsoft.AspNetCore.Mvc;
using static ezZkvi.Tests.Controllers.SadrzajTestSeed;

namespace ezZkvi.Tests.Controllers;

/// <summary>
/// Testovi stranice za upravljanje sadržajem (tabovi pitanja/oblasti/predmeti):
/// admin vidi sav sadržaj, moderator samo sadržaj predmeta koje je kreirao.
/// Stranica samo čita podatke, pa se baza pravi jednom za cijelu klasu ([ClassInitialize])
/// i zatvara nakon zadnjeg testa ([ClassCleanup]).
/// Autor: Imran Čustović (B)
/// </summary>
[TestClass]
public sealed class ContentControllerTests
{
    private static ApplicationDbContext _kontekst = null!;

    [ClassInitialize]
    public static void PripremiBazu(TestContext _)
    {
        _kontekst = Kontekst();
    }

    [ClassCleanup]
    public static void ZatvoriBazu()
    {
        _kontekst.Dispose();
    }

    private ContentController Kontroler(string korisnikId = AdminId, string uloga = "Admin")
    {
        return KontrolerHelper.SaKorisnikom(new ContentController(_kontekst), korisnikId, uloga);
    }

    private static Task<IActionResult> Index(ContentController kontroler, string? tab = null)
    {
        return kontroler.Index(null, null, null, null, null, null, null, tab);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    public void Root_PreusmjeravaNaIndex()
    {
        var rezultat = Kontroler().Root();

        Assert.AreEqual(nameof(ContentController.Index), Rezultat.Kao<RedirectToActionResult>(rezultat).ActionName);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    [DataRow(null, "questions")]
    [DataRow("areas", "areas")]
    [DataRow("  SUBJECTS ", "subjects")]
    [DataRow("nepoznato", "questions")]
    public async Task Index_RazliciteVrijednostiTaba_NormalizujeAktivniTab(string? tab, string ocekivaniTab)
    {
        var kontroler = Kontroler();

        await Index(kontroler, tab);

        Assert.AreEqual(ocekivaniTab, kontroler.ViewData["ActiveTab"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task Index_Admin_VidiSvaPitanjaOdNajnovijeg()
    {
        var rezultat = await Index(Kontroler());

        var pitanja = Rezultat.Kao<List<Pitanje>>(Rezultat.Kao<ViewResult>(rezultat).Model);
        CollectionAssert.AreEqual(new[] { 3, 2, 1 }, pitanja.Select(p => p.Id).ToArray());
        Assert.AreEqual("Matematika", pitanja[2].Predmet!.Naziv);
        Assert.AreEqual("Algebra", pitanja[2].Oblast!.Naziv);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task Index_Moderator_VidiSamoPitanjaSvogPredmeta()
    {
        var rezultat = await Index(Kontroler(ModeratorId, "Moderator"));

        var pitanja = Rezultat.Kao<List<Pitanje>>(Rezultat.Kao<ViewResult>(rezultat).Model);
        CollectionAssert.AreEqual(new[] { 2, 1 }, pitanja.Select(p => p.Id).ToArray());
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    public async Task Index_Admin_PrikazujeBrojPitanjaPoPredmetu()
    {
        var kontroler = Kontroler();

        await Index(kontroler);

        var predmeti = Rezultat.Kao<List<PredmetAktivnostItem>>(kontroler.ViewData["PredmetiLista"]);
        CollectionAssert.AreEqual(new[] { "Fizika", "Hemija", "Matematika" }, predmeti.Select(p => p.Naziv).ToArray());
        CollectionAssert.AreEqual(new[] { 1, 0, 2 }, predmeti.Select(p => p.BrojPitanja).ToArray());
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    public async Task Index_Moderator_PrikazujeSamoSvojeOblastiSaBrojemPitanja()
    {
        var kontroler = Kontroler(ModeratorId, "Moderator");

        await Index(kontroler);

        var oblasti = Rezultat.Kao<List<OblastAktivnostItem>>(kontroler.ViewData["OblastiLista"]);
        CollectionAssert.AreEqual(new[] { "Algebra", "Analiza" }, oblasti.Select(o => o.Naziv).ToArray());
        CollectionAssert.AreEqual(new[] { 1, 1 }, oblasti.Select(o => o.BrojPitanja).ToArray());
        Assert.AreEqual("Matematika", oblasti[0].PredmetNaziv);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    public async Task Index_Admin_GrupiseOdgovorePoPitanju()
    {
        var kontroler = Kontroler();

        await Index(kontroler);

        var odgovori = Rezultat.Kao<Dictionary<int, List<Odgovor>>>(kontroler.ViewData["OdgovoriPoPitanju"]);
        Assert.AreEqual(3, odgovori.Count);
        Assert.AreEqual(4, odgovori[1].Count);
        Assert.AreEqual(11, odgovori[1][0].Id);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Empty tester")]
    public async Task Index_ModeratorBezPredmeta_NeVidiNikakavSadrzaj()
    {
        var rezultat = await Index(Kontroler("mod-bez-predmeta", "Moderator"));

        Assert.AreEqual(0, Rezultat.Kao<List<Pitanje>>(Rezultat.Kao<ViewResult>(rezultat).Model).Count);
    }
}
