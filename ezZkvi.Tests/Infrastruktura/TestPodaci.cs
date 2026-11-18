using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ezZkvi.Tests.Infrastruktura;

/// <summary>
/// Čitanje pohranjenih testnih podataka (CSV, JSON, XML) iz foldera TestData
/// za data-driven testove ([DynamicData]).
/// </summary>
public static class TestPodaci
{
    private static string Putanja(string fajl) => Path.Combine(AppContext.BaseDirectory, "TestData", fajl);

    /// <summary>Redovi CSV fajla bez zaglavlja; polja mogu biti u navodnicima.</summary>
    public static IEnumerable<string[]> Csv(string fajl)
    {
        return File.ReadAllLines(Putanja(fajl), Encoding.UTF8)
            .Skip(1)
            .Where(linija => !string.IsNullOrWhiteSpace(linija))
            .Select(ParsirajCsvLiniju)
            .ToList();
    }

    /// <summary>Elementi JSON niza.</summary>
    public static IEnumerable<JsonElement> Json(string fajl)
    {
        using var dokument = JsonDocument.Parse(File.ReadAllText(Putanja(fajl), Encoding.UTF8));
        return dokument.RootElement.EnumerateArray().Select(e => e.Clone()).ToList();
    }

    /// <summary>Svi elementi zadanog imena ispod korijena XML dokumenta.</summary>
    public static IEnumerable<XElement> Xml(string fajl, string element)
    {
        return XDocument.Load(Putanja(fajl)).Root!.Elements(element).ToList();
    }

    public static int Int(string vrijednost) => int.Parse(vrijednost.Trim(), CultureInfo.InvariantCulture);

    public static int? NullableInt(string vrijednost) =>
        string.IsNullOrWhiteSpace(vrijednost) ? null : int.Parse(vrijednost.Trim(), CultureInfo.InvariantCulture);

    public static double Double(string vrijednost) => double.Parse(vrijednost.Trim(), CultureInfo.InvariantCulture);

    public static bool Bool(string vrijednost) => bool.Parse(vrijednost.Trim());

    private static string[] ParsirajCsvLiniju(string linija)
    {
        var polja = new List<string>();
        var trenutno = new StringBuilder();
        var uNavodnicima = false;

        for (var i = 0; i < linija.Length; i++)
        {
            var znak = linija[i];
            if (uNavodnicima)
            {
                if (znak == '"' && i + 1 < linija.Length && linija[i + 1] == '"')
                {
                    trenutno.Append('"');
                    i++;
                }
                else if (znak == '"')
                {
                    uNavodnicima = false;
                }
                else
                {
                    trenutno.Append(znak);
                }
            }
            else if (znak == '"')
            {
                uNavodnicima = true;
            }
            else if (znak == ',')
            {
                polja.Add(trenutno.ToString());
                trenutno.Clear();
            }
            else
            {
                trenutno.Append(znak);
            }
        }

        polja.Add(trenutno.ToString());
        return polja.ToArray();
    }
}
