using ezZkvi.Models;
using ezZkvi.Services;

namespace ezZkvi.Tests.Services;

/// <summary>
/// TDD testovi za niz uzastopnih dana učenja (Task 3, funkcionalnost 1).
/// "Danas" je fiksan datum, pa rezultat ne zavisi od dana kada se testovi pokreću.
/// </summary>
[TestClass]
public sealed class NizDanaServiceTests
{
    private static readonly DateTime Danas = new(2026, 11, 21, 15, 0, 0);

    private NizDanaService _servis = null!;

    [TestInitialize]
    public void Postavi()
    {
        _servis = new NizDanaService();
    }

    /// <summary>Sesija završena prije zadanog broja dana (0 = danas).</summary>
    private static KvizSesija Sesija(int danaUnazad, StatusSesije status = StatusSesije.ZAVRSEN)
    {
        return new KvizSesija { Status = status, DatumZavrsetka = Danas.AddDays(-danaUnazad) };
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Boundary verifier")]
    public void IzracunajTrenutniNiz_SamoKvizUToku_VracaNulu()
    {
        var sesije = new List<KvizSesija> { Sesija(0, StatusSesije.U_TOKU) };

        var niz = _servis.IzracunajTrenutniNiz(sesije, Danas);

        Assert.AreEqual(0, niz);
    }
}
