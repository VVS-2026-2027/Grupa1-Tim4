using ezZkvi.Controllers;
using ezZkvi.Data;
using ezZkvi.Models;
using ezZkvi.Tests.Infrastruktura;
using ezZkvi.ViewModels;
using Microsoft.AspNetCore.Mvc;
using static ezZkvi.Tests.Controllers.StudentTestSeed;

namespace ezZkvi.Tests.Controllers;

/// <summary>
/// TDD testovi prikaza novih funkcionalnosti (niz dana učenja i preporuke oblasti) na studentskom dashboardu.
/// </summary>
[TestClass]
public sealed class StudentControllerNizIPreporukeTests
{
    private const int MinutaUDanu = 24 * 60;

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

    private async Task<DashboardViewModel> Dashboard()
    {
        var kontroler = KontrolerHelper.SaKorisnikom(new StudentController(_kontekst), StudentId, "Student");
        var rezultat = await kontroler.Dashboard();
        return Rezultat.Kao<DashboardViewModel>(Rezultat.Kao<ViewResult>(rezultat).Model);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("State verifier")]
    public async Task Dashboard_KvizoviDanasIJucer_PrikazujeTrenutniINajduziNiz()
    {
        DodajSesiju(_kontekst, status: StatusSesije.ZAVRSEN);
        DodajSesiju(_kontekst, status: StatusSesije.ZAVRSEN, minutaOdZavrsetka: MinutaUDanu);

        var model = await Dashboard();

        Assert.AreEqual(2, model.TrenutniNizDana);
        Assert.AreEqual(2, model.NajduziNizDana);
    }
}
