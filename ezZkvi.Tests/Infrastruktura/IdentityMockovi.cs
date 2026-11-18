using ezZkvi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace ezZkvi.Tests.Infrastruktura;

/// <summary>
/// Zamjenski objekti (Mock) za ASP.NET Identity servise. Pravi UserManager/SignInManager
/// traže bazu, hash lozinki, cookie autentifikaciju itd., pa ih u unit testovima mijenjamo mockovima.
/// </summary>
public static class IdentityMockovi
{
    public static Mock<UserManager<Korisnik>> UserManager()
    {
        var store = new Mock<IUserStore<Korisnik>>();
        return new Mock<UserManager<Korisnik>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    /// <summary>IdentityResult za mock: uspjeh ili neuspjeh sa zadanom porukom.</summary>
    public static IdentityResult IdentityRezultat(bool uspjesno, string poruka = "Greška")
    {
        return uspjesno ? IdentityResult.Success : IdentityResult.Failed(new IdentityError { Description = poruka });
    }

    /// <summary>Rezultat prijave za mock SignInManager-a.</summary>
    public static SignInResult RezultatPrijave(bool uspjesno)
    {
        return uspjesno ? SignInResult.Success : SignInResult.Failed;
    }

    public static Mock<SignInManager<Korisnik>> SignInManager(Mock<UserManager<Korisnik>> userManager)
    {
        return new Mock<SignInManager<Korisnik>>(
            userManager.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<Korisnik>>(),
            null!, null!, null!, null!);
    }
}
