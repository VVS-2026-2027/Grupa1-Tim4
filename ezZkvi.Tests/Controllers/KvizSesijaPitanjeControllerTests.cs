using ezZkvi.Controllers;
using ezZkvi.Data;
using ezZkvi.Models;
using ezZkvi.Tests.Infrastruktura;
using Microsoft.AspNetCore.Mvc;

namespace ezZkvi.Tests.Controllers;

/// <summary>
/// Testovi administratorskog pregleda stavki kviz sesija (samo čitanje; izmjene su zabranjene).
/// Autor: Dino Tahirović (A)
/// </summary>
[TestClass]
public sealed class KvizSesijaPitanjeControllerTests
{
    private ApplicationDbContext _kontekst = null!;
    private KvizSesijaPitanjeController _kontroler = null!;

    [TestInitialize]
    public void Postavi()
    {
        _kontekst = TestDbFactory.Kreiraj();
        _kontekst.KvizSesijaPitanja.AddRange(
            new KvizSesijaPitanje { ID = 1, KvizSesijaId = 1, PitanjeId = 1, RedniBroj = 1, Tacno = 1, BrojBodova = 0.1 },
            new KvizSesijaPitanje { ID = 2, KvizSesijaId = 1, PitanjeId = 2, RedniBroj = 2 });
        TestDbFactory.Snimi(_kontekst);
        _kontroler = KontrolerHelper.SaKorisnikom(new KvizSesijaPitanjeController(_kontekst), "admin-1", "Admin");
    }

    [TestCleanup]
    public void Ocisti()
    {
        _kontekst.Dispose();
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("State verifier")]
    public async Task Index_VracaSveStavke()
    {
        var rezultat = await _kontroler.Index();

        Assert.AreEqual(2, Rezultat.Kao<List<KvizSesijaPitanje>>(Rezultat.Kao<ViewResult>(rezultat).Model).Count);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Null verifier")]
    public async Task Details_BezId_VracaNotFound()
    {
        Assert.IsInstanceOfType(await _kontroler.Details(null), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task Details_NepostojeciId_VracaNotFound()
    {
        Assert.IsInstanceOfType(await _kontroler.Details(99), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public async Task Details_PostojeciId_VracaTrazenuStavku()
    {
        var rezultat = await _kontroler.Details(1);

        var stavka = Rezultat.Kao<KvizSesijaPitanje>(Rezultat.Kao<ViewResult>(rezultat).Model);
        Assert.AreEqual(1, stavka.PitanjeId);
        Assert.AreEqual(0.1, stavka.BrojBodova, 0.0001);
    }

    /// <summary>Privatna metoda se nigdje u aplikaciji ne poziva, pa se testira direktno (Predavanje 6: testiranje privatne metode).</summary>
    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Boolean verifier")]
    [DataRow(1, true)]
    [DataRow(99, false)]
    public void KvizSesijaPitanjeExists_PrivatnaMetoda_VracaDaLiZapisPostoji(int id, bool ocekivano)
    {
        Assert.AreEqual(ocekivano, PrivatnaMetoda.Pozovi<bool>(_kontroler, "KvizSesijaPitanjeExists", id));
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void CreateGet_VracaForbid() => Assert.IsInstanceOfType(_kontroler.Create(), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void CreatePost_VracaForbid() => Assert.IsInstanceOfType(_kontroler.Create(new KvizSesijaPitanje()), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void EditGet_VracaForbid() => Assert.IsInstanceOfType(_kontroler.Edit(1), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void EditPost_VracaForbid() => Assert.IsInstanceOfType(_kontroler.Edit(1, new KvizSesijaPitanje()), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void DeleteGet_VracaForbid() => Assert.IsInstanceOfType(_kontroler.Delete(1), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void DeleteConfirmed_VracaForbid() => Assert.IsInstanceOfType(_kontroler.DeleteConfirmed(1), typeof(ForbidResult));
}
