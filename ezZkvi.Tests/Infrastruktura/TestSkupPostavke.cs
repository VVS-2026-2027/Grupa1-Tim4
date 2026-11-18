using System.Globalization;

namespace ezZkvi.Tests.Infrastruktura;

/// <summary>
/// Postavke koje važe za cijeli testni skup ([AssemblyInitialize] / [AssemblyCleanup]).
/// Testovi porede tekst (ToLower, brojevi sa decimalnom tačkom), a rezultat tih operacija zavisi od
/// jezičkih postavki računara. Zato se prije svih testova postavlja neutralna (invariant) kultura,
/// da testovi daju isti rezultat na svakom računaru (ponovljivost), a nakon svih testova vraća se stara.
/// </summary>
[TestClass]
public sealed class TestSkupPostavke
{
    private static CultureInfo? _prethodnaKultura;
    private static CultureInfo? _prethodnaUiKultura;

    [AssemblyInitialize]
    public static void PostaviNeutralnuKulturu(TestContext _)
    {
        _prethodnaKultura = CultureInfo.DefaultThreadCurrentCulture;
        _prethodnaUiKultura = CultureInfo.DefaultThreadCurrentUICulture;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    [AssemblyCleanup]
    public static void VratiPrethodnuKulturu()
    {
        CultureInfo.DefaultThreadCurrentCulture = _prethodnaKultura;
        CultureInfo.DefaultThreadCurrentUICulture = _prethodnaUiKultura;
    }
}
