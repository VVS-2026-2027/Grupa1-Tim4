using ezZkvi.Services;
using Microsoft.AspNetCore.Identity;

namespace ezZkvi.Tests.Services;

/// <summary>
/// Testovi prevoda poruka ASP.NET Identity grešaka na bosanski jezik.
/// Svaka metoda mora zadržati originalni Code (po njemu Identity prepoznaje grešku) i vratiti bosanski opis.
/// Opisivač nema stanje, pa se jedna instanca pravi za cijelu klasu ([ClassInitialize]) i oslobađa na kraju ([ClassCleanup]).
/// Autor: Amir Džafić (C)
/// </summary>
[TestClass]
public sealed class BosanskiIdentityErrorDescriberTests
{
    private static BosanskiIdentityErrorDescriber _opisivac = null!;

    [ClassInitialize]
    public static void KreirajOpisivac(TestContext _)
    {
        _opisivac = new BosanskiIdentityErrorDescriber();
    }

    [ClassCleanup]
    public static void OslobodiOpisivac()
    {
        _opisivac = null!;
    }

    private static void Provjeri(IdentityError greska, string ocekivaniKod, string ocekivaniOpis)
    {
        Assert.AreEqual(ocekivaniKod, greska.Code);
        Assert.AreEqual(ocekivaniOpis, greska.Description);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public void DefaultError_VracaOpcuPoruku() =>
        Provjeri(_opisivac.DefaultError(), "DefaultError", "Došlo je do nepoznate greške.");

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    [DataRow(6)]
    [DataRow(8)]
    public void PasswordTooShort_UbacujeMinimalnuDuzinu(int duzina) =>
        Provjeri(_opisivac.PasswordTooShort(duzina), "PasswordTooShort", $"Lozinka mora imati najmanje {duzina} karaktera.");

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public void PasswordRequiresNonAlphanumeric_TraziSpecijalniZnak() =>
        Provjeri(_opisivac.PasswordRequiresNonAlphanumeric(), "PasswordRequiresNonAlphanumeric", "Lozinka mora sadržavati barem jedan specijalni znak (npr. !, ?, #).");

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public void PasswordRequiresDigit_TraziBroj() =>
        Provjeri(_opisivac.PasswordRequiresDigit(), "PasswordRequiresDigit", "Lozinka mora sadržavati barem jedan broj (0-9).");

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public void PasswordRequiresLower_TraziMaloSlovo() =>
        Provjeri(_opisivac.PasswordRequiresLower(), "PasswordRequiresLower", "Lozinka mora sadržavati barem jedno malo slovo (a-z).");

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public void PasswordRequiresUpper_TraziVelikoSlovo() =>
        Provjeri(_opisivac.PasswordRequiresUpper(), "PasswordRequiresUpper", "Lozinka mora sadržavati barem jedno veliko slovo (A-Z).");

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public void PasswordRequiresUniqueChars_UbacujeBrojRazlicitihZnakova() =>
        Provjeri(_opisivac.PasswordRequiresUniqueChars(3), "PasswordRequiresUniqueChars", "Lozinka mora imati najmanje 3 različitih karaktera.");

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public void PasswordMismatch_PogresnaLozinka() =>
        Provjeri(_opisivac.PasswordMismatch(), "PasswordMismatch", "Pogrešna lozinka.");

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public void DuplicateEmail_UbacujeEmail() =>
        Provjeri(_opisivac.DuplicateEmail("ana@etf.ba"), "DuplicateEmail", "Email 'ana@etf.ba' je već registrovan.");

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public void DuplicateUserName_UbacujeKorisnickoIme() =>
        Provjeri(_opisivac.DuplicateUserName("ana"), "DuplicateUserName", "Korisničko ime 'ana' je već zauzeto.");

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public void InvalidEmail_NeOtkrivaUneseniEmail() =>
        Provjeri(_opisivac.InvalidEmail("nije-email"), "InvalidEmail", "Email adresa nije ispravna.");

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Null verifier")]
    public void InvalidEmail_NullEmail_VracaIstuPoruku() =>
        Provjeri(_opisivac.InvalidEmail(null), "InvalidEmail", "Email adresa nije ispravna.");

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public void InvalidUserName_NedozvoljeniZnakovi() =>
        Provjeri(_opisivac.InvalidUserName("ana!"), "InvalidUserName", "Korisničko ime sadrži nedozvoljene znakove.");

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public void InvalidToken_TokenNijeIspravan() =>
        Provjeri(_opisivac.InvalidToken(), "InvalidToken", "Token nije ispravan ili je istekao.");

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public void UserAlreadyHasPassword_KorisnikVecImaLozinku() =>
        Provjeri(_opisivac.UserAlreadyHasPassword(), "UserAlreadyHasPassword", "Korisnik već ima postavljenu lozinku.");

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public void UserAlreadyInRole_UbacujeUlogu() =>
        Provjeri(_opisivac.UserAlreadyInRole("Student"), "UserAlreadyInRole", "Korisnik je već u ulozi 'Student'.");

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public void ConcurrencyFailure_PodaciIzmijenjeni() =>
        Provjeri(_opisivac.ConcurrencyFailure(), "ConcurrencyFailure", "Podaci su u međuvremenu izmijenjeni. Pokušaj ponovo.");

    /// <summary>Klasa prevodi standardne Identity poruke, pa i poruka da korisnik nije u ulozi treba biti na bosanskom.</summary>
    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    public void UserNotInRole_VracaPorukuNaBosanskom() =>
        Provjeri(_opisivac.UserNotInRole("Admin"), "UserNotInRole", "Korisnik nije u ulozi 'Admin'.");

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Identity verifier")]
    public void Opisivac_JeIdentityErrorDescriber()
    {
        Assert.IsInstanceOfType(_opisivac, typeof(IdentityErrorDescriber));
    }
}
