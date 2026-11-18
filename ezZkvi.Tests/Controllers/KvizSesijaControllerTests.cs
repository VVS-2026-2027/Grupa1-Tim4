using ezZkvi.Controllers;
using ezZkvi.Data;
using ezZkvi.Models;
using ezZkvi.Tests.Infrastruktura;
using Microsoft.AspNetCore.Mvc;

namespace ezZkvi.Tests.Controllers;

/// <summary>
/// Testovi administratorskog pregleda kviz sesija (samo čitanje; izmjene su zabranjene).
/// Pošto nijedan test ne mijenja podatke, baza se pravi jednom za cijelu klasu ([ClassInitialize])
/// i zatvara nakon zadnjeg testa ([ClassCleanup]); kontroler se i dalje pravi novi za svaki test.
/// Autor: Dino Tahirović (A)
/// </summary>
[TestClass]
public sealed class KvizSesijaControllerTests
{
    private static ApplicationDbContext _kontekst = null!;
    private KvizSesijaController _kontroler = null!;

    [ClassInitialize]
    public static void PripremiBazu(TestContext _)
    {
        _kontekst = TestDbFactory.Kreiraj();
        _kontekst.KvizSesije.AddRange(
            new KvizSesija { ID = 1, TraziBrojPitanja = 5, VremenskoOgranicenje = 3, Status = StatusSesije.ZAVRSEN },
            new KvizSesija { ID = 2, TraziBrojPitanja = 5, VremenskoOgranicenje = 3, Status = StatusSesije.U_TOKU });
        TestDbFactory.Snimi(_kontekst);
    }

    [ClassCleanup]
    public static void ZatvoriBazu()
    {
        _kontekst.Dispose();
    }

    [TestInitialize]
    public void Postavi()
    {
        _kontroler = KontrolerHelper.SaKorisnikom(new KvizSesijaController(_kontekst), "admin-1", "Admin");
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("State verifier")]
    public async Task Index_VracaSveSesije()
    {
        var rezultat = await _kontroler.Index();

        Assert.AreEqual(2, Rezultat.Kao<List<KvizSesija>>(Rezultat.Kao<ViewResult>(rezultat).Model).Count);
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
    public async Task Details_PostojeciId_VracaTrazenuSesiju()
    {
        var rezultat = await _kontroler.Details(2);

        var sesija = Rezultat.Kao<KvizSesija>(Rezultat.Kao<ViewResult>(rezultat).Model);
        Assert.AreEqual(2, sesija.ID);
        Assert.AreEqual(StatusSesije.U_TOKU, sesija.Status);
    }

    /// <summary>Privatna metoda se nigdje u aplikaciji ne poziva, pa se testira direktno (Predavanje 6: testiranje privatne metode).</summary>
    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Boolean verifier")]
    [DataRow(1, true)]
    [DataRow(99, false)]
    public void KvizSesijaExists_PrivatnaMetoda_VracaDaLiZapisPostoji(int id, bool ocekivano)
    {
        Assert.AreEqual(ocekivano, PrivatnaMetoda.Pozovi<bool>(_kontroler, "KvizSesijaExists", id));
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void CreateGet_VracaForbid() => Assert.IsInstanceOfType(_kontroler.Create(), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void CreatePost_VracaForbid() => Assert.IsInstanceOfType(_kontroler.Create(new KvizSesija()), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void EditGet_VracaForbid() => Assert.IsInstanceOfType(_kontroler.Edit(1), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void EditPost_VracaForbid() => Assert.IsInstanceOfType(_kontroler.Edit(1, new KvizSesija()), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void DeleteGet_VracaForbid() => Assert.IsInstanceOfType(_kontroler.Delete(1), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void DeleteConfirmed_VracaForbid() => Assert.IsInstanceOfType(_kontroler.DeleteConfirmed(1), typeof(ForbidResult));
}
