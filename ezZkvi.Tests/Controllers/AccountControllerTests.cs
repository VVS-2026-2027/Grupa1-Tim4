using System.Text;
using ezZkvi.Controllers;
using ezZkvi.Models;
using ezZkvi.Services;
using ezZkvi.Tests.Infrastruktura;
using ezZkvi.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Moq;

namespace ezZkvi.Tests.Controllers;

/// <summary>
/// Testovi prijave, registracije, potvrde emaila i odjave.
/// Zamjenski objekti (Mock): UserManager, SignInManager, IEmailService i IUrlHelper -
/// testovi ne trebaju bazu korisnika, SMTP server ni rutiranje.
/// Autor: Amir Džafić (C)
/// </summary>
[TestClass]
public sealed class AccountControllerTests
{
    private const string Email = "ana@etf.unsa.ba";
    private const string Lozinka = "Lozinka1!";
    private const string VerifikacijskiLink = "https://ezkvi.test/Account/ConfirmEmail?userId=u1";

    private Mock<UserManager<Korisnik>> _userManager = null!;
    private Mock<SignInManager<Korisnik>> _signInManager = null!;
    private Mock<IEmailService> _emailServis = null!;
    private Mock<IUrlHelper> _url = null!;

    [TestInitialize]
    public void Postavi()
    {
        _userManager = IdentityMockovi.UserManager();
        _signInManager = IdentityMockovi.SignInManager(_userManager);
        _emailServis = new Mock<IEmailService>();
        _url = new Mock<IUrlHelper>();
        _url.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns(VerifikacijskiLink);
        _userManager.Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<Korisnik>())).ReturnsAsync("token-123");
    }

    private AccountController Kontroler()
    {
        var kontroler = KontrolerHelper.Anonimno(new AccountController(_signInManager.Object, _userManager.Object, _emailServis.Object));
        kontroler.Url = _url.Object;
        return kontroler;
    }

    private AccountController PrijavljeniKontroler()
    {
        var kontroler = KontrolerHelper.SaKorisnikom(new AccountController(_signInManager.Object, _userManager.Object, _emailServis.Object), "u1", "Student");
        kontroler.Url = _url.Object;
        return kontroler;
    }

    private static RegisterViewModel Registracija() => new() { Email = Email, Password = Lozinka, ConfirmPassword = Lozinka };

    private void NoviKorisnikSeMozeKreirati()
    {
        _userManager.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync((Korisnik?)null);
        _userManager.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((Korisnik?)null);
        _userManager.Setup(m => m.CreateAsync(It.IsAny<Korisnik>(), Lozinka)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<Korisnik>(), "Student")).ReturnsAsync(IdentityResult.Success);
    }

    // ---------- Login (GET) ----------

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public async Task LoginGet_AnonimniKorisnik_VracaFormu()
    {
        Assert.IsInstanceOfType(await Kontroler().Login(), typeof(ViewResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task LoginGet_PrijavljeniModerator_PreusmjeravaNaModeratorDashboard()
    {
        var korisnik = new Korisnik { Id = "u1" };
        _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(korisnik);
        _userManager.Setup(m => m.IsInRoleAsync(korisnik, "Moderator")).ReturnsAsync(true);

        var rezultat = await PrijavljeniKontroler().Login();

        var preusmjerenje = Rezultat.Kao<RedirectToActionResult>(rezultat);
        Assert.AreEqual("Moderator", preusmjerenje.ControllerName);
        Assert.AreEqual("Dashboard", preusmjerenje.ActionName);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task LoginGet_PrijavljenAliKorisnikNePostoji_PreusmjeravaNaPocetnu()
    {
        _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync((Korisnik?)null);

        var rezultat = await PrijavljeniKontroler().Login();

        Assert.AreEqual("Home", Rezultat.Kao<RedirectToActionResult>(rezultat).ControllerName);
    }

    // ---------- Login (POST) ----------

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Internal call verifier")]
    public async Task LoginPost_NevalidanModel_VracaFormuBezPozivaIdentity()
    {
        var kontroler = Kontroler();
        kontroler.ModelState.AddModelError("Email", "Email je obavezan");

        var rezultat = await kontroler.Login(new LoginViewModel { Email = "", Password = "" });

        Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
        _userManager.Verify(m => m.FindByEmailAsync(It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task LoginPost_NepostojeciEmail_VracaOpcuGresku()
    {
        _userManager.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync((Korisnik?)null);
        var kontroler = Kontroler();

        await kontroler.Login(new LoginViewModel { Email = Email, Password = Lozinka });

        Assert.AreEqual("Pogrešan email ili lozinka.", Rezultat.PrvaGreska(kontroler.ModelState));
    }

    /// <summary>Data-driven (JSON): stanje naloga, ispravnost lozinke i uloga -> preusmjerenje ili poruka greške.</summary>
    public static IEnumerable<object[]> ScenarijiPrijave =>
        TestPodaci.Json("prijava_scenariji.json").Select(s => new object[]
        {
            s.GetProperty("emailPotvrdjen").GetBoolean(),
            s.GetProperty("odobren").GetBoolean(),
            s.GetProperty("lozinkaIspravna").GetBoolean(),
            s.GetProperty("uloga").GetString()!,
            s.GetProperty("ocekivaniKontroler").GetString()!,
            s.GetProperty("ocekivanaAkcija").GetString()!,
            s.GetProperty("ocekivanaGreska").GetString()!
        });

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [Priority(1)]
    [TestCategory("Equality verifier")]
    [DynamicData(nameof(ScenarijiPrijave), DynamicDataSourceType.Property)]
    public async Task LoginPost_StanjeNalogaILozinka_PreusmjeravaIliPrijavljujeGresku(
        bool emailPotvrdjen, bool odobren, bool lozinkaIspravna, string uloga,
        string ocekivaniKontroler, string ocekivanaAkcija, string ocekivanaGreska)
    {
        var korisnik = new Korisnik { Id = "u1", UserName = "ana", Email = Email, EmailConfirmed = emailPotvrdjen, IsApproved = odobren };
        _userManager.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync(korisnik);
        _userManager.Setup(m => m.IsInRoleAsync(korisnik, uloga)).ReturnsAsync(true);
        _signInManager.Setup(m => m.PasswordSignInAsync("ana", Lozinka, false, false))
            .ReturnsAsync(IdentityMockovi.RezultatPrijave(lozinkaIspravna));
        var kontroler = Kontroler();

        var rezultat = await kontroler.Login(new LoginViewModel { Email = Email, Password = Lozinka });

        Assert.AreEqual(ocekivaniKontroler, Rezultat.KontrolerPreusmjerenja(rezultat));
        Assert.AreEqual(ocekivanaAkcija, Rezultat.AkcijaPreusmjerenja(rezultat));
        Assert.AreEqual(ocekivanaGreska, Rezultat.PrvaGreska(kontroler.ModelState));
    }

    // ---------- Register (GET/POST) ----------

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public void RegisterGet_PreusmjeravaNaLogin()
    {
        Assert.AreEqual("Login", Rezultat.Kao<RedirectToActionResult>(Kontroler().Register()).ActionName);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("State verifier")]
    public async Task RegisterPost_NevalidanModel_VracaLoginFormuSaTabomRegistracije()
    {
        var kontroler = Kontroler();
        kontroler.ModelState.AddModelError("ConfirmPassword", "Lozinke se ne podudaraju");

        var rezultat = await kontroler.Register(Registracija());

        Assert.AreEqual("Login", Rezultat.Kao<ViewResult>(rezultat).ViewName);
        Assert.AreEqual("register", kontroler.ViewData["ActiveTab"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Internal call verifier")]
    public async Task RegisterPost_PostojeciNepotvrdjenKorisnik_PonovoSaljeVerifikacijskiEmail()
    {
        var postojeci = new Korisnik { Id = "u1", Email = Email, EmailConfirmed = false };
        _userManager.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync(postojeci);
        var kontroler = Kontroler();

        var rezultat = await kontroler.Register(Registracija());

        Assert.AreEqual("Login", Rezultat.Kao<RedirectToActionResult>(rezultat).ActionName);
        Assert.AreEqual("Novi verifikacijski link je poslan na email adresu.", kontroler.TempData["Message"]);
        _emailServis.Verify(e => e.SendEmailAsync(Email, "Potvrda email adrese za eZkvi",
            It.Is<string>(tijelo => tijelo.Contains(VerifikacijskiLink))), Times.Once());
        _userManager.Verify(m => m.CreateAsync(It.IsAny<Korisnik>(), It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Exception verifier")]
    public async Task RegisterPost_PonovnoSlanjeEmailaNeuspjesno_VracaFormuSaGreskom()
    {
        _userManager.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync(new Korisnik { Id = "u1", Email = Email });
        _emailServis.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("SMTP nije dostupan"));
        var kontroler = Kontroler();

        var rezultat = await kontroler.Register(Registracija());

        Assert.AreEqual("Login", Rezultat.Kao<ViewResult>(rezultat).ViewName);
        Assert.AreEqual("Novi verifikacijski email nije poslan. Pokušajte ponovo kasnije.", Rezultat.PrvaGreska(kontroler.ModelState));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Exception verifier")]
    public async Task RegisterPost_LinkSeNeMozeKreirati_NeSaljeEmailIVracaGresku()
    {
        _userManager.Setup(m => m.FindByEmailAsync(Email)).ReturnsAsync(new Korisnik { Id = "u1", Email = Email });
        _url.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns((string?)null);
        var kontroler = Kontroler();

        await kontroler.Register(Registracija());

        Assert.AreEqual("Novi verifikacijski email nije poslan. Pokušajte ponovo kasnije.", Rezultat.PrvaGreska(kontroler.ModelState));
        _emailServis.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    [DataRow(false, "Korisnik sa ovom email adresom je već registrovan i čeka odobrenje administratora.")]
    [DataRow(true, "Korisnik sa ovom email adresom već postoji.")]
    public async Task RegisterPost_PostojeciPotvrdjenKorisnik_VracaGresku(bool odobren, string ocekivanaGreska)
    {
        _userManager.Setup(m => m.FindByEmailAsync(Email))
            .ReturnsAsync(new Korisnik { Id = "u1", Email = Email, EmailConfirmed = true, IsApproved = odobren });
        var kontroler = Kontroler();

        await kontroler.Register(Registracija());

        Assert.AreEqual(ocekivanaGreska, Rezultat.PrvaGreska(kontroler.ModelState));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Internal call verifier")]
    public async Task RegisterPost_ZauzetoKorisnickoIme_DodajeBrojNaKorisnickoIme()
    {
        NoviKorisnikSeMozeKreirati();
        _userManager.Setup(m => m.FindByNameAsync("ana")).ReturnsAsync(new Korisnik { Id = "drugi" });

        await Kontroler().Register(Registracija());

        _userManager.Verify(m => m.CreateAsync(
            It.Is<Korisnik>(k => k.UserName == "ana1" && k.Email == Email && !k.EmailConfirmed && !k.IsApproved),
            Lozinka), Times.Once());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [Priority(1)]
    [TestCategory("Internal call verifier")]
    public async Task RegisterPost_NoviKorisnik_DodajeUloguStudentISaljeEmail()
    {
        NoviKorisnikSeMozeKreirati();
        var kontroler = Kontroler();

        var rezultat = await kontroler.Register(Registracija());

        Assert.AreEqual("Login", Rezultat.Kao<RedirectToActionResult>(rezultat).ActionName);
        StringAssert.StartsWith((string?)kontroler.TempData["Message"], "Registracija je uspješna.");
        _userManager.Verify(m => m.AddToRoleAsync(It.Is<Korisnik>(k => k.UserName == "ana"), "Student"), Times.Once());
        _emailServis.Verify(e => e.SendEmailAsync(Email, It.IsAny<string>(), It.Is<string>(t => t.Contains(VerifikacijskiLink))), Times.Once());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task RegisterPost_KreiranjeNeuspjesno_PrikazujeGreskeIdentity()
    {
        NoviKorisnikSeMozeKreirati();
        _userManager.Setup(m => m.CreateAsync(It.IsAny<Korisnik>(), Lozinka))
            .ReturnsAsync(IdentityMockovi.IdentityRezultat(false, "Lozinka mora imati najmanje 8 karaktera."));
        var kontroler = Kontroler();

        var rezultat = await kontroler.Register(Registracija());

        Assert.AreEqual("Login", Rezultat.Kao<ViewResult>(rezultat).ViewName);
        Assert.AreEqual("Lozinka mora imati najmanje 8 karaktera.", Rezultat.PrvaGreska(kontroler.ModelState));
        _userManager.Verify(m => m.AddToRoleAsync(It.IsAny<Korisnik>(), It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Internal call verifier")]
    public async Task RegisterPost_DodjelaUlogeNeuspjesna_BriseKreiranogKorisnika()
    {
        NoviKorisnikSeMozeKreirati();
        _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<Korisnik>(), "Student"))
            .ReturnsAsync(IdentityMockovi.IdentityRezultat(false, "Uloga ne postoji."));
        var kontroler = Kontroler();

        await kontroler.Register(Registracija());

        Assert.AreEqual("Uloga ne postoji.", Rezultat.PrvaGreska(kontroler.ModelState));
        _userManager.Verify(m => m.DeleteAsync(It.Is<Korisnik>(k => k.UserName == "ana")), Times.Once());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Exception verifier")]
    public async Task RegisterPost_SlanjeEmailaNeuspjesno_BriseKorisnikaIVracaGresku()
    {
        NoviKorisnikSeMozeKreirati();
        _emailServis.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("SMTP nije dostupan"));
        var kontroler = Kontroler();

        var rezultat = await kontroler.Register(Registracija());

        Assert.AreEqual("Login", Rezultat.Kao<ViewResult>(rezultat).ViewName);
        StringAssert.StartsWith(Rezultat.PrvaGreska(kontroler.ModelState), "Registracija nije završena");
        _userManager.Verify(m => m.DeleteAsync(It.IsAny<Korisnik>()), Times.Once());
    }

    // ---------- ConfirmEmail ----------

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Boundary verifier")]
    [DataRow("", "token")]
    [DataRow("u1", "")]
    [DataRow("   ", "token")]
    public async Task ConfirmEmail_PrazniParametri_VracaGresku(string userId, string token)
    {
        var kontroler = Kontroler();

        var rezultat = await kontroler.ConfirmEmail(userId, token);

        Assert.AreEqual("Login", Rezultat.Kao<RedirectToActionResult>(rezultat).ActionName);
        Assert.AreEqual("Verifikacijski link nije ispravan.", kontroler.TempData["Error"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task ConfirmEmail_NepostojeciKorisnik_VracaGresku()
    {
        _userManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync((Korisnik?)null);
        var kontroler = Kontroler();

        await kontroler.ConfirmEmail("u1", "token");

        Assert.AreEqual("Korisnik nije pronađen.", kontroler.TempData["Error"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Internal call verifier")]
    public async Task ConfirmEmail_VecPotvrdjenEmail_NePotvrdjujePonovo()
    {
        _userManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(new Korisnik { Id = "u1", EmailConfirmed = true });
        var kontroler = Kontroler();

        await kontroler.ConfirmEmail("u1", "token");

        StringAssert.StartsWith((string?)kontroler.TempData["Message"], "Email adresa je već potvrđena.");
        _userManager.Verify(m => m.ConfirmEmailAsync(It.IsAny<Korisnik>(), It.IsAny<string>()), Times.Never());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Internal call verifier")]
    public async Task ConfirmEmail_IspravanToken_DekodiraTokenIPotvrdjujeEmail()
    {
        var korisnik = new Korisnik { Id = "u1" };
        _userManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(korisnik);
        _userManager.Setup(m => m.ConfirmEmailAsync(korisnik, "moj-token")).ReturnsAsync(IdentityResult.Success);
        var kodiraniToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("moj-token"));
        var kontroler = Kontroler();

        await kontroler.ConfirmEmail("u1", kodiraniToken);

        Assert.AreEqual("Email adresa je potvrđena. Nalog sada čeka odobrenje administratora.", kontroler.TempData["Message"]);
        _userManager.Verify(m => m.ConfirmEmailAsync(korisnik, "moj-token"), Times.Once());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public async Task ConfirmEmail_NevazeciToken_VracaGresku()
    {
        var korisnik = new Korisnik { Id = "u1" };
        _userManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(korisnik);
        _userManager.Setup(m => m.ConfirmEmailAsync(korisnik, It.IsAny<string>()))
            .ReturnsAsync(IdentityMockovi.IdentityRezultat(false, "Token nije ispravan ili je istekao."));
        var kontroler = Kontroler();

        await kontroler.ConfirmEmail("u1", WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("stari-token")));

        Assert.AreEqual("Email nije potvrđen. Link nije ispravan ili je istekao.", kontroler.TempData["Error"]);
    }

    // ---------- Logout / NijeUlogovan / AccessDenied ----------

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Internal call verifier")]
    public async Task Logout_OdjavljujeKorisnikaIPreusmjeravaNaLogin()
    {
        var rezultat = await PrijavljeniKontroler().Logout();

        Assert.AreEqual("Login", Rezultat.Kao<RedirectToActionResult>(rezultat).ActionName);
        _signInManager.Verify(m => m.SignOutAsync(), Times.Once());
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public void NijeUlogovan_PrijavljeniKorisnik_PreusmjeravaNaPocetnu()
    {
        Assert.AreEqual("Home", Rezultat.Kao<RedirectToActionResult>(PrijavljeniKontroler().NijeUlogovan()).ControllerName);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public void NijeUlogovan_AnonimniKorisnik_VracaView()
    {
        Assert.IsInstanceOfType(Kontroler().NijeUlogovan(), typeof(ViewResult));
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public void AccessDenied_AnonimniKorisnik_PreusmjeravaNaNijeUlogovan()
    {
        Assert.AreEqual(nameof(AccountController.NijeUlogovan), Rezultat.Kao<RedirectToActionResult>(Kontroler().AccessDenied()).ActionName);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Execution tester")]
    public void AccessDenied_PrijavljeniKorisnik_VracaView()
    {
        Assert.IsInstanceOfType(PrijavljeniKontroler().AccessDenied(), typeof(ViewResult));
    }
}
