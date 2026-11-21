using ezZkvi.Models;
using ezZkvi.Services;

namespace ezZkvi.Tests.Services;

/// <summary>
/// TDD testovi za preporuku oblasti za ponavljanje (Task 3, funkcionalnost 2).
/// </summary>
[TestClass]
public sealed class PreporukaOblastiServiceTests
{
    private PreporukaOblastiService _servis = null!;

    [TestInitialize]
    public void Postavi()
    {
        _servis = new PreporukaOblastiService();
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Empty tester")]
    public void OblastiZaPonavljanje_NemaOdgovora_VracaPraznuListu()
    {
        var preporuke = _servis.OblastiZaPonavljanje(new List<KvizSesijaPitanje>());

        Assert.AreEqual(0, preporuke.Count);
    }
}
