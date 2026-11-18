using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace ezZkvi.Tests.Infrastruktura;

/// <summary>
/// Postavlja ControllerContext: prijavljenog (ili anonimnog) korisnika sa ulogama
/// i TempData sa zamjenskim ITempDataProvider-om (Stub).
/// </summary>
public static class KontrolerHelper
{
    public static T SaKorisnikom<T>(T kontroler, string korisnikId, params string[] uloge) where T : Controller
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, korisnikId),
            new(ClaimTypes.Name, korisnikId)
        };
        claims.AddRange(uloge.Select(u => new Claim(ClaimTypes.Role, u)));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        return Postavi(kontroler, principal);
    }

    public static T Anonimno<T>(T kontroler) where T : Controller
    {
        return Postavi(kontroler, new ClaimsPrincipal(new ClaimsIdentity()));
    }

    private static T Postavi<T>(T kontroler, ClaimsPrincipal principal) where T : Controller
    {
        var httpContext = new DefaultHttpContext { User = principal };
        kontroler.ControllerContext = new ControllerContext { HttpContext = httpContext };
        kontroler.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        return kontroler;
    }
}
