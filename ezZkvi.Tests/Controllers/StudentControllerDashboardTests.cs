using System.Collections;
using ezZkvi.Controllers;
using ezZkvi.Data;
using ezZkvi.Models;
using ezZkvi.Tests.Infrastruktura;
using ezZkvi.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static ezZkvi.Tests.Controllers.StudentTestSeed;

namespace ezZkvi.Tests.Controllers;

/// <summary>
/// Testovi studentskog dashboarda, pripreme (vježbe) i akcija koje su zabranjene studentu.
/// Autor: Dino Tahirović (A)
/// </summary>
[TestClass]
public sealed class StudentControllerDashboardTests
{
    private ApplicationDbContext _kontekst = null!;

    [TestInitialize]
    public void Postavi()
    {
        _kontekst = KontekstSaPredmetima();
    }

    [TestCleanup]
    public void Ocisti()
    {
        _kontekst.Dispose();
    }

    private StudentController Kontroler()
    {
        return KontrolerHelper.SaKorisnikom(new StudentController(_kontekst), StudentId, "Student");
    }

    private async Task<DashboardViewModel> Dashboard()
    {
        var rezultat = await Kontroler().Dashboard();
        return Rezultat.Kao<DashboardViewModel>(Rezultat.Kao<ViewResult>(rezultat).Model);
    }

    /// <summary>Tri završena kviza prijavljenog studenta: 100 % i 80 % iz Matematike i 100 % bez predmeta.</summary>
    private void DodajTriZavrsenaKviza()
    {
        DodajSesiju(_kontekst, status: StatusSesije.ZAVRSEN, brojTacnih: 5, procenat: 100, minutaOdZavrsetka: 30);
        DodajSesiju(_kontekst, status: StatusSesije.ISTEKAO, brojTacnih: 4, procenat: 80, minutaOdZavrsetka: 20);
        DodajSesiju(_kontekst, status: StatusSesije.ZAVRSEN, brojTacnih: 5, procenat: 100, predmetId: null, oblastId: null, minutaOdZavrsetka: 10);
    }

    // ---------- Dashboard ----------

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("State verifier")]
    public async Task Dashboard_StudentBezKvizova_VracaPocetneVrijednosti()
    {
        var model = await Dashboard();

        Assert.AreEqual(0, model.KvizoviZavrseni);
        Assert.AreEqual(0, model.ProsjecanRezultat);
        Assert.AreEqual(0, model.XpUkupno);
        Assert.AreEqual(1, model.Nivo);
        Assert.AreEqual(100, model.XpZaNivo);
        Assert.AreEqual(1, model.Rang);
        Assert.AreEqual(0, model.UkupnoStudenata);
        Assert.IsFalse(model.ImaAktivnuSesiju);
        Assert.AreEqual(0, model.NedavneAktivnosti.Count);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("State verifier")]
    public async Task Dashboard_ZavrseniKvizovi_RacunaProsjekXpIRang()
    {
        DodajTriZavrsenaKviza();
        DodajSesiju(_kontekst, studentId: DrugiStudentId, status: StatusSesije.ZAVRSEN, brojTacnih: 20, procenat: 100);

        var model = await Dashboard();

        Assert.AreEqual(3, model.KvizoviZavrseni);
        Assert.AreEqual(93, model.ProsjecanRezultat);
        Assert.AreEqual(140, model.XpUkupno);
        Assert.AreEqual(2, model.Rang);
        Assert.AreEqual(2, model.UkupnoStudenata);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task Dashboard_ZavrseniKvizovi_PrikazujeNedavneAktivnostiOdNajnovije()
    {
        DodajTriZavrsenaKviza();

        var model = await Dashboard();

        Assert.AreEqual(3, model.NedavneAktivnosti.Count);
        Assert.AreEqual("Nepoznat predmet", model.NedavneAktivnosti[0].PredmetNaziv);
        Assert.AreEqual(80, model.NedavneAktivnosti[1].Procenat);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task Dashboard_ZavrseniKvizovi_PrikazujeNapredakPoPredmetu()
    {
        DodajPitanje(_kontekst, 1);
        DodajPitanje(_kontekst, 2);
        DodajTriZavrsenaKviza();

        var model = await Dashboard();

        Assert.AreEqual(1, model.MojiPredmeti.Count);
        Assert.AreEqual("Matematika", model.MojiPredmeti[0].Naziv);
        Assert.AreEqual(90, model.MojiPredmeti[0].Procenat);
        Assert.AreEqual(2, model.MojiPredmeti[0].BrojPitanja);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("State verifier")]
    public async Task Dashboard_AktivnaSesija_PrikazujeAktivnuSesiju()
    {
        var sesija = DodajSesiju(_kontekst, minutaOdPocetka: 1);

        var model = await Dashboard();

        Assert.AreEqual(sesija.ID, model.AktivnaSesijaId);
        Assert.AreEqual("Matematika", model.AktivnaSesijaPredmetNaziv);
        Assert.IsTrue(model.ImaAktivnuSesiju);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("State verifier")]
    public async Task Dashboard_IsteklaAktivnaSesija_ZatvaraSesijuIUracunavaJe()
    {
        DodajPitanje(_kontekst, 1);
        var sesija = DodajSesiju(_kontekst, minutaOdPocetka: 10);
        DodajStavke(_kontekst, sesija.ID, 1);

        var model = await Dashboard();

        Assert.IsNull(model.AktivnaSesijaId);
        Assert.AreEqual(1, model.KvizoviZavrseni);
        Assert.AreEqual(StatusSesije.ISTEKAO, (await _kontekst.KvizSesije.SingleAsync()).Status);
    }

    /// <summary>Data-driven (CSV): broj tačnih odgovora -> XP -> nivo (cijena nivoa raste 100, 200, 300...).</summary>
    public static IEnumerable<object[]> NivoiPoXp =>
        TestPodaci.Csv("nivo_xp.csv").Select(red => new object[]
        {
            TestPodaci.Int(red[0]), TestPodaci.Int(red[1]), TestPodaci.Int(red[2]), TestPodaci.Int(red[3])
        });

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Boundary verifier")]
    [DynamicData(nameof(NivoiPoXp), DynamicDataSourceType.Property)]
    public async Task Dashboard_BrojTacnihOdgovora_OdredjujeNivoIXp(int brojTacnih, int nivo, int xpUNivou, int xpZaNivo)
    {
        DodajSesiju(_kontekst, status: StatusSesije.ZAVRSEN, brojTacnih: brojTacnih, procenat: 50);

        var model = await Dashboard();

        Assert.AreEqual(brojTacnih * 10, model.XpUkupno);
        Assert.AreEqual(nivo, model.Nivo);
        Assert.AreEqual(xpUNivou, model.XpUNivou);
        Assert.AreEqual(xpZaNivo, model.XpZaNivo);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Exception verifier")]
    [ExpectedException(typeof(ObjectDisposedException))]
    public async Task Dashboard_BazaNijeDostupna_BacaObjectDisposedException()
    {
        _kontekst.Dispose();

        await Kontroler().Dashboard();
    }

    // ---------- Prepare / PrepareQuestions ----------

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("State verifier")]
    public async Task Prepare_VracaPredmeteIOblastiZaVjezbu()
    {
        var kontroler = Kontroler();

        var rezultat = await kontroler.Prepare();

        Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
        Assert.AreEqual(2, Rezultat.Kao<IList>(kontroler.ViewData["Predmeti"]).Count);
        Assert.AreEqual(2, Rezultat.Kao<IList>(kontroler.ViewData["Oblasti"]).Count);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Utility verifier")]
    public async Task PrepareQuestions_BezFiltera_VracaSamoPitanjaSaJednimTacnimIBarDvaOdgovora()
    {
        DodajPitanje(_kontekst, 1);
        DodajPitanje(_kontekst, 2, brojOdgovora: 2);
        DodajPitanje(_kontekst, 3, brojTacnih: 2);
        DodajPitanje(_kontekst, 4, brojOdgovora: 1);
        DodajPitanje(_kontekst, 5, brojOdgovora: 0);

        var rezultat = await Kontroler().PrepareQuestions(null, null);

        Assert.AreEqual(2, Rezultat.Kao<IList>(Rezultat.Kao<JsonResult>(rezultat).Value).Count);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Utility verifier")]
    public async Task PrepareQuestions_FilterPoPredmetuIOblasti_VracaSamoPitanjaTeOblasti()
    {
        DodajPitanje(_kontekst, 1);
        DodajPitanje(_kontekst, 8, oblastId: DrugaOblastId, predmetId: DrugiPredmetId);

        var rezultat = await Kontroler().PrepareQuestions(DrugiPredmetId, DrugaOblastId);

        var lista = Rezultat.Kao<IList>(Rezultat.Kao<JsonResult>(rezultat).Value);
        Assert.AreEqual(1, lista.Count);
        Assert.AreEqual("Pitanje broj 8", Rezultat.Svojstvo(lista[0]!, "tekst"));
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task PrepareQuestions_JednoPitanje_IndeksTacnogPokazujeNaTacanOdgovor()
    {
        DodajPitanje(_kontekst, 1);

        var rezultat = await Kontroler().PrepareQuestions(PredmetId, OblastId);

        var pitanje = Rezultat.Kao<IList>(Rezultat.Kao<JsonResult>(rezultat).Value)[0]!;
        var opcije = Rezultat.Kao<List<string>>(Rezultat.Svojstvo(pitanje, "opcije"));
        var tacan = Rezultat.Kao<int>(Rezultat.Svojstvo(pitanje, "tacan"));
        Assert.AreEqual(4, opcije.Count);
        Assert.AreEqual("Odgovor 1-1", opcije[tacan]);
    }

    // ---------- Index i zabranjene CRUD akcije ----------

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public void Index_PreusmjeravaNaDashboard()
    {
        var rezultat = Kontroler().Index();

        Assert.AreEqual(nameof(StudentController.Dashboard), Rezultat.Kao<RedirectToActionResult>(rezultat).ActionName);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void Details_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Details("x"), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void CreateGet_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Create(), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void CreatePost_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Create(new Student()), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void EditGet_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Edit("x"), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void EditPost_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Edit("x", new Student()), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void DeleteGet_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Delete("x"), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void DeleteConfirmed_VracaForbid() => Assert.IsInstanceOfType(Kontroler().DeleteConfirmed("x"), typeof(ForbidResult));
}
