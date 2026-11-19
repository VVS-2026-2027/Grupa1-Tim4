using System.Security.Claims;
using ezZkvi.Controllers;
using ezZkvi.Data;
using ezZkvi.Models;
using ezZkvi.Services;
using ezZkvi.Tests.Infrastruktura;
using ezZkvi.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ezZkvi.Tests.Controllers;

/// <summary>
/// Testovi administracije korisnika (odobravanje, odbijanje, uklanjanje pristupa, promjena uloge),
/// admin dashboarda i slanja email obavijesti.
/// Zamjenski objekti: Mock UserManager, Mock IEmailService, Mock IUrlHelper; baza je InMemory (fake).
/// Autor: Amir Džafić (C)
/// </summary>
[TestClass]
public sealed class AdministratorControllerTests
{
    private const string AdminId = "admin-1";

    private ApplicationDbContext _kontekst = null!;
    private Mock<UserManager<Korisnik>> _userManager = null!;
    private Mock<IEmailService> _emailServis = null!;

    [TestInitialize]
    public void Postavi()
    {
        _kontekst = TestDbFactory.Kreiraj();
        _userManager = IdentityMockovi.UserManager();
        _userManager.SetupGet(m => m.Users).Returns(_kontekst.Users);
        _userManager.Setup(m => m.UpdateAsync(It.IsAny<Korisnik>())).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<Korisnik>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.RemoveFromRolesAsync(It.IsAny<Korisnik>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.DeleteAsync(It.IsAny<Korisnik>())).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.UpdateSecurityStampAsync(It.IsAny<Korisnik>())).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.GetRolesAsync(It.IsAny<Korisnik>())).ReturnsAsync(new List<string>());
        _emailServis = new Mock<IEmailService>();
    }

    [TestCleanup]
    public void Ocisti()
    {
        _kontekst.Dispose();
    }

    private AdministratorController Kontroler()
    {
        var kontroler = KontrolerHelper.SaKorisnikom(
            new AdministratorController(_kontekst, _userManager.Object, _emailServis.Object), AdminId, "Admin");
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("https://ezkvi.test/Account/Login");
        kontroler.Url = url.Object;
        return kontroler;
    }

    private Korisnik KorisnikKojegNalazi(string id, bool emailPotvrdjen = true, bool odobren = false, string? email = "ana@etf.unsa.ba")
    {
        var korisnik = new Korisnik { Id = id, Email = email, UserName = email, EmailConfirmed = emailPotvrdjen, IsApproved = odobren };
        _userManager.Setup(m => m.FindByIdAsync(id)).ReturnsAsync(korisnik);
        return korisnik;
    }

    private void TrenutniAdminJe(Korisnik korisnik)
    {
        _userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(korisnik);
    }

    private static string AkcijaIz(IActionResult rezultat) => Rezultat.Kao<RedirectToActionResult>(rezultat).ActionName!;

    // ---------- Priprema podataka (Arrange) ----------

    private void DodajKorisnikeSaUlogama()
    {
        _kontekst.Users.AddRange(
            new Korisnik { Id = "s1", Email = "s1@etf.ba", EmailConfirmed = true, IsApproved = true, LastActivity = DateTime.UtcNow.AddMinutes(-1) },
            new Korisnik { Id = "m1", Email = "m1@etf.ba", EmailConfirmed = true, IsApproved = true, LastActivity = DateTime.UtcNow.AddHours(-1) },
            new Korisnik { Id = "s2", Email = "s2@etf.ba", EmailConfirmed = true, IsApproved = false, LastActivity = DateTime.UtcNow },
            new Korisnik { Id = "s3", Email = "s3@etf.ba", EmailConfirmed = false });
        TestDbFactory.Snimi(_kontekst);
        _userManager.Setup(m => m.GetRolesAsync(It.Is<Korisnik>(k => k.Id == "s1"))).ReturnsAsync(new List<string> { "Student" });
        _userManager.Setup(m => m.GetRolesAsync(It.Is<Korisnik>(k => k.Id == "m1"))).ReturnsAsync(new List<string> { "Moderator" });
    }

    private void DodajKorisnikePitanjeIFeedback()
    {
        _kontekst.Users.AddRange(
            new Korisnik { Id = "s1", UserName = "ana@etf.ba", EmailConfirmed = true, IsApproved = true },
            new Korisnik { Id = "s2", UserName = "edin@etf.ba", EmailConfirmed = true, IsApproved = false },
            new Korisnik { Id = "s3", UserName = "x", EmailConfirmed = true, IsApproved = false },
            new Korisnik { Id = "s4", UserName = "neaktivan", EmailConfirmed = false });
        _kontekst.Pitanje.Add(new Pitanje { Id = 1, TekstPitanja = "Šta je matrica?", PredmetId = 1, OblastId = 1 });
        _kontekst.Feedback.AddRange(
            new Feedback { ID = 1, Sadrzaj = "Neobrađen feedback", Status = StatusFeedbacka.NA_CEKANJU },
            new Feedback { ID = 2, Sadrzaj = "Obrađen feedback", Status = StatusFeedbacka.ODOBREN });
        TestDbFactory.Snimi(_kontekst);
    }

    private void DodajPredmeteIKvizove()
    {
        _kontekst.Users.Add(new Korisnik { Id = "s1", UserName = "ana@etf.ba", EmailConfirmed = true });
        _kontekst.Predmet.AddRange(new Predmet { Id = 1, Naziv = "Matematika" }, new Predmet { Id = 2, Naziv = "Fizika" });
        _kontekst.KvizSesije.AddRange(
            new KvizSesija { ID = 1, PredmetId = 2, StudentId = "s1", Status = StatusSesije.ZAVRSEN, Procenat = 80, DatumZavrsetka = DateTime.UtcNow.AddMinutes(-3) },
            new KvizSesija { ID = 2, PredmetId = 2, StudentId = "nepoznat", Status = StatusSesije.ISTEKAO, Procenat = 20, DatumZavrsetka = DateTime.UtcNow.AddMinutes(-2) },
            new KvizSesija { ID = 3, PredmetId = null, StudentId = null, Status = StatusSesije.ZAVRSEN, Procenat = 50, DatumZavrsetka = DateTime.UtcNow.AddMinutes(-1) },
            new KvizSesija { ID = 4, PredmetId = 1, StudentId = "s1", Status = StatusSesije.U_TOKU });
        TestDbFactory.Snimi(_kontekst);
    }

    private void StudentiURoliSuDvaOdobrenaNeodobrenIBezEmaila()
    {
        _userManager.Setup(m => m.GetUsersInRoleAsync("Student")).ReturnsAsync(new List<Korisnik>
        {
            new() { Id = "s1", Email = "s1@etf.ba", IsApproved = true },
            new() { Id = "s2", Email = "s2@etf.ba", IsApproved = true },
            new() { Id = "s3", Email = "s3@etf.ba", IsApproved = false },
            new() { Id = "s4", Email = null, IsApproved = true }
        });
    }

    private void AdminJeIModerator()
    {
        var zajednicki = new Korisnik { Id = "a1", Email = "a1@etf.ba", IsApproved = true };
        _userManager.Setup(m => m.GetUsersInRoleAsync("Admin")).ReturnsAsync(new List<Korisnik> { zajednicki });
        _userManager.Setup(m => m.GetUsersInRoleAsync("Moderator")).ReturnsAsync(new List<Korisnik>
        {
            zajednicki,
            new() { Id = "m1", Email = "m1@etf.ba", IsApproved = true }
        });
    }

    // ---------- Users ----------

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("State verifier")]
    public async Task Users_PrikazujeSamoKorisnikeSaPotvrdjenimEmailomIUloge()
    {
        DodajKorisnikeSaUlogama();

        var rezultat = await Kontroler().Users();

        var model = Rezultat.Kao<List<AdminUserViewModel>>(Rezultat.Kao<ViewResult>(rezultat).Model).OrderBy(u => u.Id).ToList();
        CollectionAssert.AreEqual(new[] { "m1", "s1", "s2" }, model.Select(u => u.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "Moderator", "Student", "Nema ulogu" }, model.Select(u => u.Role).ToArray());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Boolean verifier")]
    public async Task Users_AktivnostUZadnjihPetMinuta_OznacavaOdobreneKaoAktivne()
    {
        _kontekst.Users.AddRange(
            new Korisnik { Id = "a", EmailConfirmed = true, IsApproved = true, LastActivity = DateTime.UtcNow.AddMinutes(-1) },
            new Korisnik { Id = "b", EmailConfirmed = true, IsApproved = true, LastActivity = DateTime.UtcNow.AddMinutes(-10) },
            new Korisnik { Id = "c", EmailConfirmed = true, IsApproved = false, LastActivity = DateTime.UtcNow },
            new Korisnik { Id = "d", EmailConfirmed = true, IsApproved = true, LastActivity = null });
        TestDbFactory.Snimi(_kontekst);

        var rezultat = await Kontroler().Users();

        var model = Rezultat.Kao<List<AdminUserViewModel>>(Rezultat.Kao<ViewResult>(rezultat).Model).OrderBy(u => u.Id).ToList();
        CollectionAssert.AreEqual(new[] { true, false, false, false }, model.Select(u => u.IsActive).ToArray());
    }

    // ---------- ApproveUser ----------

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Null verifier")]
    public async Task ApproveUser_NepostojeciKorisnik_VracaNotFound()
    {
        _userManager.Setup(m => m.FindByIdAsync("x")).ReturnsAsync((Korisnik?)null);

        Assert.IsInstanceOfType(await Kontroler().ApproveUser("x"), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Internal call verifier")]
    public async Task ApproveUser_EmailNijePotvrdjen_NeOdobravaKorisnika()
    {
        var korisnik = KorisnikKojegNalazi("u1", emailPotvrdjen: false);
        var kontroler = Kontroler();

        await kontroler.ApproveUser("u1");

        Assert.IsFalse(korisnik.IsApproved);
        Assert.AreEqual("Korisnik ne može biti odobren dok ne potvrdi email adresu.", kontroler.TempData["Error"]);
        _userManager.Verify(m => m.UpdateAsync(It.IsAny<Korisnik>()), Times.Never());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task ApproveUser_AzuriranjeNeuspjesno_VracaGresku()
    {
        KorisnikKojegNalazi("u1");
        _userManager.Setup(m => m.UpdateAsync(It.IsAny<Korisnik>())).ReturnsAsync(IdentityMockovi.IdentityRezultat(false));
        var kontroler = Kontroler();

        await kontroler.ApproveUser("u1");

        Assert.AreEqual("Greška prilikom odobravanja korisnika.", kontroler.TempData["Error"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [Priority(1)]
    [TestCategory("Internal call verifier")]
    public async Task ApproveUser_KorisnikBezUlogeStudent_DodajeUloguISaljeEmail()
    {
        var korisnik = KorisnikKojegNalazi("u1");
        var kontroler = Kontroler();

        var rezultat = await kontroler.ApproveUser("u1");

        Assert.AreEqual(nameof(AdministratorController.Users), AkcijaIz(rezultat));
        Assert.IsTrue(korisnik.IsApproved);
        Assert.AreEqual("Korisnik ana@etf.unsa.ba je odobren i poslan mu je email.", kontroler.TempData["Message"]);
        _userManager.Verify(m => m.AddToRoleAsync(korisnik, "Student"), Times.Once());
        _emailServis.Verify(e => e.SendEmailAsync("ana@etf.unsa.ba", "Vaš eZkvi nalog je odobren", It.IsAny<string>()), Times.Once());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Internal call verifier")]
    public async Task ApproveUser_KorisnikVecStudent_NeDodajeUloguPonovo()
    {
        var korisnik = KorisnikKojegNalazi("u1");
        _userManager.Setup(m => m.IsInRoleAsync(korisnik, "Student")).ReturnsAsync(true);

        await Kontroler().ApproveUser("u1");

        _userManager.Verify(m => m.AddToRoleAsync(It.IsAny<Korisnik>(), It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task ApproveUser_DodjelaUlogeNeuspjesna_VracaGresku()
    {
        KorisnikKojegNalazi("u1");
        _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<Korisnik>(), "Student")).ReturnsAsync(IdentityMockovi.IdentityRezultat(false));
        var kontroler = Kontroler();

        await kontroler.ApproveUser("u1");

        Assert.AreEqual("Korisnik je odobren, ali uloga nije dodijeljena.", kontroler.TempData["Error"]);
        _emailServis.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Exception verifier")]
    public async Task ApproveUser_SlanjeEmailaNeuspjesno_KorisnikJeIpakOdobren()
    {
        var korisnik = KorisnikKojegNalazi("u1");
        _emailServis.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("SMTP greška"));
        var kontroler = Kontroler();

        await kontroler.ApproveUser("u1");

        Assert.IsTrue(korisnik.IsApproved);
        Assert.AreEqual("Korisnik ana@etf.unsa.ba je odobren, ali email obavijest nije poslana.", kontroler.TempData["Message"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Null verifier")]
    public async Task ApproveUser_KorisnikBezEmaila_OdobravaBezSlanjaEmaila()
    {
        KorisnikKojegNalazi("u1", email: null);
        var kontroler = Kontroler();

        await kontroler.ApproveUser("u1");

        Assert.AreEqual("Korisnik je odobren, ali nema registrovanu email adresu.", kontroler.TempData["Message"]);
        _emailServis.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
    }

    // ---------- RejectUser ----------

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Null verifier")]
    public async Task RejectUser_NepostojeciKorisnik_VracaNotFound()
    {
        _userManager.Setup(m => m.FindByIdAsync("x")).ReturnsAsync((Korisnik?)null);

        Assert.IsInstanceOfType(await Kontroler().RejectUser("x"), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Internal call verifier")]
    public async Task RejectUser_VecOdobrenKorisnik_NeBriseKorisnika()
    {
        KorisnikKojegNalazi("u1", odobren: true);

        var rezultat = await Kontroler().RejectUser("u1");

        Assert.AreEqual(nameof(AdministratorController.Users), AkcijaIz(rezultat));
        _userManager.Verify(m => m.DeleteAsync(It.IsAny<Korisnik>()), Times.Never());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("State verifier")]
    public async Task RejectUser_NeodobrenKorisnik_BriseKorisnikaINjegovSadrzaj()
    {
        var korisnik = KorisnikKojegNalazi("u1");
        _kontekst.Feedback.Add(new Feedback { ID = 1, Sadrzaj = "Prijava greške u pitanju", KorisnikId = "u1" });
        _kontekst.StudentStatistike.Add(new StudentStatistika { Id = 1, KorisnikId = "u1", PredmetId = 1 });
        TestDbFactory.Snimi(_kontekst);

        await Kontroler().RejectUser("u1");

        Assert.AreEqual(0, await _kontekst.Feedback.CountAsync());
        Assert.AreEqual(0, await _kontekst.StudentStatistike.CountAsync());
        _userManager.Verify(m => m.DeleteAsync(korisnik), Times.Once());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task RejectUser_BrisanjeNeuspjesno_VracaGresku()
    {
        KorisnikKojegNalazi("u1");
        _userManager.Setup(m => m.DeleteAsync(It.IsAny<Korisnik>())).ReturnsAsync(IdentityMockovi.IdentityRezultat(false));
        var kontroler = Kontroler();

        await kontroler.RejectUser("u1");

        Assert.AreEqual("Greška prilikom brisanja korisnika.", kontroler.TempData["Error"]);
    }

    // ---------- RemoveAccess ----------

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Null verifier")]
    public async Task RemoveAccess_NepostojeciKorisnik_VracaNotFound()
    {
        _userManager.Setup(m => m.FindByIdAsync("x")).ReturnsAsync((Korisnik?)null);

        Assert.IsInstanceOfType(await Kontroler().RemoveAccess("x"), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task RemoveAccess_AdminSamomSebi_VracaGresku()
    {
        TrenutniAdminJe(KorisnikKojegNalazi(AdminId, odobren: true));
        var kontroler = Kontroler();

        await kontroler.RemoveAccess(AdminId);

        Assert.AreEqual("Ne možete ukloniti pristup sami sebi.", kontroler.TempData["Error"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task RemoveAccess_DrugomAdministratoru_VracaGresku()
    {
        var drugiAdmin = KorisnikKojegNalazi("admin-2", odobren: true);
        _userManager.Setup(m => m.IsInRoleAsync(drugiAdmin, "Admin")).ReturnsAsync(true);
        var kontroler = Kontroler();

        await kontroler.RemoveAccess("admin-2");

        Assert.AreEqual("Ne možete ukloniti pristup administratoru.", kontroler.TempData["Error"]);
        Assert.IsTrue(drugiAdmin.IsApproved);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task RemoveAccess_AzuriranjeNeuspjesno_VracaGresku()
    {
        KorisnikKojegNalazi("u1", odobren: true);
        _userManager.Setup(m => m.UpdateAsync(It.IsAny<Korisnik>())).ReturnsAsync(IdentityMockovi.IdentityRezultat(false));
        var kontroler = Kontroler();

        await kontroler.RemoveAccess("u1");

        Assert.AreEqual("Greška prilikom uklanjanja pristupa.", kontroler.TempData["Error"]);
        _userManager.Verify(m => m.UpdateSecurityStampAsync(It.IsAny<Korisnik>()), Times.Never());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Internal call verifier")]
    public async Task RemoveAccess_ObicanKorisnik_UklanjaOdobrenjeIPonistavaSesije()
    {
        var korisnik = KorisnikKojegNalazi("u1", odobren: true);
        var kontroler = Kontroler();

        await kontroler.RemoveAccess("u1");

        Assert.IsFalse(korisnik.IsApproved);
        Assert.AreEqual("Korisniku je uklonjen pristup.", kontroler.TempData["Message"]);
        _userManager.Verify(m => m.UpdateSecurityStampAsync(korisnik), Times.Once());
    }

    // ---------- ChangeRole ----------

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Boundary verifier")]
    [DataRow("Superadmin")]
    [DataRow("admin")]
    [DataRow("")]
    public async Task ChangeRole_NedozvoljenaUloga_VracaGreskuBezTrazenjaKorisnika(string uloga)
    {
        var kontroler = Kontroler();

        await kontroler.ChangeRole("u1", uloga);

        Assert.AreEqual("Neispravna uloga.", kontroler.TempData["Error"]);
        _userManager.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Null verifier")]
    public async Task ChangeRole_NepostojeciKorisnik_VracaNotFound()
    {
        _userManager.Setup(m => m.FindByIdAsync("x")).ReturnsAsync((Korisnik?)null);

        Assert.IsInstanceOfType(await Kontroler().ChangeRole("x", "Moderator"), typeof(NotFoundResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task ChangeRole_EmailNijePotvrdjen_VracaGresku()
    {
        KorisnikKojegNalazi("u1", emailPotvrdjen: false);
        var kontroler = Kontroler();

        await kontroler.ChangeRole("u1", "Moderator");

        Assert.AreEqual("Korisniku se ne može mijenjati uloga dok ne potvrdi email adresu.", kontroler.TempData["Error"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task ChangeRole_VlastitaUloga_VracaGresku()
    {
        TrenutniAdminJe(KorisnikKojegNalazi(AdminId, odobren: true));
        var kontroler = Kontroler();

        await kontroler.ChangeRole(AdminId, "Student");

        Assert.AreEqual("Ne možete mijenjati vlastitu ulogu.", kontroler.TempData["Error"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Internal call verifier")]
    public async Task ChangeRole_UklanjanjeStareUlogeNeuspjesno_NeDodajeNovu()
    {
        var korisnik = KorisnikKojegNalazi("u1");
        _userManager.Setup(m => m.GetRolesAsync(korisnik)).ReturnsAsync(new List<string> { "Student" });
        _userManager.Setup(m => m.RemoveFromRolesAsync(korisnik, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityMockovi.IdentityRezultat(false));
        var kontroler = Kontroler();

        await kontroler.ChangeRole("u1", "Moderator");

        StringAssert.EndsWith((string?)kontroler.TempData["Error"], "ka prilikom uklanjanja stare uloge.");
        _userManager.Verify(m => m.AddToRoleAsync(It.IsAny<Korisnik>(), It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task ChangeRole_DodavanjeNoveUlogeNeuspjesno_VracaGresku()
    {
        KorisnikKojegNalazi("u1");
        _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<Korisnik>(), "Moderator")).ReturnsAsync(IdentityMockovi.IdentityRezultat(false));
        var kontroler = Kontroler();

        await kontroler.ChangeRole("u1", "Moderator");

        Assert.AreEqual("Greška prilikom dodjeljivanja nove uloge.", kontroler.TempData["Error"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [Priority(1)]
    [TestCategory("Internal call verifier")]
    public async Task ChangeRole_StudentUModeratora_ZamjenjujeUloguIOdobravaKorisnika()
    {
        var korisnik = KorisnikKojegNalazi("u1");
        _userManager.Setup(m => m.GetRolesAsync(korisnik)).ReturnsAsync(new List<string> { "Student" });
        var kontroler = Kontroler();

        await kontroler.ChangeRole("u1", "Moderator");

        Assert.IsTrue(korisnik.IsApproved);
        Assert.AreEqual("Korisniku ana@etf.unsa.ba dodijeljena je uloga Moderator.", kontroler.TempData["Message"]);
        _userManager.Verify(m => m.RemoveFromRolesAsync(korisnik, It.Is<IEnumerable<string>>(u => u.Single() == "Student")), Times.Once());
        _userManager.Verify(m => m.AddToRoleAsync(korisnik, "Moderator"), Times.Once());
        _userManager.Verify(m => m.UpdateSecurityStampAsync(korisnik), Times.Once());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Internal call verifier")]
    public async Task ChangeRole_KorisnikBezUloga_NePokusavaUklonitiUloge()
    {
        KorisnikKojegNalazi("u1");

        await Kontroler().ChangeRole("u1", "Student");

        _userManager.Verify(m => m.RemoveFromRolesAsync(It.IsAny<Korisnik>(), It.IsAny<IEnumerable<string>>()), Times.Never());
    }

    // ---------- Dashboard ----------

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("State verifier")]
    public async Task Dashboard_PopunjenaBaza_RacunaBrojace()
    {
        DodajKorisnikePitanjeIFeedback();
        var kontroler = Kontroler();

        await kontroler.Dashboard();

        Assert.AreEqual(3, kontroler.ViewData["BrojKorisnika"]);
        Assert.AreEqual(1, kontroler.ViewData["BrojPitanja"]);
        Assert.AreEqual(1, kontroler.ViewData["NeobradeniFeedback"]);
        Assert.AreEqual(2, kontroler.ViewData["BrojNaCekanju"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task Dashboard_KorisniciNaCekanju_PrikazujeImeIzEmailaIInicijale()
    {
        DodajKorisnikePitanjeIFeedback();
        var kontroler = Kontroler();

        await kontroler.Dashboard();

        var naCekanju = Rezultat.Kao<List<KorisnikNaCekanjuItem>>(kontroler.ViewData["NaCekanju"]).OrderBy(k => k.Id).ToList();
        Assert.AreEqual("edin", naCekanju[0].Ime);
        Assert.AreEqual("ED", naCekanju[0].Inicijali);
        Assert.AreEqual("X", naCekanju[1].Inicijali);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task Dashboard_Kvizovi_SortiraPredmetePoBrojuKvizovaIPrikazujeNedavne()
    {
        DodajPredmeteIKvizove();
        var kontroler = Kontroler();

        await kontroler.Dashboard();

        var predmeti = Rezultat.Kao<List<PredmetAktivnostItem>>(kontroler.ViewData["NajaktivnijiPredmeti"]);
        var nedavni = Rezultat.Kao<List<KvizAktivnostItem>>(kontroler.ViewData["NedavniKvizovi"]);
        Assert.AreEqual(4, kontroler.ViewData["BrojKvizova"]);
        CollectionAssert.AreEqual(new[] { "Fizika", "Matematika" }, predmeti.Select(p => p.Naziv).ToArray());
        CollectionAssert.AreEqual(new[] { 2, 1 }, predmeti.Select(p => p.BrojKvizova).ToArray());
        CollectionAssert.AreEqual(new[] { "Nepoznat", "Nepoznat", "ana" }, nedavni.Select(n => n.Korisnik).ToArray());
        CollectionAssert.AreEqual(new[] { "—", "Fizika", "Fizika" }, nedavni.Select(n => n.Predmet).ToArray());
    }

    // ---------- EmailObavijest ----------

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("State verifier")]
    public void EmailObavijestGet_VracaPrazanModelSaStudentimaKaoPrimaocima()
    {
        var model = Rezultat.Kao<AdminEmailObavijestViewModel>(Rezultat.Kao<ViewResult>(Kontroler().EmailObavijest()).Model);

        Assert.AreEqual("Studenti", model.Primaoci);
        Assert.AreEqual(string.Empty, model.Naslov);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Internal call verifier")]
    public async Task EmailObavijestPost_NevalidanModel_VracaFormuBezSlanja()
    {
        var kontroler = Kontroler();
        kontroler.ModelState.AddModelError("Naslov", "Naslov je obavezan.");
        var model = new AdminEmailObavijestViewModel { Naslov = "", Poruka = "Tekst" };

        var rezultat = await kontroler.EmailObavijest(model);

        Assert.AreSame(model, Rezultat.Kao<ViewResult>(rezultat).Model);
        _emailServis.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Internal call verifier")]
    public async Task EmailObavijestPost_Studenti_SaljeSamoOdobrenimStudentimaSaEmailom()
    {
        StudentiURoliSuDvaOdobrenaNeodobrenIBezEmaila();
        var kontroler = Kontroler();

        await kontroler.EmailObavijest(new AdminEmailObavijestViewModel { Primaoci = "Studenti", Naslov = "Ispit", Poruka = "Ispit je u petak." });

        Assert.AreEqual("Email obavijest je poslana studentima. Broj primalaca: 2", kontroler.TempData["Message"]);
        _emailServis.Verify(e => e.SendEmailAsync(It.IsAny<string>(), "Ispit", "Ispit je u petak."), Times.Exactly(2));
        _emailServis.Verify(e => e.SendEmailAsync("s3@etf.ba", It.IsAny<string>(), It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Internal call verifier")]
    public async Task EmailObavijestPost_AdminiModeratori_KorisnikUObjeUlogeDobijaJedanEmail()
    {
        AdminJeIModerator();
        var kontroler = Kontroler();

        await kontroler.EmailObavijest(new AdminEmailObavijestViewModel { Primaoci = "AdminiModeratori", Naslov = "Sastanak", Poruka = "Sastanak u 10h." });

        Assert.AreEqual("Email obavijest je poslana adminima i moderatorima. Broj primalaca: 2", kontroler.TempData["Message"]);
        _emailServis.Verify(e => e.SendEmailAsync("a1@etf.ba", It.IsAny<string>(), It.IsAny<string>()), Times.Once());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task EmailObavijestPost_NepoznataGrupa_VracaGresku()
    {
        var kontroler = Kontroler();

        var rezultat = await kontroler.EmailObavijest(new AdminEmailObavijestViewModel { Primaoci = "Svi", Naslov = "N", Poruka = "P" });

        Assert.AreEqual(nameof(AdministratorController.EmailObavijest), AkcijaIz(rezultat));
        Assert.AreEqual("Odabrana grupa primalaca nije validna.", kontroler.TempData["Error"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Empty tester")]
    public async Task EmailObavijestPost_NemaPrimalaca_VracaGresku()
    {
        _userManager.Setup(m => m.GetUsersInRoleAsync("Student")).ReturnsAsync(new List<Korisnik>());
        var kontroler = Kontroler();

        await kontroler.EmailObavijest(new AdminEmailObavijestViewModel { Primaoci = "Studenti", Naslov = "N", Poruka = "P" });

        Assert.AreEqual("Nema korisnika za odabranu grupu primalaca.", kontroler.TempData["Error"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Exception verifier")]
    public async Task EmailObavijestPost_SmtpNijeKonfigurisan_PrikazujePorukuIzuzetka()
    {
        _userManager.Setup(m => m.GetUsersInRoleAsync("Student"))
            .ReturnsAsync(new List<Korisnik> { new() { Id = "s1", Email = "s1@etf.ba", IsApproved = true } });
        _emailServis.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Email konfiguracija nije ispravno podešena."));
        var kontroler = Kontroler();

        await kontroler.EmailObavijest(new AdminEmailObavijestViewModel { Primaoci = "Studenti", Naslov = "N", Poruka = "P" });

        Assert.AreEqual("Email konfiguracija nije ispravno podešena.", kontroler.TempData["Error"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Exception verifier")]
    public async Task EmailObavijestPost_NeocekivanIzuzetak_PrikazujeOpcuPoruku()
    {
        _userManager.Setup(m => m.GetUsersInRoleAsync("Student"))
            .ReturnsAsync(new List<Korisnik> { new() { Id = "s1", Email = "s1@etf.ba", IsApproved = true } });
        _emailServis.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new TimeoutException());
        var kontroler = Kontroler();

        await kontroler.EmailObavijest(new AdminEmailObavijestViewModel { Primaoci = "Studenti", Naslov = "N", Poruka = "P" });

        Assert.AreEqual("Došlo je do greške prilikom slanja email obavijesti.", kontroler.TempData["Error"]);
    }

    /// <summary>Privatna metoda se nigdje u aplikaciji ne poziva, pa se testira direktno (Predavanje 6: testiranje privatne metode).</summary>
    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Boolean verifier")]
    public void AdministratorExists_PostojeciAdministrator_VracaTrue()
    {
        _kontekst.Administrator.Add(new Administrator { Id = "admin-2", UserName = "admin2" });
        TestDbFactory.Snimi(_kontekst);

        Assert.IsTrue(PrivatnaMetoda.Pozovi<bool>(Kontroler(), "AdministratorExists", "admin-2"));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Boolean verifier")]
    public void AdministratorExists_NepostojeciAdministrator_VracaFalse()
    {
        Assert.IsFalse(PrivatnaMetoda.Pozovi<bool>(Kontroler(), "AdministratorExists", "nema"));
    }

    // ---------- Index i zabranjene akcije ----------

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public void Index_PreusmjeravaNaDashboard()
    {
        Assert.AreEqual(nameof(AdministratorController.Dashboard), AkcijaIz(Kontroler().Index()));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public void Details_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Details("x"), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public void CreateGet_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Create(), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public void CreatePost_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Create(new Administrator()), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public void EditGet_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Edit("x"), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public void EditPost_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Edit("x", new Administrator()), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public void DeleteGet_VracaForbid() => Assert.IsInstanceOfType(Kontroler().Delete("x"), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public void DeleteConfirmed_VracaForbid() => Assert.IsInstanceOfType(Kontroler().DeleteConfirmed("x"), typeof(ForbidResult));
}
