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
/// Testovi pokretanja i nastavka simulacije kviza (Simulate, StartSimulation, ContinueSimulation).
/// Autor: Dino Tahirović (A)
/// </summary>
[TestClass]
public sealed class StudentControllerSimulacijaTests
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

    private StudentController Kontroler(string korisnikId = StudentId)
    {
        return KontrolerHelper.SaKorisnikom(new StudentController(_kontekst), korisnikId, "Student");
    }

    private static SimulacijaKvizaViewModel ModelIz(IActionResult rezultat)
    {
        return Rezultat.Kao<SimulacijaKvizaViewModel>(Rezultat.Kao<ViewResult>(rezultat).Model);
    }

    /// <summary>Sesija starija od 10 minuta (istekla) sa 2 pitanja: prvo tačno odgovoreno, drugo otvoreno.</summary>
    private void DodajIstekluSesijuSaDvaPitanja()
    {
        DodajPitanje(_kontekst, 1);
        DodajPitanje(_kontekst, 2);
        var sesija = DodajSesiju(_kontekst, minutaOdPocetka: 10);
        DodajStavku(_kontekst, sesija.ID, 1, 1, TacanOdgovor(1), 1, 0.1);
        DodajStavku(_kontekst, sesija.ID, 2, 2);
    }

    /// <summary>Aktivna sesija sa 3 pitanja: odgovoreno, preskočeno i otvoreno. Vraća Id sesije.</summary>
    private int AktivnaSesijaSaOdgovorenimPreskocenimIOtvorenimPitanjem()
    {
        DodajPitanje(_kontekst, 1);
        DodajPitanje(_kontekst, 2);
        DodajPitanje(_kontekst, 3);
        var sesija = DodajSesiju(_kontekst, minutaOdPocetka: 1);
        DodajStavku(_kontekst, sesija.ID, 1, 1, TacanOdgovor(1), 1, 0.1);
        DodajStavku(_kontekst, sesija.ID, 2, 2, null, -1, 0);
        DodajStavku(_kontekst, sesija.ID, 3, 3);
        return sesija.ID;
    }

    private Task<IActionResult> PokreniSimulaciju() =>
        Kontroler().StartSimulation(new SimulacijaKvizaViewModel { PredmetId = PredmetId, OblastId = OblastId });

    // ---------- Simulate ----------

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("State verifier")]
    public async Task Simulate_BezAktivneSesije_VracaPredmeteIOblastiSortiranePoNazivu()
    {
        var model = ModelIz(await Kontroler().Simulate());

        Assert.AreEqual(2, model.Predmeti.Count);
        Assert.AreEqual("Fizika", model.Predmeti[0].Text);
        Assert.AreEqual(2, model.Oblasti.Count);
        Assert.IsNull(model.AktivnaSesijaId);
        Assert.IsNull(model.Result);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task Simulate_GreskaUTempData_PostavljaPorukuGreske()
    {
        var kontroler = Kontroler();
        kontroler.TempData["SimulationError"] = "Testna greška";

        var model = ModelIz(await kontroler.Simulate());

        Assert.AreEqual("Testna greška", model.ErrorMessage);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("State verifier")]
    public async Task Simulate_AktivnaNeisteklaSesija_PrikazujeAktivnuSesiju()
    {
        var sesija = DodajSesiju(_kontekst, minutaOdPocetka: 1);

        var model = ModelIz(await Kontroler().Simulate());

        Assert.AreEqual(sesija.ID, model.AktivnaSesijaId);
        Assert.AreEqual("Matematika", model.AktivnaSesijaPredmetNaziv);
        Assert.AreEqual("Algebra", model.AktivnaSesijaOblastNaziv);
        Assert.IsTrue(model.PreostaloSekundi is > 0 and <= 120);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("State verifier")]
    public async Task Simulate_IsteklaSesija_ZatvaraSesijuIVracaRezultat()
    {
        DodajIstekluSesijuSaDvaPitanja();

        var model = ModelIz(await Kontroler().Simulate());

        Assert.IsNotNull(model.Result);
        Assert.AreEqual(2, model.Result.UkupnoPitanja);
        Assert.AreEqual(1, model.Result.TacnihOdgovora);
        Assert.AreEqual(2, model.BrojPitanja);
        Assert.AreEqual(StatusSesije.ISTEKAO, (await _kontekst.KvizSesije.SingleAsync()).Status);
        Assert.AreEqual(0, await _kontekst.KvizSesijaPitanja.CountAsync());
    }

    // ---------- StartSimulation: validacija ulaza ----------

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task StartSimulation_BezPredmeta_PreusmjeravaSaGreskom()
    {
        var kontroler = Kontroler();

        var rezultat = await kontroler.StartSimulation(new SimulacijaKvizaViewModel { PredmetId = null, OblastId = OblastId });

        Assert.AreEqual(nameof(StudentController.Simulate), Rezultat.Kao<RedirectToActionResult>(rezultat).ActionName);
        Assert.AreEqual("Moraš odabrati predmet prije pokretanja simulacije.", kontroler.TempData["SimulationError"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task StartSimulation_BezOblasti_PreusmjeravaSaGreskom()
    {
        var kontroler = Kontroler();

        var rezultat = await kontroler.StartSimulation(new SimulacijaKvizaViewModel { PredmetId = PredmetId, OblastId = null });

        Assert.AreEqual(nameof(StudentController.Simulate), Rezultat.Kao<RedirectToActionResult>(rezultat).ActionName);
        Assert.AreEqual("Moraš odabrati oblast prije pokretanja simulacije.", kontroler.TempData["SimulationError"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task StartSimulation_OblastIzDrugogPredmeta_PreusmjeravaSaGreskom()
    {
        var kontroler = Kontroler();

        await kontroler.StartSimulation(new SimulacijaKvizaViewModel { PredmetId = PredmetId, OblastId = DrugaOblastId });

        Assert.AreEqual("Odabrana oblast ne pripada odabranom predmetu.", kontroler.TempData["SimulationError"]);
        Assert.AreEqual(0, await _kontekst.KvizSesije.CountAsync());
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("State verifier")]
    public async Task StartSimulation_PostojiAktivnaSesija_OdbijaNovuSimulaciju()
    {
        DodajPetValidnihPitanja(_kontekst);
        DodajSesiju(_kontekst, minutaOdPocetka: 1);
        var kontroler = Kontroler();

        await kontroler.StartSimulation(new SimulacijaKvizaViewModel { PredmetId = PredmetId, OblastId = OblastId });

        StringAssert.StartsWith((string?)kontroler.TempData["SimulationError"], "Već imaš aktivnu simulaciju kviza.");
        Assert.AreEqual(1, await _kontekst.KvizSesije.CountAsync());
    }

    // ---------- StartSimulation: kreiranje sesije ----------

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [Priority(1)]
    [TestCategory("Equality verifier")]
    public async Task StartSimulation_ValidniPodaci_VracaSimulacijuSaPetPitanjaITriMinute()
    {
        DodajPetValidnihPitanja(_kontekst);

        var view = Rezultat.Kao<ViewResult>(await PokreniSimulaciju());

        var model = Rezultat.Kao<SimulacijaKvizaViewModel>(view.Model);
        Assert.AreEqual("Simulate", view.ViewName);
        Assert.AreEqual(5, model.Questions.Count);
        Assert.AreEqual(180, model.PreostaloSekundi);
        Assert.IsNotNull(model.KvizSesijaId);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("State verifier")]
    public async Task StartSimulation_ValidniPodaci_SpasavaSesijuUTokuSaPetStavki()
    {
        DodajPetValidnihPitanja(_kontekst);

        await PokreniSimulaciju();

        var sesija = await _kontekst.KvizSesije.SingleAsync();
        Assert.AreEqual(StatusSesije.U_TOKU, sesija.Status);
        Assert.AreEqual(StudentId, sesija.StudentId);
        Assert.AreEqual(5, await _kontekst.KvizSesijaPitanja.CountAsync(s => s.KvizSesijaId == sesija.ID));
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task StartSimulation_ValidniPodaci_BiraPitanjaPoTezini1Tesko2Srednja2Laka()
    {
        DodajPetValidnihPitanja(_kontekst);
        DodajPitanje(_kontekst, 6, Tezina.LAKO);
        DodajPitanje(_kontekst, 7, Tezina.TESKO);

        var model = ModelIz(await Kontroler().StartSimulation(new SimulacijaKvizaViewModel { PredmetId = PredmetId, OblastId = OblastId }));

        Assert.AreEqual(1, model.Questions.Count(q => q.Tezina == Tezina.TESKO));
        Assert.AreEqual(2, model.Questions.Count(q => q.Tezina == Tezina.SREDNJE));
        Assert.AreEqual(2, model.Questions.Count(q => q.Tezina == Tezina.LAKO));
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Utility verifier")]
    public async Task StartSimulation_NevalidnaPitanja_IzostavljaIhIzSimulacije()
    {
        DodajPetValidnihPitanja(_kontekst);
        DodajPitanje(_kontekst, 6, brojOdgovora: 3);
        DodajPitanje(_kontekst, 7, brojTacnih: 2);

        var model = ModelIz(await Kontroler().StartSimulation(new SimulacijaKvizaViewModel { PredmetId = PredmetId, OblastId = OblastId }));

        var ids = model.Questions.Select(q => q.Id).ToList();
        CollectionAssert.DoesNotContain(ids, 6);
        CollectionAssert.DoesNotContain(ids, 7);
        CollectionAssert.AreEqual(new[] { 4, 4, 4, 4, 4 }, model.Questions.Select(q => q.Odgovori.Count).ToArray());
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Utility verifier")]
    public async Task StartSimulation_SamoLakaPitanja_DopunjavaDoPetPitanja()
    {
        DodajPitanje(_kontekst, 1);
        DodajPitanje(_kontekst, 2);
        DodajPitanje(_kontekst, 3);
        DodajPitanje(_kontekst, 4);
        DodajPitanje(_kontekst, 5);
        DodajPitanje(_kontekst, 6);

        var model = ModelIz(await Kontroler().StartSimulation(new SimulacijaKvizaViewModel { PredmetId = PredmetId, OblastId = OblastId }));

        Assert.AreEqual(5, model.Questions.Count);
        Assert.AreEqual(5, model.Questions.Select(q => q.Id).Distinct().Count());
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Boundary verifier")]
    public async Task StartSimulation_ManjeOdPetValidnihPitanja_KoristiSvaDostupna()
    {
        DodajPitanje(_kontekst, 1, Tezina.TESKO);
        DodajPitanje(_kontekst, 2, Tezina.SREDNJE);
        DodajPitanje(_kontekst, 3, Tezina.LAKO);

        var model = ModelIz(await Kontroler().StartSimulation(new SimulacijaKvizaViewModel { PredmetId = PredmetId, OblastId = OblastId }));

        Assert.AreEqual(3, model.BrojPitanja);
        Assert.AreEqual(3, (await _kontekst.KvizSesije.SingleAsync()).TraziBrojPitanja);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task StartSimulation_OblastBezValidnihPitanja_PreusmjeravaSaGreskom()
    {
        DodajPitanje(_kontekst, 1, brojOdgovora: 2);
        var kontroler = Kontroler();

        var rezultat = await kontroler.StartSimulation(new SimulacijaKvizaViewModel { PredmetId = PredmetId, OblastId = OblastId });

        Assert.IsInstanceOfType(rezultat, typeof(RedirectToActionResult));
        StringAssert.StartsWith((string?)kontroler.TempData["SimulationError"], "Za odabranu oblast ne postoji nijedno validno ABCD pitanje.");
        Assert.AreEqual(0, await _kontekst.KvizSesije.CountAsync());
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task StartSimulation_PredmetOblastiNePostoji_PreusmjeravaSaGreskom()
    {
        _kontekst.Oblast.Add(new Oblast { Id = 3, Naziv = "Bez predmeta", PredmetId = 77 });
        TestDbFactory.Snimi(_kontekst);
        var kontroler = Kontroler();

        await kontroler.StartSimulation(new SimulacijaKvizaViewModel { PredmetId = 77, OblastId = 3 });

        Assert.AreEqual("Odabrani predmet ne postoji.", kontroler.TempData["SimulationError"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("State verifier")]
    public async Task StartSimulation_IsteklaPrethodnaSesija_ZatvaraJuIPokreceNovu()
    {
        DodajPetValidnihPitanja(_kontekst);
        var stara = DodajSesiju(_kontekst, minutaOdPocetka: 10);
        DodajStavke(_kontekst, stara.ID, 1);

        var rezultat = await Kontroler().StartSimulation(new SimulacijaKvizaViewModel { PredmetId = PredmetId, OblastId = OblastId });

        Assert.AreEqual("Simulate", Rezultat.Kao<ViewResult>(rezultat).ViewName);
        Assert.AreEqual(StatusSesije.ISTEKAO, (await _kontekst.KvizSesije.SingleAsync(s => s.ID == stara.ID)).Status);
        Assert.AreEqual(2, await _kontekst.KvizSesije.CountAsync());
    }

    // ---------- ContinueSimulation ----------

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task ContinueSimulation_NepostojecaSesija_PreusmjeravaSaGreskom()
    {
        var kontroler = Kontroler();

        var rezultat = await kontroler.ContinueSimulation(12345);

        Assert.AreEqual(nameof(StudentController.Simulate), Rezultat.Kao<RedirectToActionResult>(rezultat).ActionName);
        Assert.AreEqual("Aktivna simulacija nije pronađena.", kontroler.TempData["SimulationError"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task ContinueSimulation_SesijaDrugogStudenta_NijeDostupna()
    {
        var tudja = DodajSesiju(_kontekst, studentId: DrugiStudentId);
        var kontroler = Kontroler();

        await kontroler.ContinueSimulation(tudja.ID);

        Assert.AreEqual("Aktivna simulacija nije pronađena.", kontroler.TempData["SimulationError"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task ContinueSimulation_AktivnaSesija_NastavljaOdPrvogOtvorenogPitanja()
    {
        var sesijaId = AktivnaSesijaSaOdgovorenimPreskocenimIOtvorenimPitanjem();

        var model = ModelIz(await Kontroler().ContinueSimulation(sesijaId));

        Assert.AreEqual(2, model.PocetniIndex);
        Assert.AreEqual(3, model.BrojPitanja);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("State verifier")]
    public async Task ContinueSimulation_AktivnaSesija_VracaStanjeSvakogPitanja()
    {
        var sesijaId = AktivnaSesijaSaOdgovorenimPreskocenimIOtvorenimPitanjem();

        var pitanja = ModelIz(await Kontroler().ContinueSimulation(sesijaId)).Questions;

        Assert.IsTrue(pitanja[0].JeOdgovoreno);
        Assert.AreEqual(TacanOdgovor(1), pitanja[0].OdabraniOdgovorId);
        Assert.IsTrue(pitanja[1].JePreskoceno);
        Assert.IsTrue(pitanja[1].JeZakljucano);
        Assert.IsFalse(pitanja[2].JeZakljucano);
        Assert.AreEqual(4, pitanja[2].Odgovori.Count);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Boundary verifier")]
    public async Task ContinueSimulation_SvaPitanjaZakljucana_PocetniIndexJeZadnjePitanje()
    {
        DodajPitanje(_kontekst, 1);
        DodajPitanje(_kontekst, 2);
        var sesija = DodajSesiju(_kontekst, minutaOdPocetka: 1);
        DodajStavku(_kontekst, sesija.ID, 1, 1, TacanOdgovor(1), 1, 0.1);
        DodajStavku(_kontekst, sesija.ID, 2, 2, NetacanOdgovor(2), 0, -0.03);

        var model = ModelIz(await Kontroler().ContinueSimulation(sesija.ID));

        Assert.AreEqual(1, model.PocetniIndex);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Null verifier")]
    public async Task ContinueSimulation_PitanjeBezOdgovoraUBazi_VracaPraznuListuOdgovora()
    {
        DodajPitanje(_kontekst, 1, brojOdgovora: 0);
        var sesija = DodajSesiju(_kontekst, minutaOdPocetka: 1);
        DodajStavke(_kontekst, sesija.ID, 1);

        var model = ModelIz(await Kontroler().ContinueSimulation(sesija.ID));

        Assert.AreEqual(0, model.Questions[0].Odgovori.Count);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("State verifier")]
    public async Task ContinueSimulation_IsteklaSesija_VracaRezultatIZatvaraSesiju()
    {
        DodajPitanje(_kontekst, 1);
        var sesija = DodajSesiju(_kontekst, minutaOdPocetka: 10);
        DodajStavke(_kontekst, sesija.ID, 1);

        var model = ModelIz(await Kontroler().ContinueSimulation(sesija.ID));

        Assert.IsNotNull(model.Result);
        Assert.AreEqual(1, model.Result.Neodgovorenih);
        Assert.AreEqual(StatusSesije.ISTEKAO, (await _kontekst.KvizSesije.SingleAsync()).Status);
    }
}
