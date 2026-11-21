using ezZkvi.Models;
using ezZkvi.Services;
using ezZkvi.Tests.Infrastruktura;

namespace ezZkvi.Tests.Services;

/// <summary>
/// TDD testovi za niz uzastopnih dana učenja (Task 3, funkcionalnost 1).
/// "Danas" je fiksan datum, pa rezultat ne zavisi od dana kada se testovi pokreću.
/// </summary>
[TestClass]
public sealed class NizDanaServiceTests
{
    private static readonly DateTime Danas = new(2026, 11, 21, 15, 0, 0);

    /// <summary>CSV: dani unazad (odvojeni sa ;) i očekivani najduži niz.</summary>
    public static IEnumerable<object[]> NajduziNizIzCsv =>
        TestPodaci.Csv("najduzi_niz.csv").Select(red => new object[] { Dani(red[0]), TestPodaci.Int(red[1]) });

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

    private static List<KvizSesija> Sesije(params int[] daniUnazad)
    {
        return daniUnazad.Select(d => Sesija(d)).ToList();
    }

    private static int[] Dani(string tekst)
    {
        return string.IsNullOrWhiteSpace(tekst)
            ? Array.Empty<int>()
            : tekst.Split(';').Select(TestPodaci.Int).ToArray();
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

    [TestMethod]
    [TestProperty("Autor", "Dino Tahirović")]
    [TestCategory("Equality verifier")]
    [DataRow(new[] { 0 }, 1, DisplayName = "Samo danas")]
    [DataRow(new[] { 0, 1, 2 }, 3, DisplayName = "Tri dana zaredom")]
    [DataRow(new[] { 0, 0, 1 }, 2, DisplayName = "Dva kviza danas se broje kao jedan dan")]
    [DataRow(new[] { 0, 1, 3, 4 }, 2, DisplayName = "Preskocen dan prekida niz")]
    public void IzracunajTrenutniNiz_ZavrseniKvizovi_BrojiUzastopneDane(int[] daniUnazad, int ocekivano)
    {
        var niz = _servis.IzracunajTrenutniNiz(Sesije(daniUnazad), Danas);

        Assert.AreEqual(ocekivano, niz);
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    [DataRow(new[] { 1, 2 }, 2, DisplayName = "Jucer i prekjucer - niz traje")]
    [DataRow(new[] { 2, 3 }, 0, DisplayName = "Posljednji kviz prije dva dana - niz prekinut")]
    public void IzracunajTrenutniNiz_DanasJosNemaKviza_NizSeRacunaOdJucer(int[] daniUnazad, int ocekivano)
    {
        var niz = _servis.IzracunajTrenutniNiz(Sesije(daniUnazad), Danas);

        Assert.AreEqual(ocekivano, niz);
    }

    [TestMethod]
    [TestProperty("Autor", "Amir Džafić")]
    [TestCategory("Equality verifier")]
    [DynamicData(nameof(NajduziNizIzCsv), DynamicDataSourceType.Property)]
    public void IzracunajNajduziNiz_HistorijaKvizova_VracaNajduziNizUzastopnihDana(int[] daniUnazad, int ocekivano)
    {
        var niz = _servis.IzracunajNajduziNiz(Sesije(daniUnazad));

        Assert.AreEqual(ocekivano, niz);
    }
}
