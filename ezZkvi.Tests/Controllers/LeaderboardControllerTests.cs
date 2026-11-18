using ezZkvi.Controllers;
using ezZkvi.Data;
using ezZkvi.Models;
using ezZkvi.Tests.Infrastruktura;
using ezZkvi.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ezZkvi.Tests.Controllers;

/// <summary>
/// Testovi rang-liste (leaderboard): agregacija statistike, tačnost, bodovi, sortiranje i filter po predmetu.
/// Autor: Dino Tahirović (A)
/// </summary>
[TestClass]
public sealed class LeaderboardControllerTests
{
    private ApplicationDbContext _kontekst = null!;

    [TestInitialize]
    public void Postavi()
    {
        _kontekst = TestDbFactory.Kreiraj();
        _kontekst.Predmet.AddRange(
            new Predmet { Id = 1, Naziv = "Matematika" },
            new Predmet { Id = 2, Naziv = "Fizika" });
        _kontekst.Users.AddRange(
            new Korisnik { Id = "s1", UserName = "ana@etf.unsa.ba" },
            new Korisnik { Id = "s2", UserName = "b" });
        TestDbFactory.Snimi(_kontekst);
    }

    [TestCleanup]
    public void Ocisti()
    {
        _kontekst.Dispose();
    }

    private LeaderboardController Kontroler(string korisnikId = "s1", string uloga = "Student")
    {
        return KontrolerHelper.SaKorisnikom(new LeaderboardController(_kontekst), korisnikId, uloga);
    }

    private void DodajStatistiku(string korisnikId, int predmetId, int kvizova, int pitanja, int tacnih)
    {
        _kontekst.StudentStatistike.Add(new StudentStatistika
        {
            KorisnikId = korisnikId,
            PredmetId = predmetId,
            BrojKvizova = kvizova,
            UkupnoPitanja = pitanja,
            TacniOdgovori = tacnih
        });
        TestDbFactory.Snimi(_kontekst);
    }

    private async Task<List<LeaderboardEntryViewModel>> Podaci(int? predmetId = null, string korisnikId = "s1")
    {
        var rezultat = await Kontroler(korisnikId).Data(predmetId);
        return Rezultat.Kao<List<LeaderboardEntryViewModel>>(Rezultat.Kao<JsonResult>(rezultat).Value);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Empty tester")]
    public async Task Data_BezStatistike_VracaPraznuListu()
    {
        var unosi = await Podaci();

        Assert.AreEqual(0, unosi.Count);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [Priority(1)]
    [TestCategory("Equality verifier")]
    public async Task Data_ViseStudenata_SortiraPoTacnostiOdNajvece()
    {
        DodajStatistiku("s1", 1, 2, 10, 9);
        DodajStatistiku("s2", 1, 1, 5, 5);
        DodajStatistiku("s3", 2, 1, 5, 2);

        var unosi = await Podaci();

        CollectionAssert.AreEqual(new[] { "b", "ana", "Student" }, unosi.Select(u => u.Ime).ToArray());
        CollectionAssert.AreEqual(new[] { 100, 90, 40 }, unosi.Select(u => u.Tacnost).ToArray());
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task Data_JednakaTacnost_SortiraPoBodovimaPaPoKvizovima()
    {
        DodajStatistiku("x", 1, 1, 10, 5);
        DodajStatistiku("y", 1, 1, 20, 10);
        DodajStatistiku("z", 1, 3, 20, 10);
        _kontekst.Users.AddRange(
            new Korisnik { Id = "x", UserName = "xavier" },
            new Korisnik { Id = "y", UserName = "yasmin" },
            new Korisnik { Id = "z", UserName = "zana" });
        TestDbFactory.Snimi(_kontekst);

        var unosi = await Podaci();

        CollectionAssert.AreEqual(new[] { "zana", "yasmin", "xavier" }, unosi.Select(u => u.Ime).ToArray());
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task Data_StatistikaViseOblastiIstogStudenta_SabiraSe()
    {
        DodajStatistiku("s1", 1, 1, 5, 4);
        DodajStatistiku("s1", 2, 2, 10, 5);

        var unos = (await Podaci()).Single();

        Assert.AreEqual(3, unos.Kvizovi);
        Assert.AreEqual(90, unos.Bodovi);
        Assert.AreEqual(60, unos.Tacnost);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Utility verifier")]
    public async Task Data_FilterPoPredmetu_VracaSamoStatistikuTogPredmeta()
    {
        DodajStatistiku("s1", 1, 2, 10, 9);
        DodajStatistiku("s3", 2, 1, 5, 2);

        var unos = (await Podaci(predmetId: 2)).Single();

        Assert.AreEqual("Student", unos.Ime);
        Assert.AreEqual("ST", unos.Inicijali);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task Data_KorisnickoImeJeEmail_PrikazujeDioPrijeAtIInicijale()
    {
        DodajStatistiku("s1", 1, 1, 5, 5);

        var unos = (await Podaci()).Single();

        Assert.AreEqual("ana", unos.Ime);
        Assert.AreEqual("AN", unos.Inicijali);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Boundary verifier")]
    public async Task Data_ImeOdJednogZnaka_InicijaliSuTajZnak()
    {
        DodajStatistiku("s2", 1, 1, 5, 5);

        var unos = (await Podaci()).Single();

        Assert.AreEqual("B", unos.Inicijali);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Boolean verifier")]
    public async Task Data_PrijavljeniStudent_OznacenJeKaoTrenutni()
    {
        DodajStatistiku("s1", 1, 1, 5, 5);
        DodajStatistiku("s2", 1, 1, 5, 4);

        var unosi = await Podaci(korisnikId: "s2");

        Assert.AreEqual("b", unosi.Single(u => u.JeTrenutni).Ime);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Utility verifier")]
    public async Task Data_StatistikaBezKvizovaIliPitanja_Izostavlja()
    {
        DodajStatistiku("s1", 1, 0, 5, 5);
        DodajStatistiku("s2", 1, 1, 0, 0);

        var unosi = await Podaci();

        Assert.AreEqual(0, unosi.Count);
    }

    /// <summary>Data-driven (CSV): tačni/ukupno -> tačnost u % (zaokruživanje) i bodovi (10 po tačnom).</summary>
    public static IEnumerable<object[]> TacnostIBodovi =>
        TestPodaci.Csv("leaderboard_tacnost.csv").Select(red => new object[]
        {
            TestPodaci.Int(red[0]), TestPodaci.Int(red[1]), TestPodaci.Int(red[2]), TestPodaci.Int(red[3])
        });

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    [DynamicData(nameof(TacnostIBodovi), DynamicDataSourceType.Property)]
    public async Task Data_TacniIUkupnoPitanja_RacunaTacnostIBodove(int tacnih, int ukupno, int ocekivanaTacnost, int ocekivaniBodovi)
    {
        DodajStatistiku("s1", 1, 1, ukupno, tacnih);

        var unos = (await Podaci()).Single();

        Assert.AreEqual(ocekivanaTacnost, unos.Tacnost);
        Assert.AreEqual(ocekivaniBodovi, unos.Bodovi);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("State verifier")]
    public async Task Index_SaFilterom_VracaViewSaPredmetimaIOdabranimPredmetom()
    {
        DodajStatistiku("s1", 1, 1, 5, 5);
        var kontroler = Kontroler();

        var rezultat = await kontroler.Index(1);

        var model = Rezultat.Kao<LeaderboardViewModel>(Rezultat.Kao<ViewResult>(rezultat).Model);
        Assert.AreEqual(1, model.Entries.Count);
        Assert.AreEqual(1, kontroler.ViewData["SelectedPredmetId"]);
        Assert.IsNotNull(kontroler.ViewData["Predmeti"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    [DataRow("Admin", "Administrator")]
    [DataRow("Moderator", "Moderator")]
    [DataRow("Student", "Student")]
    public async Task Index_UlogaKorisnika_OdredjujeKontrolerZaPovratak(string uloga, string ocekivaniKontroler)
    {
        var kontroler = Kontroler(uloga: uloga);

        await kontroler.Index(null);

        Assert.AreEqual(ocekivaniKontroler, kontroler.ViewData["BackController"]);
    }
}
