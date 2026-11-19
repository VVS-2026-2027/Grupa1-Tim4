using System.Collections;
using ezZkvi.Controllers;
using ezZkvi.Data;
using ezZkvi.Models;
using ezZkvi.Tests.Infrastruktura;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static ezZkvi.Tests.Controllers.SadrzajTestSeed;

namespace ezZkvi.Tests.Controllers;

/// <summary>
/// Testovi upravljanja oblastima: dohvat po predmetu, dodavanje, izmjena (uklj. prebacivanje na drugi predmet)
/// i brisanje, sa provjerom prava (admin sve, moderator samo svoje predmete).
/// Autor: Imran Čustović (B)
/// </summary>
[TestClass]
public sealed class OblastControllerTests
{
    private ApplicationDbContext _kontekst = null!;

    [TestInitialize]
    public void Postavi()
    {
        _kontekst = Kontekst();
    }

    [TestCleanup]
    public void Ocisti()
    {
        _kontekst.Dispose();
    }

    private OblastController Kontroler(string korisnikId = AdminId, string uloga = "Admin")
    {
        return KontrolerHelper.SaKorisnikom(new OblastController(_kontekst), korisnikId, uloga);
    }

    private OblastController Moderator() => Kontroler(ModeratorId, "Moderator");

    private static void ProvjeriPreusmjerenjeNaOblasti(IActionResult rezultat)
    {
        var preusmjerenje = Rezultat.Kao<RedirectToActionResult>(rezultat);
        Assert.AreEqual("Content", preusmjerenje.ControllerName);
        Assert.AreEqual("areas", preusmjerenje.RouteValues!["tab"]);
    }

    // ---------- ByPredmet / Index ----------

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Empty tester")]
    public async Task ByPredmet_NepostojeciPredmet_VracaPraznuListu()
    {
        var rezultat = await Kontroler().ByPredmet(99);

        Assert.AreEqual(0, Rezultat.Kao<IList>(Rezultat.Kao<JsonResult>(rezultat).Value).Count);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Empty tester")]
    public async Task ByPredmet_ModeratorTudjegPredmeta_VracaPraznuListu()
    {
        var rezultat = await Moderator().ByPredmet(2);

        Assert.AreEqual(0, Rezultat.Kao<IList>(Rezultat.Kao<JsonResult>(rezultat).Value).Count);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    [DataRow(AdminId, "Admin")]
    [DataRow(ModeratorId, "Moderator")]
    [DataRow("student-1", "Student")]
    public async Task ByPredmet_DozvoljenPristup_VracaOblastiSortiranePoNazivu(string korisnikId, string uloga)
    {
        var rezultat = await Kontroler(korisnikId, uloga).ByPredmet(1);

        var lista = Rezultat.Kao<IList>(Rezultat.Kao<JsonResult>(rezultat).Value);
        Assert.AreEqual(2, lista.Count);
        Assert.AreEqual("Algebra", Rezultat.Svojstvo(lista[0]!, "Naziv"));
        Assert.AreEqual("Analiza", Rezultat.Svojstvo(lista[1]!, "Naziv"));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    public void Index_PreusmjeravaNaTabOblasti()
    {
        ProvjeriPreusmjerenjeNaOblasti(Kontroler().Index());
    }

    // ---------- CreateFromContent ----------

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    public async Task CreateFromContent_NepostojeciPredmet_PreusmjeravaSaGreskom()
    {
        var kontroler = Kontroler();

        var rezultat = await kontroler.CreateFromContent(99, "Geometrija");

        ProvjeriPreusmjerenjeNaOblasti(rezultat);
        Assert.AreEqual("Predmet za oblast nije pronađen.", kontroler.TempData["Error"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public async Task CreateFromContent_ModeratorTudjegPredmeta_VracaForbid()
    {
        Assert.IsInstanceOfType(await Moderator().CreateFromContent(2, "Optika"), typeof(ForbidResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task CreateFromContent_ModeratorSvogPredmeta_DodajeNormalizovanuOblast()
    {
        await Moderator().CreateFromContent(1, "  Linearna   algebra ");

        var oblast = await _kontekst.Oblast.SingleAsync(o => o.Naziv == "Linearna algebra");
        Assert.AreEqual(1, oblast.PredmetId);
    }

    /// <summary>Data-driven (CSV): naziv nove oblasti u predmetu "Matematika" i očekivana poruka.</summary>
    public static IEnumerable<object[]> NaziviOblasti =>
        TestPodaci.Csv("nazivi_oblasti.csv").Select(red => new object[] { red[0], red[1], red[2] });

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [Priority(1)]
    [TestCategory("Equality verifier")]
    [DynamicData(nameof(NaziviOblasti), DynamicDataSourceType.Property)]
    public async Task CreateFromContent_RazliciteVrijednostiNaziva_VracaOcekivanuPoruku(string naziv, string kljucTempData, string ocekivanaPoruka)
    {
        var kontroler = Kontroler();

        var rezultat = await kontroler.CreateFromContent(1, naziv);

        ProvjeriPreusmjerenjeNaOblasti(rezultat);
        Assert.AreEqual(ocekivanaPoruka, kontroler.TempData[kljucTempData]);
    }

    /// <summary>
    /// Oblast "Linearna  Algebra" (dva razmaka, npr. iz starijih podataka) i nova "linearna algebra"
    /// su isti naziv nakon normalizacije, pa nova oblast ne smije biti dodana kao duplikat.
    /// </summary>
    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    public async Task CreateFromContent_PostojeciNazivSaViseRazmaka_OdbijaDuplikat()
    {
        _kontekst.Oblast.Add(new Oblast { Id = 8, Naziv = "Linearna  Algebra", PredmetId = 1 });
        TestDbFactory.Snimi(_kontekst);
        var kontroler = Kontroler();

        await kontroler.CreateFromContent(1, "linearna algebra");

        Assert.AreEqual("Oblast sa istim nazivom već postoji u ovom predmetu.", kontroler.TempData["Error"]);
    }

    // ---------- EditFromContent ----------

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public async Task EditFromContent_NepostojecaOblast_VracaNotFound()
    {
        Assert.IsInstanceOfType(await Kontroler().EditFromContent(99, 1, "Nova"), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public async Task EditFromContent_ModeratorTudjeOblasti_VracaForbid()
    {
        Assert.IsInstanceOfType(await Moderator().EditFromContent(3, 2, "Kinematika"), typeof(ForbidResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public async Task EditFromContent_NepostojeciNoviPredmet_VracaForbid()
    {
        Assert.IsInstanceOfType(await Kontroler().EditFromContent(1, 99, "Algebra"), typeof(ForbidResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public async Task EditFromContent_ModeratorPrebacujeNaTudjiPredmet_VracaForbid()
    {
        Assert.IsInstanceOfType(await Moderator().EditFromContent(1, 2, "Algebra"), typeof(ForbidResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    [DataRow("A", "Naziv oblasti mora imati između 2 i 100 karaktera.")]
    [DataRow("Algebra #1", "Naziv oblasti smije sadržavati samo slova, brojeve i razmake.")]
    [DataRow("  ANALIZA ", "Oblast sa istim nazivom već postoji u ovom predmetu.")]
    public async Task EditFromContent_NeispravanNaziv_PreusmjeravaSaGreskom(string naziv, string ocekivanaPoruka)
    {
        var kontroler = Kontroler();

        await kontroler.EditFromContent(1, 1, naziv);

        Assert.AreEqual(ocekivanaPoruka, kontroler.TempData["Error"]);
        Assert.AreEqual("Algebra", (await _kontekst.Oblast.FindAsync(1))!.Naziv);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task EditFromContent_IstiNazivDrugaVelicinaSlova_DozvoljavaPreimenovanje()
    {
        var kontroler = Moderator();

        await kontroler.EditFromContent(1, 1, "ALGEBRA");

        Assert.AreEqual("Oblast je ažurirana.", kontroler.TempData["Success"]);
        Assert.AreEqual("ALGEBRA", (await _kontekst.Oblast.FindAsync(1))!.Naziv);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task EditFromContent_PrebacivanjeOblastiSaPitanjima_OdbijaPrebacivanje()
    {
        var kontroler = Kontroler();

        await kontroler.EditFromContent(1, 3, "Algebra");

        StringAssert.StartsWith((string?)kontroler.TempData["Error"], "Oblast se ne može prebaciti na drugi predmet");
        Assert.AreEqual(1, (await _kontekst.Oblast.FindAsync(1))!.PredmetId);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task EditFromContent_PrebacivanjeOblastiSaHistorijomKvizova_OdbijaPrebacivanje()
    {
        _kontekst.Oblast.Add(new Oblast { Id = 5, Naziv = "Geometrija", PredmetId = 1 });
        _kontekst.KvizSesije.Add(new KvizSesija { ID = 1, TraziBrojPitanja = 5, VremenskoOgranicenje = 3, OblastId = 5 });
        TestDbFactory.Snimi(_kontekst);
        var kontroler = Kontroler();

        await kontroler.EditFromContent(5, 3, "Geometrija");

        StringAssert.StartsWith((string?)kontroler.TempData["Error"], "Oblast se ne može prebaciti na drugi predmet");
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task EditFromContent_PrebacivanjePrazneOblasti_MijenjaPredmet()
    {
        _kontekst.Oblast.Add(new Oblast { Id = 4, Naziv = "Statistika", PredmetId = 1 });
        TestDbFactory.Snimi(_kontekst);
        var kontroler = Kontroler();

        await kontroler.EditFromContent(4, 3, "Statistika");

        Assert.AreEqual("Oblast je ažurirana.", kontroler.TempData["Success"]);
        Assert.AreEqual(3, (await _kontekst.Oblast.FindAsync(4))!.PredmetId);
    }

    // ---------- DeleteFromContent ----------

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public async Task DeleteFromContent_NepostojecaOblast_VracaNotFound()
    {
        Assert.IsInstanceOfType(await Kontroler().DeleteFromContent(99), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public async Task DeleteFromContent_ModeratorTudjeOblasti_VracaForbid()
    {
        Assert.IsInstanceOfType(await Moderator().DeleteFromContent(3), typeof(ForbidResult));
        Assert.AreEqual(3, await _kontekst.Oblast.CountAsync());
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task DeleteFromContent_ModeratorSvojeOblasti_BriseOblastIPitanja()
    {
        var kontroler = Moderator();

        var rezultat = await kontroler.DeleteFromContent(1);

        ProvjeriPreusmjerenjeNaOblasti(rezultat);
        Assert.IsNull(await _kontekst.Oblast.FindAsync(1));
        Assert.AreEqual(0, await _kontekst.Pitanje.CountAsync(p => p.OblastId == 1));
        StringAssert.StartsWith((string?)kontroler.TempData["Success"], "Oblast je obrisana");
    }
}
