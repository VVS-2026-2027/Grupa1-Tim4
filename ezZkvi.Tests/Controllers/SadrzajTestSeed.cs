using ezZkvi.Data;
using ezZkvi.Models;
using ezZkvi.Tests.Infrastruktura;

namespace ezZkvi.Tests.Controllers;

/// <summary>
/// Zajednički početni podaci za testove upravljanja sadržajem (predmeti, oblasti, pitanja).
///  - Predmet 1 "Matematika" (kreator: mod-1): oblasti 1 "Algebra", 2 "Analiza"; pitanja 1 i 2
///  - Predmet 2 "Fizika" (kreator: mod-2): oblast 3 "Mehanika"; pitanje 3
///  - Predmet 3 "Hemija" (sistemski, bez kreatora): bez oblasti
/// Odgovori: Id = pitanjeId * 10 + k (k = 1..4), tačan je k = 1.
/// Autor: Imran Čustović (B)
/// </summary>
public static class SadrzajTestSeed
{
    public const string AdminId = "admin-1";
    public const string ModeratorId = "mod-1";
    public const string DrugiModeratorId = "mod-2";

    public static ApplicationDbContext Kontekst()
    {
        var kontekst = TestDbFactory.Kreiraj();

        kontekst.Predmet.AddRange(
            new Predmet { Id = 1, Naziv = "Matematika", KreatorId = ModeratorId },
            new Predmet { Id = 2, Naziv = "Fizika", KreatorId = DrugiModeratorId },
            new Predmet { Id = 3, Naziv = "Hemija", KreatorId = null });

        kontekst.Oblast.AddRange(
            new Oblast { Id = 1, Naziv = "Algebra", PredmetId = 1 },
            new Oblast { Id = 2, Naziv = "Analiza", PredmetId = 1 },
            new Oblast { Id = 3, Naziv = "Mehanika", PredmetId = 2 });

        DodajPitanje(kontekst, 1, "Šta je matrica?", 1, 1);
        DodajPitanje(kontekst, 2, "Šta je derivacija?", 1, 2);
        DodajPitanje(kontekst, 3, "Šta je sila?", 2, 3);

        TestDbFactory.Snimi(kontekst);
        return kontekst;
    }

    public static void DodajPitanje(ApplicationDbContext kontekst, int id, string tekst, int predmetId, int oblastId, int brojOdgovora = 4)
    {
        kontekst.Pitanje.Add(new Pitanje
        {
            Id = id,
            TekstPitanja = tekst,
            PredmetId = predmetId,
            OblastId = oblastId,
            Tezina = Tezina.LAKO
        });

        for (var k = 1; k <= brojOdgovora; k++)
        {
            kontekst.Odgovor.Add(new Odgovor
            {
                Id = id * 10 + k,
                Tekst = $"Odgovor {id}-{k}",
                IsTacan = k == 1,
                PitanjeId = id
            });
        }
    }
}
