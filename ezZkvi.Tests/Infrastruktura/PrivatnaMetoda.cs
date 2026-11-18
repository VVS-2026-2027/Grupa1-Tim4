using System.Reflection;

namespace ezZkvi.Tests.Infrastruktura;

/// <summary>
/// Poziv privatne metode instance preko refleksije. Zamjena za PrivateObject iz Predavanja 6
/// (klasa PrivateObject ne postoji u MSTest v3 za .NET 8).
/// </summary>
public static class PrivatnaMetoda
{
    public static T Pozovi<T>(object instanca, string nazivMetode, params object[] argumenti)
    {
        var metoda = instanca.GetType().GetMethod(nazivMetode, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(metoda, $"Privatna metoda '{nazivMetode}' ne postoji.");
        return (T)metoda.Invoke(instanca, argumenti)!;
    }
}
