using System.Diagnostics;
using ezZkvi.Controllers;
using ezZkvi.Data;
using ezZkvi.Models;
using ezZkvi.Tests.Infrastruktura;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ezZkvi.Tests.Controllers;

/// <summary>
/// Testovi početne stranice i stranica grešaka.
/// Zamjenski objekti: Mock&lt;ILogger&gt; (dummy - samo zadovoljava konstruktor) i
/// Mock&lt;HttpContext&gt; (mock sa provjerom poziva) u testu akcije Error.
/// Autor: Dino Tahirović (A)
/// </summary>
[TestClass]
public sealed class HomeControllerTests
{
    private ApplicationDbContext _kontekst = null!;
    private Mock<ILogger<HomeController>> _logger = null!;

    [TestInitialize]
    public void Postavi()
    {
        _kontekst = TestDbFactory.Kreiraj();
        _logger = new Mock<ILogger<HomeController>>();
    }

    [TestCleanup]
    public void Ocisti()
    {
        _kontekst.Dispose();
    }

    /// <summary>2 korisnika, 2 predmeta, 1 pitanje i 1 kviz sesija.</summary>
    private void PopuniBazu()
    {
        _kontekst.Users.AddRange(new Korisnik { Id = "u1" }, new Korisnik { Id = "u2" });
        _kontekst.Predmet.AddRange(new Predmet { Id = 1, Naziv = "Matematika" }, new Predmet { Id = 2, Naziv = "Fizika" });
        _kontekst.Pitanje.Add(new Pitanje { Id = 1, TekstPitanja = "Koliko je 2+2?", PredmetId = 1, OblastId = 1 });
        _kontekst.KvizSesije.Add(new KvizSesija { ID = 1, TraziBrojPitanja = 5, VremenskoOgranicenje = 3 });
        TestDbFactory.Snimi(_kontekst);
    }

    /// <summary>Kontroler čiji je HttpContext mock koji vraća zadani TraceIdentifier.</summary>
    private HomeController KontrolerSaMockHttpContextom(Mock<HttpContext> httpContext)
    {
        httpContext.SetupGet(h => h.TraceIdentifier).Returns("trace-123");
        return new HomeController(_logger.Object, _kontekst)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext.Object }
        };
    }

    private HomeController Kontroler()
    {
        return KontrolerHelper.Anonimno(new HomeController(_logger.Object, _kontekst));
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("State verifier")]
    public async Task Index_PraznaBaza_SviBrojaciSuNula()
    {
        var kontroler = Kontroler();

        await kontroler.Index();

        Assert.AreEqual(0, kontroler.ViewData["BrojKorisnika"]);
        Assert.AreEqual(0, kontroler.ViewData["BrojPitanja"]);
        Assert.AreEqual(0, kontroler.ViewData["BrojKvizova"]);
        Assert.AreEqual(0, kontroler.ViewData["BrojPredmeta"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("State verifier")]
    public async Task Index_PopunjenaBaza_PrikazujeBrojKorisnikaPitanjaKvizovaIPredmeta()
    {
        PopuniBazu();
        var kontroler = Kontroler();

        var rezultat = await kontroler.Index();

        Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
        Assert.AreEqual(2, kontroler.ViewData["BrojKorisnika"]);
        Assert.AreEqual(1, kontroler.ViewData["BrojPitanja"]);
        Assert.AreEqual(1, kontroler.ViewData["BrojKvizova"]);
        Assert.AreEqual(2, kontroler.ViewData["BrojPredmeta"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void Privacy_VracaView()
    {
        Assert.IsInstanceOfType(Kontroler().Privacy(), typeof(ViewResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Internal call verifier")]
    public void Error_BezAktivnosti_KoristiTraceIdentifierIzHttpContexta()
    {
        Activity.Current = null;
        var httpContext = new Mock<HttpContext>();
        var kontroler = KontrolerSaMockHttpContextom(httpContext);

        var model = Rezultat.Kao<ErrorViewModel>(Rezultat.Kao<ViewResult>(kontroler.Error()).Model);

        Assert.AreEqual("trace-123", model.RequestId);
        Assert.IsTrue(model.ShowRequestId);
        httpContext.VerifyGet(h => h.TraceIdentifier, Times.Once());
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    [DataRow(404)]
    [DataRow(500)]
    public void Greska_SaKodom_ProsljedjujeKodPogledu(int kod)
    {
        var kontroler = Kontroler();

        kontroler.Greska(kod);

        Assert.AreEqual(kod, kontroler.ViewData["Kod"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Null verifier")]
    public void Greska_BezKoda_KodJeNull()
    {
        var kontroler = Kontroler();

        var rezultat = kontroler.Greska(null);

        Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
        Assert.IsNull(kontroler.ViewData["Kod"]);
    }
}
