using ezZkvi.Controllers;
using ezZkvi.Data;
using ezZkvi.Models;
using ezZkvi.Tests.Infrastruktura;
using ezZkvi.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using static ezZkvi.Tests.Controllers.SadrzajTestSeed;

namespace ezZkvi.Tests.Controllers;

/// <summary>
/// Testovi upravljanja pitanjima (sa 4 ponuđena odgovora): izmjena, brisanje, dodavanje iz Content stranice
/// i provjera prava pristupa (moderator samo pitanja svojih predmeta).
/// Autor: Imran Čustović (B)
/// </summary>
[TestClass]
public sealed class PitanjeControllerTests
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

    private PitanjeController Kontroler(string korisnikId = AdminId, string uloga = "Admin")
    {
        return KontrolerHelper.SaKorisnikom(new PitanjeController(_kontekst), korisnikId, uloga);
    }

    private PitanjeController Moderator() => Kontroler(ModeratorId, "Moderator");

    private static PitanjeSaOdgovorimaViewModel NovoPitanje(
        string tekst = "Koliko je 2 + 2?",
        int predmetId = 1,
        int oblastId = 1,
        int id = 0,
        string odgovor4 = "8",
        int tacan = 2)
    {
        return new PitanjeSaOdgovorimaViewModel
        {
            Id = id,
            TekstPitanja = tekst,
            PredmetId = predmetId,
            OblastId = oblastId,
            Tezina = Tezina.SREDNJE,
            Odgovor1 = "3",
            Odgovor2 = "4",
            Odgovor3 = "5",
            Odgovor4 = odgovor4,
            TacanOdgovor = tacan
        };
    }

    private static Pitanje IzmjenaPitanja(int id = 1, string tekst = "Šta je inverzna matrica?", int predmetId = 1, int oblastId = 1)
    {
        return new Pitanje { Id = id, TekstPitanja = tekst, PredmetId = predmetId, OblastId = oblastId, Tezina = Tezina.TESKO };
    }

    // ---------- Index i zabranjene akcije ----------

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    public void Index_PreusmjeravaNaTabPitanja()
    {
        Assert.AreEqual("questions", Rezultat.Kao<RedirectToActionResult>(Kontroler().Index()).RouteValues!["tab"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public void Details_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Details(1), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public void CreateGet_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Create(), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public void CreatePost_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Create(NovoPitanje()), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public void DeleteGet_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Delete(1), typeof(ForbidResult));

    // ---------- Edit (GET) ----------

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Null verifier")]
    public async Task EditGet_BezId_VracaNotFound()
    {
        Assert.IsInstanceOfType(await Kontroler().Edit((int?)null), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public async Task EditGet_NepostojecePitanje_VracaNotFound()
    {
        Assert.IsInstanceOfType(await Kontroler().Edit((int?)99), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public async Task EditGet_ModeratorTudjegPitanja_VracaForbid()
    {
        Assert.IsInstanceOfType(await Moderator().Edit((int?)3), typeof(ForbidResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task EditGet_ModeratorSvogPitanja_NudiSamoSvojePredmeteIOblasti()
    {
        var kontroler = Moderator();

        var rezultat = await kontroler.Edit((int?)1);

        Assert.AreEqual(1, Rezultat.Kao<Pitanje>(Rezultat.Kao<ViewResult>(rezultat).Model).Id);
        Assert.AreEqual(1, Rezultat.Kao<SelectList>(kontroler.ViewData["PredmetId"]).Count());
        Assert.AreEqual(2, Rezultat.Kao<SelectList>(kontroler.ViewData["OblastId"]).Count());
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    public async Task EditGet_Admin_NudiSvePredmeteIOblastiSaPunimNazivom()
    {
        var kontroler = Kontroler();

        await kontroler.Edit((int?)1);

        var oblasti = Rezultat.Kao<SelectList>(kontroler.ViewData["OblastId"]).ToList();
        Assert.AreEqual(3, Rezultat.Kao<SelectList>(kontroler.ViewData["PredmetId"]).Count());
        Assert.AreEqual("Fizika / Mehanika", oblasti[0].Text);
        Assert.AreEqual("Matematika / Algebra", oblasti[1].Text);
    }

    // ---------- Edit (POST) ----------

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public async Task EditPost_RazlicitiId_VracaNotFound()
    {
        Assert.IsInstanceOfType(await Kontroler().Edit(2, IzmjenaPitanja(id: 1)), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public async Task EditPost_NepostojecePitanje_VracaNotFound()
    {
        Assert.IsInstanceOfType(await Kontroler().Edit(99, IzmjenaPitanja(id: 99)), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public async Task EditPost_ModeratorTudjegPitanja_VracaForbid()
    {
        Assert.IsInstanceOfType(await Moderator().Edit(3, IzmjenaPitanja(id: 3, predmetId: 2, oblastId: 3)), typeof(ForbidResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public async Task EditPost_ModeratorPrebacujeUTudjiPredmet_VracaForbid()
    {
        Assert.IsInstanceOfType(await Moderator().Edit(1, IzmjenaPitanja(predmetId: 2, oblastId: 3)), typeof(ForbidResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public async Task EditPost_OblastNePripadaPredmetu_VracaForbid()
    {
        Assert.IsInstanceOfType(await Kontroler().Edit(1, IzmjenaPitanja(predmetId: 1, oblastId: 3)), typeof(ForbidResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task EditPost_NevalidanModel_VracaFormuSaPodacima()
    {
        var kontroler = Kontroler();
        kontroler.ModelState.AddModelError("TekstPitanja", "Obavezno");

        var rezultat = await kontroler.Edit(1, IzmjenaPitanja());

        Assert.IsInstanceOfType(Rezultat.Kao<ViewResult>(rezultat).Model, typeof(Pitanje));
        Assert.IsNotNull(kontroler.ViewData["PredmetId"]);
        Assert.AreEqual("Šta je matrica?", (await _kontekst.Pitanje.FindAsync(1))!.TekstPitanja);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    [DataRow("  Šta?  ", "Tekst pitanja mora imati između 5 i 1000 karaktera.")]
    [DataRow("  ŠTA JE   SILA? ", "Pitanje sa istim tekstom već postoji.")]
    public async Task EditPost_NeispravanTekst_VracaFormuSaGreskom(string tekst, string ocekivanaGreska)
    {
        var kontroler = Kontroler();

        var rezultat = await kontroler.Edit(1, IzmjenaPitanja(tekst: tekst));

        Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
        Assert.AreEqual(ocekivanaGreska, kontroler.ModelState[nameof(Pitanje.TekstPitanja)]!.Errors[0].ErrorMessage);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task EditPost_ModeratorSvogPitanja_AzuriraPitanje()
    {
        var kontroler = Moderator();

        var rezultat = await kontroler.Edit(1, IzmjenaPitanja(tekst: "  Šta je   inverzna matrica? ", oblastId: 2));

        var pitanje = (await _kontekst.Pitanje.FindAsync(1))!;
        Assert.AreEqual("questions", Rezultat.Kao<RedirectToActionResult>(rezultat).RouteValues!["tab"]);
        Assert.AreEqual("Šta je inverzna matrica?", pitanje.TekstPitanja);
        Assert.AreEqual(Tezina.TESKO, pitanje.Tezina);
        Assert.AreEqual(2, pitanje.OblastId);
        Assert.AreEqual("Pitanje je ažurirano.", kontroler.TempData["Success"]);
    }

    // ---------- DeleteConfirmed ----------

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public async Task DeleteConfirmed_NepostojecePitanje_VracaNotFound()
    {
        Assert.IsInstanceOfType(await Kontroler().DeleteConfirmed(99), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public async Task DeleteConfirmed_ModeratorTudjegPitanja_VracaForbid()
    {
        Assert.IsInstanceOfType(await Moderator().DeleteConfirmed(3), typeof(ForbidResult));
        Assert.IsNotNull(await _kontekst.Pitanje.FindAsync(3));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task DeleteConfirmed_ModeratorSvogPitanja_BrisePitanjeIOdgovore()
    {
        var kontroler = Moderator();

        await kontroler.DeleteConfirmed(1);

        Assert.IsNull(await _kontekst.Pitanje.FindAsync(1));
        Assert.AreEqual(0, await _kontekst.Odgovor.CountAsync(o => o.PitanjeId == 1));
        StringAssert.StartsWith((string?)kontroler.TempData["Success"], "Pitanje je obrisano");
    }

    // ---------- EditFromContent ----------

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Boundary verifier")]
    public async Task EditFromContent_IdNula_VracaNotFound()
    {
        Assert.IsInstanceOfType(await Kontroler().EditFromContent(NovoPitanje(id: 0)), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public async Task EditFromContent_NepostojecePitanje_VracaNotFound()
    {
        Assert.IsInstanceOfType(await Kontroler().EditFromContent(NovoPitanje(id: 99)), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public async Task EditFromContent_ModeratorTudjegPitanja_VracaForbid()
    {
        Assert.IsInstanceOfType(await Moderator().EditFromContent(NovoPitanje(id: 3, predmetId: 2, oblastId: 3)), typeof(ForbidResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    public async Task EditFromContent_NevalidanModel_PreusmjeravaSaGreskom()
    {
        var kontroler = Kontroler();
        kontroler.ModelState.AddModelError("Odgovor1", "Obavezno");

        await kontroler.EditFromContent(NovoPitanje(id: 1));

        Assert.AreEqual("Pitanje nije ažurirano. Provjerite unesene podatke.", kontroler.TempData["Error"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public async Task EditFromContent_ModeratorPrebacujeUTudjiPredmet_VracaForbid()
    {
        Assert.IsInstanceOfType(await Moderator().EditFromContent(NovoPitanje(id: 1, predmetId: 2, oblastId: 3)), typeof(ForbidResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    [DataRow("Kako", "8", "Tekst pitanja mora imati između 5 i 1000 karaktera.")]
    [DataRow("šta je DERIVACIJA?", "8", "Pitanje sa istim tekstom već postoji.")]
    [DataRow("Koliko je 2 + 2?", "   ", "Svaki odgovor mora imati između 1 i 500 karaktera.")]
    public async Task EditFromContent_NeispravniPodaci_PreusmjeravaSaGreskom(string tekst, string odgovor4, string ocekivanaPoruka)
    {
        var kontroler = Kontroler();

        await kontroler.EditFromContent(NovoPitanje(tekst: tekst, id: 1, odgovor4: odgovor4));

        Assert.AreEqual(ocekivanaPoruka, kontroler.TempData["Error"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task EditFromContent_CetiriOdgovora_AzuriraTekstoveITacanOdgovor()
    {
        var kontroler = Moderator();

        await kontroler.EditFromContent(NovoPitanje(id: 1, tacan: 3));

        var odgovori = await _kontekst.Odgovor.Where(o => o.PitanjeId == 1).OrderBy(o => o.Id).ToListAsync();
        CollectionAssert.AreEqual(new[] { "3", "4", "5", "8" }, odgovori.Select(o => o.Tekst).ToArray());
        CollectionAssert.AreEqual(new[] { false, false, true, false }, odgovori.Select(o => o.IsTacan).ToArray());
        Assert.AreEqual("Koliko je 2 + 2?", (await _kontekst.Pitanje.FindAsync(1))!.TekstPitanja);
        Assert.AreEqual("Pitanje je ažurirano.", kontroler.TempData["Success"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task EditFromContent_PitanjeSaTriOdgovora_DodajeCetvrti()
    {
        DodajPitanje(_kontekst, 4, "Šta je integral?", 1, 2, brojOdgovora: 3);
        TestDbFactory.Snimi(_kontekst);

        await Kontroler().EditFromContent(NovoPitanje(tekst: "Šta je integral?", id: 4));

        Assert.AreEqual(4, await _kontekst.Odgovor.CountAsync(o => o.PitanjeId == 4));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task EditFromContent_PitanjeSaPetOdgovora_BriseVisak()
    {
        DodajPitanje(_kontekst, 4, "Šta je integral?", 1, 2, brojOdgovora: 5);
        TestDbFactory.Snimi(_kontekst);

        await Kontroler().EditFromContent(NovoPitanje(tekst: "Šta je integral?", id: 4));

        Assert.AreEqual(4, await _kontekst.Odgovor.CountAsync(o => o.PitanjeId == 4));
        Assert.IsNull(await _kontekst.Odgovor.FindAsync(45));
    }

    // ---------- CreateFromContent ----------

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    public async Task CreateFromContent_NevalidanModel_PreusmjeravaSaGreskom()
    {
        var kontroler = Kontroler();
        kontroler.ModelState.AddModelError("TekstPitanja", "Obavezno");

        await kontroler.CreateFromContent(NovoPitanje());

        Assert.AreEqual("Pitanje nije sačuvano. Provjerite unesene podatke.", kontroler.TempData["Error"]);
        Assert.AreEqual(3, await _kontekst.Pitanje.CountAsync());
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public async Task CreateFromContent_ModeratorUTudjiPredmet_VracaForbid()
    {
        Assert.IsInstanceOfType(await Moderator().CreateFromContent(NovoPitanje(predmetId: 2, oblastId: 3)), typeof(ForbidResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    [DataRow("Kako", "8", "Tekst pitanja mora imati između 5 i 1000 karaktera.")]
    [DataRow("  ŠTA JE MATRICA? ", "8", "Pitanje sa istim tekstom već postoji.")]
    [DataRow("Koliko je 2 + 2?", "", "Svaki odgovor mora imati između 1 i 500 karaktera.")]
    public async Task CreateFromContent_NeispravniPodaci_NeDodajePitanje(string tekst, string odgovor4, string ocekivanaPoruka)
    {
        var kontroler = Kontroler();

        await kontroler.CreateFromContent(NovoPitanje(tekst: tekst, odgovor4: odgovor4));

        Assert.AreEqual(ocekivanaPoruka, kontroler.TempData["Error"]);
        Assert.AreEqual(3, await _kontekst.Pitanje.CountAsync());
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [Priority(1)]
    [TestCategory("State verifier")]
    public async Task CreateFromContent_ModeratorSvogPredmeta_DodajePitanjeSaCetiriOdgovora()
    {
        var kontroler = Moderator();

        await kontroler.CreateFromContent(NovoPitanje(tekst: "  Koliko   je 2 + 2? ", tacan: 2));

        var pitanje = await _kontekst.Pitanje.SingleAsync(p => p.TekstPitanja == "Koliko je 2 + 2?");
        var odgovori = await _kontekst.Odgovor.Where(o => o.PitanjeId == pitanje.Id).OrderBy(o => o.Id).ToListAsync();
        Assert.AreEqual(Tezina.SREDNJE, pitanje.Tezina);
        CollectionAssert.AreEqual(new[] { "3", "4", "5", "8" }, odgovori.Select(o => o.Tekst).ToArray());
        Assert.AreEqual("4", odgovori.Single(o => o.IsTacan).Tekst);
        Assert.AreEqual("Pitanje je uspješno dodano.", kontroler.TempData["Success"]);
    }
}
