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

    /// <summary>Odgovori iz jedne oblasti: tačni (1), netačni (0) i preskočeni (-1).</summary>
    private static List<KvizSesijaPitanje> Stavke(int oblastId, int tacnih, int netacnih = 0, int preskocenih = 0)
    {
        var oblast = new Oblast { Id = oblastId, Naziv = $"Oblast {oblastId}" };
        return Enumerable.Repeat(1.0, tacnih)
            .Concat(Enumerable.Repeat(0.0, netacnih))
            .Concat(Enumerable.Repeat(-1.0, preskocenih))
            .Select(tacno => new KvizSesijaPitanje
            {
                Tacno = tacno,
                Pitanje = new Pitanje { OblastId = oblastId, Oblast = oblast }
            })
            .ToList();
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Empty tester")]
    public void OblastiZaPonavljanje_NemaOdgovora_VracaPraznuListu()
    {
        var preporuke = _servis.OblastiZaPonavljanje(new List<KvizSesijaPitanje>());

        Assert.AreEqual(0, preporuke.Count);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Boundary verifier")]
    [DataRow(5, 5, 0, true, DisplayName = "50 % se preporucuje")]
    [DataRow(6, 4, 0, false, DisplayName = "Tacno 60 % se ne preporucuje")]
    [DataRow(0, 10, 0, true, DisplayName = "0 % se preporucuje")]
    [DataRow(5, 0, 5, true, DisplayName = "Preskoceno pitanje se racuna kao netacno")]
    public void OblastiZaPonavljanje_TacnostOblasti_PreporucujeSamoIspodPraga(
        int tacnih, int netacnih, int preskocenih, bool ocekivanoPreporuceno)
    {
        var stavke = Stavke(1, tacnih, netacnih, preskocenih);

        var preporuke = _servis.OblastiZaPonavljanje(stavke);

        Assert.AreEqual(ocekivanoPreporuceno, preporuke.Any(p => p.OblastId == 1));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Boundary verifier")]
    [DataRow(4, false, DisplayName = "4 odgovora su premalo")]
    [DataRow(5, true, DisplayName = "5 odgovora je dovoljno")]
    public void OblastiZaPonavljanje_BrojOdgovora_TrebaNajmanjePet(int brojNetacnih, bool ocekivanoPreporuceno)
    {
        var stavke = Stavke(1, tacnih: 0, netacnih: brojNetacnih);

        var preporuke = _servis.OblastiZaPonavljanje(stavke);

        Assert.AreEqual(ocekivanoPreporuceno, preporuke.Any(p => p.OblastId == 1));
    }

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    public void OblastiZaPonavljanje_CetiriSlabeOblasti_VracaTriNajslabijeRedom()
    {
        var stavke = Stavke(1, tacnih: 2, netacnih: 3)       // 40 %, 5 odgovora
            .Concat(Stavke(2, tacnih: 0, netacnih: 5))       // 0 %
            .Concat(Stavke(3, tacnih: 4, netacnih: 6))       // 40 %, 10 odgovora
            .Concat(Stavke(4, tacnih: 5, netacnih: 5));      // 50 %

        var preporuke = _servis.OblastiZaPonavljanje(stavke);

        CollectionAssert.AreEqual(new[] { 2, 3, 1 }, preporuke.Select(p => p.OblastId).ToArray());
    }
}
