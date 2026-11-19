using ezZkvi.Services;
using ezZkvi.Tests.Infrastruktura;

namespace ezZkvi.Tests.Services;

/// <summary>
/// Testovi pomoćne klase za validaciju i normalizaciju naziva predmeta/oblasti i teksta pitanja.
/// Data-driven: CSV (normalizacija + dozvoljeni znakovi) i XML (ključ za poređenje).
/// Autor: Imran Čustović (B)
/// </summary>
[TestClass]
public sealed class ContentValidationTests
{
    /// <summary>CSV: ulaz, očekivani normalizovani tekst, da li normalizovani naziv ima samo dozvoljene znakove.</summary>
    public static IEnumerable<object[]> NaziviIzCsv =>
        TestPodaci.Csv("content_validacija.csv").Select(red => new object[] { red[0], red[1], TestPodaci.Bool(red[2]) });

    /// <summary>XML: ulaz i očekivani ključ za poređenje (normalizovan + mala slova).</summary>
    public static IEnumerable<object[]> KljuceviIzXml =>
        TestPodaci.Xml("kljuc_poredjenja.xml", "slucaj").Select(e => new object[]
        {
            (string)e.Attribute("ulaz")!,
            (string)e.Attribute("ocekivano")!
        });

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    [DynamicData(nameof(NaziviIzCsv), DynamicDataSourceType.Property)]
    public void NormalizujUnos_RazliciteVrijednosti_UklanjaVisakRazmaka(string ulaz, string ocekivano, bool _)
    {
        Assert.AreEqual(ocekivano, ContentValidation.NormalizujUnos(ulaz));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Boolean verifier")]
    [DynamicData(nameof(NaziviIzCsv), DynamicDataSourceType.Property)]
    public void NazivImaDozvoljeneZnakove_RazliciteVrijednosti_DozvoljavaSamoSlovaBrojeveIRazmake(string _, string normalizovano, bool dozvoljeno)
    {
        Assert.AreEqual(dozvoljeno, ContentValidation.NazivImaDozvoljeneZnakove(normalizovano));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    [DynamicData(nameof(KljuceviIzXml), DynamicDataSourceType.Property)]
    public void KljucZaPoredjenje_RazliciteVrijednosti_VracaNormalizovanTekstMalimSlovima(string ulaz, string ocekivano)
    {
        Assert.AreEqual(ocekivano, ContentValidation.KljucZaPoredjenje(ulaz));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Null verifier")]
    public void NormalizujUnos_Null_VracaPrazanString()
    {
        Assert.AreEqual(string.Empty, ContentValidation.NormalizujUnos(null));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Equality verifier")]
    [DataRow("a\tb", "a b")]
    [DataRow("a\r\n\r\nb", "a b")]
    [DataRow("\t prvi \n drugi \t", "prvi drugi")]
    public void NormalizujUnos_TaboviINoviRedovi_ZamjenjujeJednimRazmakom(string ulaz, string ocekivano)
    {
        Assert.AreEqual(ocekivano, ContentValidation.NormalizujUnos(ulaz));
    }

    [TestMethod]
    [TestProperty("Autor", "Imran Čustović")]
    [TestCategory("Boolean verifier")]
    public void KljucZaPoredjenje_RazlicitaVelicinaSlovaIRazmaci_DajeIstiKljuc()
    {
        Assert.AreEqual(
            ContentValidation.KljucZaPoredjenje("Linearna Algebra"),
            ContentValidation.KljucZaPoredjenje("  LINEARNA    algebra "));
    }
}
