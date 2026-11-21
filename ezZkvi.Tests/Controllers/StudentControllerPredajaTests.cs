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
/// Testovi spašavanja odgovora i predaje simulacije (SaveSimulationAnswer, SubmitSimulation),
/// uključujući bodovanje (+0.1 tačan, -0.03 netačan, 0 preskočen) i statistiku studenta.
/// Autor: Dino Tahirović (A)
/// </summary>
[TestClass]
public sealed class StudentControllerPredajaTests
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

    private static SimulacijaRezultatViewModel RezultatIz(IActionResult rezultat)
    {
        var model = Rezultat.Kao<SimulacijaKvizaViewModel>(Rezultat.Kao<ViewResult>(rezultat).Model);
        Assert.IsNotNull(model.Result);
        return model.Result;
    }

    /// <summary>Aktivna sesija sa pet otvorenih pitanja (Id 1..5). Vraća Id sesije.</summary>
    private int AktivnaSesijaSaPetPitanja(int minutaOdPocetka = 0)
    {
        DodajPetValidnihPitanja(_kontekst);
        var sesija = DodajSesiju(_kontekst, minutaOdPocetka: minutaOdPocetka);
        DodajStavke(_kontekst, sesija.ID, 1, 2, 3, 4, 5);
        return sesija.ID;
    }

    private void DodajStatistiku(int brojKvizova, int ukupnoPitanja, int tacnih)
    {
        _kontekst.StudentStatistike.Add(new StudentStatistika
        {
            KorisnikId = StudentId, PredmetId = PredmetId, BrojKvizova = brojKvizova, UkupnoPitanja = ukupnoPitanja, TacniOdgovori = tacnih
        });
        TestDbFactory.Snimi(_kontekst);
    }

    private static SimulacijaSubmitViewModel Predaja(int sesijaId, params (int PitanjeId, int? OdgovorId)[] odgovori)
    {
        return new SimulacijaSubmitViewModel
        {
            KvizSesijaId = sesijaId,
            Odgovori = odgovori
                .Select(o => new SimulacijaKorisnickiOdgovorViewModel { PitanjeId = o.PitanjeId, OdgovorId = o.OdgovorId })
                .ToList()
        };
    }

    // ---------- SubmitSimulation ----------

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Boundary verifier")]
    [DataRow(0)]
    [DataRow(-5)]
    public async Task SubmitSimulation_NevalidanIdSesije_PreusmjeravaSaGreskom(int sesijaId)
    {
        var kontroler = Kontroler();

        var rezultat = await kontroler.SubmitSimulation(Predaja(sesijaId));

        Assert.AreEqual(nameof(StudentController.Simulate), Rezultat.Kao<RedirectToActionResult>(rezultat).ActionName);
        Assert.AreEqual("Simulacija nije validna.", kontroler.TempData["SimulationError"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task SubmitSimulation_NepostojecaSesija_PreusmjeravaSaGreskom()
    {
        var kontroler = Kontroler();

        var rezultat = await kontroler.SubmitSimulation(Predaja(999));

        Assert.IsInstanceOfType(rezultat, typeof(RedirectToActionResult));
        Assert.AreEqual("Sesija simulacije nije pronađena.", kontroler.TempData["SimulationError"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [Priority(1)]
    [TestCategory("Equality verifier")]
    public async Task SubmitSimulation_TacanOdgovorNaPrvoPitanje_RacunaBodoveIProcenat()
    {
        var sesijaId = AktivnaSesijaSaPetPitanja();

        var rezultat = RezultatIz(await Kontroler().SubmitSimulation(Predaja(sesijaId, (1, TacanOdgovor(1)))));

        Assert.AreEqual(1, rezultat.TacnihOdgovora);
        Assert.AreEqual(0, rezultat.NetacnihOdgovora);
        Assert.AreEqual(4, rezultat.Neodgovorenih);
        Assert.AreEqual(0.1, rezultat.OsvojeniPoeni, 0.0001);
        Assert.AreEqual(0.5, rezultat.MaksimalniPoeni, 0.0001);
        Assert.AreEqual(20, rezultat.Procenat);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("State verifier")]
    public async Task SubmitSimulation_TacanOdgovorNaPrvoPitanje_ZatvaraSesijuIZadrzavaOdgovore()
    {
        var sesijaId = AktivnaSesijaSaPetPitanja();

        await Kontroler().SubmitSimulation(Predaja(sesijaId, (1, TacanOdgovor(1))));

        var sesija = await _kontekst.KvizSesije.SingleAsync();
        Assert.AreEqual(StatusSesije.ZAVRSEN, sesija.Status);
        Assert.AreEqual(1, sesija.BrojTacnih);
        Assert.AreEqual(20, sesija.Procenat);
        Assert.AreEqual(5, await _kontekst.KvizSesijaPitanja.CountAsync());
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task SubmitSimulation_TacanOdgovor_PregledSadrziKorisnickiITacanOdgovor()
    {
        var sesijaId = AktivnaSesijaSaPetPitanja();

        var rezultat = RezultatIz(await Kontroler().SubmitSimulation(Predaja(sesijaId, (1, TacanOdgovor(1)))));

        Assert.AreEqual(5, rezultat.Pregled.Count);
        Assert.AreEqual("Pitanje broj 1", rezultat.Pregled[0].TekstPitanja);
        Assert.AreEqual("Odgovor 1-1", rezultat.Pregled[0].KorisnickiOdgovor);
        Assert.AreEqual("Odgovor 1-1", rezultat.Pregled[0].TacanOdgovor);
        Assert.IsTrue(rezultat.Pregled[0].JeTacno);
        Assert.IsTrue(rezultat.Pregled[1].JePreskoceno);
        Assert.IsNull(rezultat.Pregled[1].KorisnickiOdgovor);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("State verifier")]
    public async Task SubmitSimulation_PrviKviz_KreiraStatistikuStudenta()
    {
        var sesijaId = AktivnaSesijaSaPetPitanja();

        await Kontroler().SubmitSimulation(Predaja(sesijaId, (1, TacanOdgovor(1))));

        var statistika = await _kontekst.StudentStatistike.SingleAsync();
        Assert.AreEqual(StudentId, statistika.KorisnikId);
        Assert.AreEqual(PredmetId, statistika.PredmetId);
        Assert.AreEqual(1, statistika.BrojKvizova);
        Assert.AreEqual(5, statistika.UkupnoPitanja);
        Assert.AreEqual(1, statistika.TacniOdgovori);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("State verifier")]
    public async Task SubmitSimulation_PostojecaStatistika_UvecavaPostojeceVrijednosti()
    {
        DodajStatistiku(brojKvizova: 2, ukupnoPitanja: 10, tacnih: 7);
        var sesijaId = AktivnaSesijaSaPetPitanja();

        await Kontroler().SubmitSimulation(Predaja(sesijaId));

        var statistika = await _kontekst.StudentStatistike.SingleAsync();
        Assert.AreEqual(3, statistika.BrojKvizova);
        Assert.AreEqual(15, statistika.UkupnoPitanja);
        Assert.AreEqual(7, statistika.TacniOdgovori);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("State verifier")]
    public async Task SubmitSimulation_SesijaBezPredmeta_NeKreiraStatistiku()
    {
        DodajPitanje(_kontekst, 1);
        var sesija = DodajSesiju(_kontekst, predmetId: null, oblastId: null);
        DodajStavke(_kontekst, sesija.ID, 1);

        await Kontroler().SubmitSimulation(Predaja(sesija.ID));

        Assert.AreEqual(0, await _kontekst.StudentStatistike.CountAsync());
        Assert.AreEqual(StatusSesije.ZAVRSEN, (await _kontekst.KvizSesije.SingleAsync()).Status);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task SubmitSimulation_OdgovorNaPitanjeKojeNijePrvoOtvoreno_IgnoriseOdgovor()
    {
        var sesijaId = AktivnaSesijaSaPetPitanja();

        var rezultat = RezultatIz(await Kontroler().SubmitSimulation(Predaja(sesijaId, (2, TacanOdgovor(2)))));

        Assert.AreEqual(0, rezultat.TacnihOdgovora);
        Assert.AreEqual(5, rezultat.Neodgovorenih);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task SubmitSimulation_OdgovorKojiPripadaDrugomPitanju_IgnoriseOdgovor()
    {
        var sesijaId = AktivnaSesijaSaPetPitanja();

        var rezultat = RezultatIz(await Kontroler().SubmitSimulation(Predaja(sesijaId, (1, TacanOdgovor(2)))));

        Assert.AreEqual(5, rezultat.Neodgovorenih);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task SubmitSimulation_PitanjeKojeNijeDioSesije_IgnoriseOdgovor()
    {
        var sesijaId = AktivnaSesijaSaPetPitanja();

        var rezultat = RezultatIz(await Kontroler().SubmitSimulation(Predaja(sesijaId, (99, 991))));

        Assert.AreEqual(5, rezultat.Neodgovorenih);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task SubmitSimulation_OdgovoriBezOdabraneOpcije_NistaSeNeSpasava()
    {
        var sesijaId = AktivnaSesijaSaPetPitanja();

        var rezultat = RezultatIz(await Kontroler().SubmitSimulation(Predaja(sesijaId, (1, null), (2, null))));

        Assert.AreEqual(5, rezultat.Neodgovorenih);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task SubmitSimulation_ViseOdgovoraNaIstoPitanje_UzimaSeZadnji()
    {
        var sesijaId = AktivnaSesijaSaPetPitanja();

        var rezultat = RezultatIz(await Kontroler().SubmitSimulation(
            Predaja(sesijaId, (1, NetacanOdgovor(1)), (1, TacanOdgovor(1)))));

        Assert.AreEqual(1, rezultat.TacnihOdgovora);
        Assert.AreEqual(0, rezultat.NetacnihOdgovora);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task SubmitSimulation_ZakljucanoPitanje_NeMijenjaRanijiOdgovor()
    {
        DodajPitanje(_kontekst, 1);
        DodajPitanje(_kontekst, 2);
        var sesija = DodajSesiju(_kontekst);
        DodajStavku(_kontekst, sesija.ID, 1, 1, NetacanOdgovor(1), 0, -0.03);
        DodajStavku(_kontekst, sesija.ID, 2, 2);

        var rezultat = RezultatIz(await Kontroler().SubmitSimulation(Predaja(sesija.ID, (1, TacanOdgovor(1)))));

        Assert.AreEqual(0, rezultat.TacnihOdgovora);
        Assert.AreEqual(1, rezultat.NetacnihOdgovora);
    }

    /// <summary>
    /// Kad vrijeme istekne, forma se automatski preda (tajmer u pogledu). Student tada treba vidjeti rezultat,
    /// sesija treba biti označena kao istekla, a odgovor koji nije potvrđen prije isteka se ne boduje.
    /// </summary>
    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("State verifier")]
    public async Task SubmitSimulation_IsteklaSesija_VracaRezultatIStatusIstekao()
    {
        var sesijaId = AktivnaSesijaSaPetPitanja(minutaOdPocetka: 10);

        var rezultat = await Kontroler().SubmitSimulation(Predaja(sesijaId, (1, TacanOdgovor(1))));

        var sesija = await _kontekst.KvizSesije.SingleAsync();
        Assert.AreEqual(5, RezultatIz(rezultat).Neodgovorenih);
        Assert.AreEqual(0, RezultatIz(rezultat).TacnihOdgovora);
        Assert.AreEqual(StatusSesije.ISTEKAO, sesija.Status);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("State verifier")]
    public async Task SubmitSimulation_VecZavrsenaSesija_VracaRezultatBezPonovneObrade()
    {
        DodajPitanje(_kontekst, 1);
        var sesija = DodajSesiju(_kontekst, status: StatusSesije.ZAVRSEN, brojTacnih: 1, procenat: 100);
        DodajStavku(_kontekst, sesija.ID, 1, 1, TacanOdgovor(1), 1, 0.1);

        var rezultat = RezultatIz(await Kontroler().SubmitSimulation(Predaja(sesija.ID)));

        Assert.AreEqual(100, rezultat.Procenat);
        Assert.AreEqual(0, await _kontekst.StudentStatistike.CountAsync());
        Assert.AreEqual(1, await _kontekst.KvizSesijaPitanja.CountAsync());
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task SubmitSimulation_PitanjeBezTacnogOdgovora_PregledNavodiDaTacanNijeDefinisan()
    {
        DodajPitanje(_kontekst, 1, brojTacnih: 0);
        var sesija = DodajSesiju(_kontekst);
        DodajStavke(_kontekst, sesija.ID, 1);

        var rezultat = RezultatIz(await Kontroler().SubmitSimulation(Predaja(sesija.ID)));

        Assert.AreEqual("Nije definisan tačan odgovor", rezultat.Pregled[0].TacanOdgovor);
    }

    /// <summary>Data-driven (JSON): različite kombinacije odgovora i očekivani bodovi/procenat.</summary>
    public static IEnumerable<object[]> ScenarijiOcjenjivanja =>
        TestPodaci.Json("ocjenjivanje_kviza.json").Select(s => new object[]
        {
            s.GetProperty("tacnih").GetInt32(),
            s.GetProperty("netacnih").GetInt32(),
            s.GetProperty("preskocenih").GetInt32(),
            s.GetProperty("ocekivaniPoeni").GetDouble(),
            s.GetProperty("ocekivaniProcenat").GetInt32()
        });

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [Priority(1)]
    [TestCategory("Equality verifier")]
    [DynamicData(nameof(ScenarijiOcjenjivanja), DynamicDataSourceType.Property)]
    public async Task SubmitSimulation_KombinacijaOdgovora_RacunaBodoveIProcenat(
        int tacnih, int netacnih, int preskocenih, double ocekivaniPoeni, int ocekivaniProcenat)
    {
        var sesijaId = SesijaSaStanjimaOdgovora(_kontekst, tacnih, netacnih, preskocenih);

        var rezultat = RezultatIz(await Kontroler().SubmitSimulation(Predaja(sesijaId)));

        Assert.AreEqual(tacnih, rezultat.TacnihOdgovora);
        Assert.AreEqual(netacnih, rezultat.NetacnihOdgovora);
        Assert.AreEqual(preskocenih, rezultat.Neodgovorenih);
        Assert.AreEqual(ocekivaniPoeni, rezultat.OsvojeniPoeni, 0.0001);
        Assert.AreEqual(ocekivaniProcenat, rezultat.Procenat);
    }

    // ---------- SaveSimulationAnswer ----------

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task SaveSimulationAnswer_NepostojecaSesija_VracaBadRequest()
    {
        var rezultat = await Kontroler().SaveSimulationAnswer(
            new SaveSimulationAnswerViewModel { KvizSesijaId = 999, PitanjeId = 1, OdgovorId = 11 });

        Assert.AreEqual("Aktivna sesija nije pronađena.", Rezultat.Kao<BadRequestObjectResult>(rezultat).Value);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("State verifier")]
    public async Task SaveSimulationAnswer_IsteklaSesija_VracaBadRequestIZatvaraSesiju()
    {
        var sesijaId = AktivnaSesijaSaPetPitanja(minutaOdPocetka: 10);

        var rezultat = await Kontroler().SaveSimulationAnswer(
            new SaveSimulationAnswerViewModel { KvizSesijaId = sesijaId, PitanjeId = 1, OdgovorId = TacanOdgovor(1) });

        Assert.AreEqual("Vrijeme je isteklo.", Rezultat.Kao<BadRequestObjectResult>(rezultat).Value);
        Assert.AreEqual(StatusSesije.ISTEKAO, (await _kontekst.KvizSesije.SingleAsync()).Status);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task SaveSimulationAnswer_PitanjeNijeDioSesije_VracaBadRequest()
    {
        var sesijaId = AktivnaSesijaSaPetPitanja();

        var rezultat = await Kontroler().SaveSimulationAnswer(
            new SaveSimulationAnswerViewModel { KvizSesijaId = sesijaId, PitanjeId = 99, OdgovorId = 991 });

        Assert.AreEqual("Pitanje nije dio ove sesije.", Rezultat.Kao<BadRequestObjectResult>(rezultat).Value);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task SaveSimulationAnswer_VecPotvrdjenoPitanje_VracaBadRequest()
    {
        DodajPitanje(_kontekst, 1);
        DodajPitanje(_kontekst, 2);
        var sesija = DodajSesiju(_kontekst);
        DodajStavku(_kontekst, sesija.ID, 1, 1, TacanOdgovor(1), 1, 0.1);
        DodajStavku(_kontekst, sesija.ID, 2, 2);

        var rezultat = await Kontroler().SaveSimulationAnswer(
            new SaveSimulationAnswerViewModel { KvizSesijaId = sesija.ID, PitanjeId = 1, OdgovorId = NetacanOdgovor(1) });

        Assert.AreEqual("Pitanje je već potvrđeno ili preskočeno.", Rezultat.Kao<BadRequestObjectResult>(rezultat).Value);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task SaveSimulationAnswer_PreskakanjePitanja_VracaJsonSaSkippedTrue()
    {
        var sesijaId = AktivnaSesijaSaPetPitanja();

        var rezultat = await Kontroler().SaveSimulationAnswer(
            new SaveSimulationAnswerViewModel { KvizSesijaId = sesijaId, PitanjeId = 1, OdgovorId = null });

        var json = Rezultat.Kao<JsonResult>(rezultat);
        Assert.AreEqual(true, Rezultat.Svojstvo(json.Value!, "saved"));
        Assert.AreEqual(true, Rezultat.Svojstvo(json.Value!, "skipped"));
    }

    /// <summary>Data-driven (CSV): kombinacije pitanja/odgovora i očekivani ishod spašavanja.</summary>
    public static IEnumerable<object[]> KombinacijeOdgovora =>
        TestPodaci.Csv("sacuvaj_odgovor.csv").Select(red => new object[]
        {
            TestPodaci.Int(red[0]),
            TestPodaci.NullableInt(red[1])!,
            red[2].Trim(),
            TestPodaci.Double(red[3]),
            TestPodaci.Double(red[4])
        });

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [Priority(1)]
    [TestCategory("Equality verifier")]
    [DynamicData(nameof(KombinacijeOdgovora), DynamicDataSourceType.Property)]
    public async Task SaveSimulationAnswer_KombinacijaPitanjaIOdgovora_VracaOcekivaniIshod(
        int pitanjeId, int? odgovorId, string ocekivaniTipRezultata, double ocekivanoTacno, double ocekivaniBodovi)
    {
        var sesijaId = AktivnaSesijaSaPetPitanja();

        var rezultat = await Kontroler().SaveSimulationAnswer(
            new SaveSimulationAnswerViewModel { KvizSesijaId = sesijaId, PitanjeId = pitanjeId, OdgovorId = odgovorId });

        var stavka = await _kontekst.KvizSesijaPitanja.SingleAsync(x => x.PitanjeId == pitanjeId);
        Assert.AreEqual(ocekivaniTipRezultata, rezultat.GetType().Name);
        Assert.AreEqual(ocekivanoTacno, stavka.Tacno, 0.0001);
        Assert.AreEqual(ocekivaniBodovi, stavka.BrojBodova, 0.0001);
    }
}
