using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ezZkvi.Tests.Infrastruktura;

/// <summary>Pomoćne metode za provjeru rezultata akcija (bez logike u samim testovima).</summary>
public static class Rezultat
{
    /// <summary>Provjeri tip i vrati objekat kastovan na taj tip.</summary>
    public static T Kao<T>(object? vrijednost)
    {
        Assert.IsInstanceOfType(vrijednost, typeof(T));
        return (T)vrijednost!;
    }

    /// <summary>Naziv kontrolera na koji akcija preusmjerava ili "" ako rezultat nije preusmjerenje.</summary>
    public static string KontrolerPreusmjerenja(IActionResult rezultat)
    {
        return (rezultat as RedirectToActionResult)?.ControllerName ?? string.Empty;
    }

    /// <summary>Naziv akcije na koju se preusmjerava ili "" ako rezultat nije preusmjerenje.</summary>
    public static string AkcijaPreusmjerenja(IActionResult rezultat)
    {
        return (rezultat as RedirectToActionResult)?.ActionName ?? string.Empty;
    }

    /// <summary>Prva greška modela za zadani ključ ("" = greške cijelog modela) ili "" ako je nema.</summary>
    public static string PrvaGreska(ModelStateDictionary modelState, string kljuc = "")
    {
        return modelState.TryGetValue(kljuc, out var unos) && unos.Errors.Count > 0
            ? unos.Errors[0].ErrorMessage
            : string.Empty;
    }

    /// <summary>Pročitaj svojstvo anonimnog objekta (npr. iz JsonResult) preko refleksije.</summary>
    public static object? Svojstvo(object objekat, string naziv)
    {
        var svojstvo = objekat.GetType().GetProperty(naziv);
        Assert.IsNotNull(svojstvo, $"Objekat nema svojstvo '{naziv}'.");
        return svojstvo.GetValue(objekat);
    }
}
