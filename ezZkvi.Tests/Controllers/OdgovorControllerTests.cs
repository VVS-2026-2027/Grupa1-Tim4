using ezZkvi.Controllers;
using ezZkvi.Models;
using ezZkvi.Tests.Infrastruktura;
using Microsoft.AspNetCore.Mvc;

namespace ezZkvi.Tests.Controllers;

/// <summary>
/// OdgovorController nema vlastitu logiku: Index preusmjerava na upravljanje sadržajem,
/// a sve CRUD akcije su zabranjene (odgovori se uređuju kroz pitanje).
/// Autor: Dino Tahirović (A)
/// </summary>
[TestClass]
public sealed class OdgovorControllerTests
{
    private OdgovorController _kontroler = null!;

    [TestInitialize]
    public void Postavi()
    {
        _kontroler = KontrolerHelper.SaKorisnikom(new OdgovorController(), "admin-1", "Admin");
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public void Index_PreusmjeravaNaTabPitanjaUSadrzaju()
    {
        var preusmjerenje = Rezultat.Kao<RedirectToActionResult>(_kontroler.Index());

        Assert.AreEqual("Index", preusmjerenje.ActionName);
        Assert.AreEqual("Content", preusmjerenje.ControllerName);
        Assert.AreEqual("questions", preusmjerenje.RouteValues!["tab"]);
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void Details_VracaForbid() => Assert.IsInstanceOfType(_kontroler.Details(1), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void CreateGet_VracaForbid() => Assert.IsInstanceOfType(_kontroler.Create(), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void CreatePost_VracaForbid() => Assert.IsInstanceOfType(_kontroler.Create(new Odgovor()), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void EditGet_VracaForbid() => Assert.IsInstanceOfType(_kontroler.Edit(1), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void EditPost_VracaForbid() => Assert.IsInstanceOfType(_kontroler.Edit(1, new Odgovor()), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void DeleteGet_VracaForbid() => Assert.IsInstanceOfType(_kontroler.Delete(1), typeof(ForbidResult));

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Execution tester")]
    public void DeleteConfirmed_VracaForbid() => Assert.IsInstanceOfType(_kontroler.DeleteConfirmed(1), typeof(ForbidResult));
}
