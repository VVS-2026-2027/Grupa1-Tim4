using System.Text;
using ezZkvi.Controllers;
using ezZkvi.Data;
using ezZkvi.Models;
using ezZkvi.Tests.Infrastruktura;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using static ezZkvi.Tests.Controllers.SadrzajTestSeed;

namespace ezZkvi.Tests.Controllers;

/// <summary>
/// Testovi upravljanja predmetima i CSV izvoza/uvoza pitanja.
/// Zamjenski objekat: Mock&lt;IFormFile&gt; - uploadovani CSV fajl se simulira bez HTTP zahtjeva.
/// Autor: Imran Čustović (B)
/// </summary>
[TestClass]
public sealed class PredmetControllerTests
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

    private PredmetController Kontroler(string korisnikId = AdminId, string uloga = "Admin")
    {
        return KontrolerHelper.SaKorisnikom(new PredmetController(_kontekst), korisnikId, uloga);
    }

    private PredmetController Moderator() => Kontroler(ModeratorId, "Moderator");

    private static Mock<IFormFile> CsvFajl(string sadrzaj)
    {
        var bajtovi = Encoding.UTF8.GetBytes(sadrzaj);
        var fajl = new Mock<IFormFile>();
        fajl.SetupGet(f => f.Length).Returns(bajtovi.Length);
        fajl.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(bajtovi));
        return fajl;
    }

    private static string[] LinijeCsvIzvoza(IActionResult rezultat)
    {
        var fajl = Rezultat.Kao<FileContentResult>(rezultat);
        var tekst = Encoding.UTF8.GetString(fajl.FileContents, 3, fajl.FileContents.Length - 3);
        return tekst.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
    }

    private const string ZaglavljeCsv = "TekstPitanja,OdgovorA,OdgovorB,OdgovorC,OdgovorD,Tacan,Tezina,Oblast";

    /// <summary>
    /// CSV sa 3 ispravna reda i 5 neispravnih (prekratak tekst, jedan odgovor, nepostojeći tačan odgovor,
    /// premalo kolona, duplikat postojećeg pitanja) plus jedan prazan red koji se samo preskače.
    /// </summary>
    private static readonly string MijesaniCsv = string.Join("\n",
        ZaglavljeCsv,
        "Koliko je 2+2?,3,4,5,6,B,LAKO",
        "\"Šta je \"\"vektor\"\", ukratko?\",Smjer i intenzitet,Broj,,,A,tesko",
        "",
        "Ne?,a,b,c,d,A,LAKO",
        "Pitanje sa jednim odgovorom,a,,,,A,LAKO",
        "Pitanje sa nepostojecim tacnim,a,b,,,C,LAKO",
        "Premalo polja,a,b",
        "  ŠTA JE   matrica? ,a,b,c,d,A,LAKO",
        "Pitanje sa nepoznatom tezinom,a,b,c,d,D,EKSTREMNO");

    // ---------- ExportCsv ----------

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    public async Task ExportCsv_NepostojeciPredmet_PreusmjeravaSaGreskom()
    {
        var kontroler = Kontroler();

        var rezultat = await kontroler.ExportCsv(99);

        Assert.AreEqual("questions", Rezultat.Kao<RedirectToActionResult>(rezultat).RouteValues!["tab"]);
        Assert.AreEqual("Odaberi predmet za preuzimanje CSV-a.", kontroler.TempData["Error"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    public async Task ExportCsv_ModeratorTudjegPredmeta_PreusmjeravaSaGreskom()
    {
        var kontroler = Moderator();

        var rezultat = await kontroler.ExportCsv(2);

        Assert.IsInstanceOfType(rezultat, typeof(RedirectToActionResult));
        Assert.AreEqual("Nemaš pristup ovom predmetu.", kontroler.TempData["Error"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task ExportCsv_ModeratorSvogPredmeta_VracaCsvFajlSaBomZaglavljem()
    {
        var rezultat = await Moderator().ExportCsv(1);

        var fajl = Rezultat.Kao<FileContentResult>(rezultat);
        Assert.AreEqual("text/csv", fajl.ContentType);
        Assert.AreEqual("pitanja_Matematika.csv", fajl.FileDownloadName);
        CollectionAssert.AreEqual(new byte[] { 0xEF, 0xBB, 0xBF }, fajl.FileContents.Take(3).ToArray());
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    public async Task ExportCsv_PitanjaPredmeta_SortiranaPoOblastiSaTacnimSlovom()
    {
        var linije = LinijeCsvIzvoza(await Kontroler().ExportCsv(1));

        CollectionAssert.AreEqual(new[]
        {
            ZaglavljeCsv,
            "Šta je matrica?,Odgovor 1-1,Odgovor 1-2,Odgovor 1-3,Odgovor 1-4,A,LAKO,Algebra",
            "Šta je derivacija?,Odgovor 2-1,Odgovor 2-2,Odgovor 2-3,Odgovor 2-4,A,LAKO,Analiza"
        }, linije);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    public async Task ExportCsv_TekstSaZarezomINavodnicima_PoljaSuEscapeovana()
    {
        _kontekst.Oblast.Add(new Oblast { Id = 7, Naziv = "Stehiometrija", PredmetId = 3 });
        _kontekst.Pitanje.Add(new Pitanje { Id = 7, TekstPitanja = "Šta je \"mol\", ukratko?", PredmetId = 3, OblastId = 7, Tezina = Tezina.SREDNJE });
        _kontekst.Odgovor.AddRange(
            new Odgovor { Id = 71, Tekst = "Jedinica", PitanjeId = 7 },
            new Odgovor { Id = 72, Tekst = "6,022", PitanjeId = 7 },
            new Odgovor { Id = 73, Tekst = "Avogadro", PitanjeId = 7, IsTacan = true });
        TestDbFactory.Snimi(_kontekst);

        var linije = LinijeCsvIzvoza(await Kontroler().ExportCsv(3));

        Assert.AreEqual("\"Šta je \"\"mol\"\", ukratko?\",Jedinica,\"6,022\",Avogadro,,C,SREDNJE,Stehiometrija", linije[1]);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    public async Task ExportCsv_PitanjeBezOdgovora_IzvoziPraznaPoljaBezTacnog()
    {
        _kontekst.Oblast.Add(new Oblast { Id = 7, Naziv = "Kiseline", PredmetId = 3 });
        _kontekst.Pitanje.Add(new Pitanje { Id = 7, TekstPitanja = "Šta je pH?", PredmetId = 3, OblastId = 7, Tezina = Tezina.TESKO });
        TestDbFactory.Snimi(_kontekst);

        var linije = LinijeCsvIzvoza(await Kontroler().ExportCsv(3));

        Assert.AreEqual("Šta je pH?,,,,,,TESKO,Kiseline", linije[1]);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    public async Task ExportCsv_PredmetBezPitanja_VracaSamoZaglavlje()
    {
        var linije = LinijeCsvIzvoza(await Kontroler().ExportCsv(3));

        CollectionAssert.AreEqual(new[] { ZaglavljeCsv }, linije);
    }

    // ---------- ImportCsv (Mock<IFormFile>) ----------

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    public async Task ImportCsv_NepostojeciPredmet_PreusmjeravaSaGreskom()
    {
        var kontroler = Kontroler();
        var fajl = CsvFajl(ZaglavljeCsv);

        await kontroler.ImportCsv(99, 1, fajl.Object);

        Assert.AreEqual("Odaberi predmet za uvoz.", kontroler.TempData["Error"]);
        fajl.Verify(f => f.OpenReadStream(), Times.Never());
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    public async Task ImportCsv_ModeratorTudjegPredmeta_PreusmjeravaSaGreskom()
    {
        var kontroler = Moderator();

        await kontroler.ImportCsv(2, 3, CsvFajl(ZaglavljeCsv).Object);

        Assert.AreEqual("Nemaš pristup ovom predmetu.", kontroler.TempData["Error"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    public async Task ImportCsv_OblastIzDrugogPredmeta_PreusmjeravaSaGreskom()
    {
        var kontroler = Kontroler();

        await kontroler.ImportCsv(1, 3, CsvFajl(ZaglavljeCsv).Object);

        Assert.AreEqual("Odabrana oblast ne pripada predmetu ili nemaš pristup.", kontroler.TempData["Error"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Null verifier")]
    public async Task ImportCsv_BezFajla_PreusmjeravaSaGreskom()
    {
        var kontroler = Kontroler();

        await kontroler.ImportCsv(1, 1, null!);

        Assert.AreEqual("Odaberi CSV fajl.", kontroler.TempData["Error"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Internal call verifier")]
    public async Task ImportCsv_PrazanFajl_NeCitaSadrzajIVracaGresku()
    {
        var kontroler = Kontroler();
        var fajl = CsvFajl(string.Empty);

        await kontroler.ImportCsv(1, 1, fajl.Object);

        Assert.AreEqual("Odaberi CSV fajl.", kontroler.TempData["Error"]);
        fajl.Verify(f => f.OpenReadStream(), Times.Never());
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [Priority(1)]
    [TestCategory("Internal call verifier")]
    public async Task ImportCsv_IspravniIneispravniRedovi_UvoziIspravneIBrojiPreskocene()
    {
        var kontroler = Kontroler();
        var fajl = CsvFajl(MijesaniCsv);

        await kontroler.ImportCsv(1, 1, fajl.Object);

        Assert.AreEqual("Uvezeno 3 pitanja. Preskočeno 5 neispravnih redova.", kontroler.TempData["Success"]);
        Assert.AreEqual(6, await _kontekst.Pitanje.CountAsync());
        fajl.Verify(f => f.OpenReadStream(), Times.Once());
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task ImportCsv_IspravanRed_KreiraPitanjeSaOdgovorimaITacnimOdgovorom()
    {
        await Kontroler().ImportCsv(1, 1, CsvFajl(ZaglavljeCsv + "\nKoliko je 2+2?,3,4,5,6,B,LAKO").Object);

        var pitanje = await _kontekst.Pitanje.SingleAsync(p => p.TekstPitanja == "Koliko je 2+2?");
        var odgovori = await _kontekst.Odgovor.Where(o => o.PitanjeId == pitanje.Id).OrderBy(o => o.Id).ToListAsync();
        Assert.AreEqual(Tezina.LAKO, pitanje.Tezina);
        Assert.AreEqual(1, pitanje.OblastId);
        CollectionAssert.AreEqual(new[] { "3", "4", "5", "6" }, odgovori.Select(o => o.Tekst).ToArray());
        Assert.AreEqual("4", odgovori.Single(o => o.IsTacan).Tekst);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task ImportCsv_PoljaUNavodnicimaIMalaSlovaTezine_IspravnoParsira()
    {
        var kontroler = Kontroler();

        await kontroler.ImportCsv(1, 1, CsvFajl(ZaglavljeCsv + "\n\"Šta je \"\"vektor\"\", ukratko?\",\"Smjer, intenzitet\",Broj,,,A,tesko").Object);

        var pitanje = await _kontekst.Pitanje.SingleAsync(p => p.TekstPitanja == "Šta je \"vektor\", ukratko?");
        var odgovori = await _kontekst.Odgovor.Where(o => o.PitanjeId == pitanje.Id).OrderBy(o => o.Id).ToListAsync();
        Assert.AreEqual(Tezina.TESKO, pitanje.Tezina);
        CollectionAssert.AreEqual(new[] { "Smjer, intenzitet", "Broj" }, odgovori.Select(o => o.Tekst).ToArray());
        Assert.AreEqual("Uvezeno 1 pitanja.", kontroler.TempData["Success"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    public async Task ImportCsv_NepoznataTezina_PostavljaSrednjuTezinu()
    {
        await Kontroler().ImportCsv(1, 1, CsvFajl(ZaglavljeCsv + "\nPitanje sa nepoznatom tezinom,a,b,c,d,D,EKSTREMNO").Object);

        var pitanje = await _kontekst.Pitanje.SingleAsync(p => p.TekstPitanja == "Pitanje sa nepoznatom tezinom");
        Assert.AreEqual(Tezina.SREDNJE, pitanje.Tezina);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Boundary verifier")]
    public async Task ImportCsv_OdgovorDuziOd500Znakova_PreskaceRed()
    {
        var kontroler = Kontroler();
        var predugOdgovor = new string('x', 501);

        await kontroler.ImportCsv(1, 1, CsvFajl($"{ZaglavljeCsv}\nPitanje sa dugim odgovorom,{predugOdgovor},b,c,d,A,LAKO").Object);

        Assert.AreEqual("Uvezeno 0 pitanja. Preskočeno 1 neispravnih redova.", kontroler.TempData["Success"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Exception verifier")]
    [ExpectedException(typeof(IOException))]
    public async Task ImportCsv_GreskaPriCitanjuFajla_PropagiraIOException()
    {
        var fajl = new Mock<IFormFile>();
        fajl.SetupGet(f => f.Length).Returns(100);
        fajl.Setup(f => f.OpenReadStream()).Throws(new IOException("Fajl je oštećen."));

        await Kontroler().ImportCsv(1, 1, fajl.Object);
    }

    // ---------- CreateFromContent ----------

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    public async Task CreateFromContent_NevalidanModel_PreusmjeravaSaGreskom()
    {
        var kontroler = Kontroler();
        kontroler.ModelState.AddModelError("Naziv", "Obavezno");

        await kontroler.CreateFromContent(new Predmet { Naziv = "Biologija" });

        Assert.AreEqual("Predmet nije sačuvan. Naziv mora imati između 2 i 100 karaktera.", kontroler.TempData["Error"]);
        Assert.AreEqual(3, await _kontekst.Predmet.CountAsync());
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    [DataRow("B", "Predmet nije sačuvan. Naziv mora imati između 2 i 100 karaktera.")]
    [DataRow("Biologija 2.0", "Naziv predmeta smije sadržavati samo slova, brojeve i razmake.")]
    [DataRow("  MATEMATIKA  ", "Predmet sa istim nazivom već postoji.")]
    public async Task CreateFromContent_NeispravanNaziv_PreusmjeravaSaGreskom(string naziv, string ocekivanaPoruka)
    {
        var kontroler = Kontroler();

        var rezultat = await kontroler.CreateFromContent(new Predmet { Naziv = naziv });

        Assert.AreEqual("subjects", Rezultat.Kao<RedirectToActionResult>(rezultat).RouteValues!["tab"]);
        Assert.AreEqual(ocekivanaPoruka, kontroler.TempData["Error"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task CreateFromContent_Admin_KreiraSistemskiPredmetSaPocetnomOblascu()
    {
        var kontroler = Kontroler();

        await kontroler.CreateFromContent(new Predmet { Naziv = "  Biologija   ćelije " });

        var predmet = await _kontekst.Predmet.SingleAsync(p => p.Naziv == "Biologija ćelije");
        Assert.IsNull(predmet.KreatorId);
        Assert.AreEqual("Oblast 1", (await _kontekst.Oblast.SingleAsync(o => o.PredmetId == predmet.Id)).Naziv);
        Assert.AreEqual("Predmet je uspješno dodan.", kontroler.TempData["Success"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    public async Task CreateFromContent_Moderator_PostajeKreatorPredmeta()
    {
        await Moderator().CreateFromContent(new Predmet { Naziv = "Biologija" });

        Assert.AreEqual(ModeratorId, (await _kontekst.Predmet.SingleAsync(p => p.Naziv == "Biologija")).KreatorId);
    }

    // ---------- EditFromContent ----------

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public async Task EditFromContent_RazlicitiId_VracaNotFound()
    {
        Assert.IsInstanceOfType(await Kontroler().EditFromContent(1, new Predmet { Id = 2, Naziv = "X" }), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public async Task EditFromContent_NepostojeciPredmet_VracaNotFound()
    {
        Assert.IsInstanceOfType(await Kontroler().EditFromContent(99, new Predmet { Id = 99, Naziv = "X" }), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public async Task EditFromContent_ModeratorTudjegPredmeta_VracaForbid()
    {
        Assert.IsInstanceOfType(await Moderator().EditFromContent(2, new Predmet { Id = 2, Naziv = "Fizika 2" }), typeof(ForbidResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    [DataRow("M", "Naziv predmeta mora imati između 2 i 100 karaktera.")]
    [DataRow("Matematika-1", "Naziv predmeta smije sadržavati samo slova, brojeve i razmake.")]
    [DataRow("fizika", "Predmet sa istim nazivom već postoji.")]
    public async Task EditFromContent_NeispravanNaziv_NeMijenjaPredmet(string naziv, string ocekivanaPoruka)
    {
        var kontroler = Kontroler();

        await kontroler.EditFromContent(1, new Predmet { Id = 1, Naziv = naziv });

        Assert.AreEqual(ocekivanaPoruka, kontroler.TempData["Error"]);
        Assert.AreEqual("Matematika", (await _kontekst.Predmet.FindAsync(1))!.Naziv);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task EditFromContent_ModeratorSvogPredmeta_MijenjaNaziv()
    {
        var kontroler = Moderator();

        await kontroler.EditFromContent(1, new Predmet { Id = 1, Naziv = " Inženjerska   matematika " });

        Assert.AreEqual("Inženjerska matematika", (await _kontekst.Predmet.FindAsync(1))!.Naziv);
        Assert.AreEqual("Predmet je ažuriran.", kontroler.TempData["Success"]);
    }

    // ---------- DeleteFromContent ----------

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public async Task DeleteFromContent_NepostojeciPredmet_VracaNotFound()
    {
        Assert.IsInstanceOfType(await Kontroler().DeleteFromContent(99), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public async Task DeleteFromContent_ModeratorTudjegPredmeta_VracaForbid()
    {
        Assert.IsInstanceOfType(await Moderator().DeleteFromContent(2), typeof(ForbidResult));
        Assert.AreEqual(3, await _kontekst.Predmet.CountAsync());
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [Priority(1)]
    [TestCategory("State verifier")]
    public async Task DeleteFromContent_ModeratorSvogPredmeta_BrisePredmetISadrzaj()
    {
        var kontroler = Moderator();

        await kontroler.DeleteFromContent(1);

        Assert.IsNull(await _kontekst.Predmet.FindAsync(1));
        Assert.AreEqual(0, await _kontekst.Oblast.CountAsync(o => o.PredmetId == 1));
        Assert.AreEqual(0, await _kontekst.Pitanje.CountAsync(p => p.PredmetId == 1));
        StringAssert.StartsWith((string?)kontroler.TempData["Success"], "Predmet je obrisan");
    }

    // ---------- Index i zabranjene akcije ----------

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    public void Index_PreusmjeravaNaTabPredmeti()
    {
        Assert.AreEqual("subjects", Rezultat.Kao<RedirectToActionResult>(Kontroler().Index()).RouteValues!["tab"]);
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
    public void CreatePost_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Create(new Predmet()), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public void EditGet_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Edit(1), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public void EditPost_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Edit(1, new Predmet()), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public void DeleteGet_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Delete(1), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Execution tester")]
    public void DeleteConfirmed_VracaForbid() => Assert.IsInstanceOfType(Kontroler().DeleteConfirmed(1), typeof(ForbidResult));
}
