using ezZkvi.Data;
using ezZkvi.Models;
using ezZkvi.Tests.Infrastruktura;

namespace ezZkvi.Tests.Controllers;

/// <summary>
/// Priprema podataka (Arrange) za testove StudentControllera.
/// Konvencija: odgovor sa Id = pitanjeId * 10 + k; k = 1 je tačan, ostali su netačni.
/// </summary>
public static class StudentTestSeed
{
    public const string StudentId = "student-1";
    public const string DrugiStudentId = "student-2";
    public const int PredmetId = 1;
    public const int OblastId = 1;
    public const int DrugiPredmetId = 2;
    public const int DrugaOblastId = 2;

    public static int TacanOdgovor(int pitanjeId) => pitanjeId * 10 + 1;
    public static int NetacanOdgovor(int pitanjeId) => pitanjeId * 10 + 2;

    public static ApplicationDbContext KontekstSaPredmetima()
    {
        var kontekst = TestDbFactory.Kreiraj();
        kontekst.Predmet.AddRange(
            new Predmet { Id = PredmetId, Naziv = "Matematika" },
            new Predmet { Id = DrugiPredmetId, Naziv = "Fizika" });
        kontekst.Oblast.AddRange(
            new Oblast { Id = OblastId, Naziv = "Algebra", PredmetId = PredmetId },
            new Oblast { Id = DrugaOblastId, Naziv = "Mehanika", PredmetId = DrugiPredmetId });
        TestDbFactory.Snimi(kontekst);
        return kontekst;
    }

    public static void DodajPitanje(
        ApplicationDbContext kontekst,
        int pitanjeId,
        Tezina tezina = Tezina.LAKO,
        int oblastId = OblastId,
        int predmetId = PredmetId,
        int brojOdgovora = 4,
        int brojTacnih = 1)
    {
        kontekst.Pitanje.Add(new Pitanje
        {
            Id = pitanjeId,
            TekstPitanja = $"Pitanje broj {pitanjeId}",
            Tezina = tezina,
            PredmetId = predmetId,
            OblastId = oblastId
        });

        for (var k = 1; k <= brojOdgovora; k++)
        {
            kontekst.Odgovor.Add(new Odgovor
            {
                Id = pitanjeId * 10 + k,
                Tekst = $"Odgovor {pitanjeId}-{k}",
                IsTacan = k <= brojTacnih,
                PitanjeId = pitanjeId
            });
        }

        TestDbFactory.Snimi(kontekst);
    }

    /// <summary>Pet validnih ABCD pitanja: 1 teško, 2 srednja, 2 laka (Id 1..5).</summary>
    public static void DodajPetValidnihPitanja(ApplicationDbContext kontekst)
    {
        DodajPitanje(kontekst, 1, Tezina.TESKO);
        DodajPitanje(kontekst, 2, Tezina.SREDNJE);
        DodajPitanje(kontekst, 3, Tezina.SREDNJE);
        DodajPitanje(kontekst, 4, Tezina.LAKO);
        DodajPitanje(kontekst, 5, Tezina.LAKO);
    }

    public static KvizSesija DodajSesiju(
        ApplicationDbContext kontekst,
        string studentId = StudentId,
        StatusSesije status = StatusSesije.U_TOKU,
        int minutaOdPocetka = 0,
        int? predmetId = PredmetId,
        int? oblastId = OblastId,
        int brojTacnih = 0,
        int procenat = 0,
        int minutaOdZavrsetka = 0)
    {
        var pocetak = DateTime.UtcNow.AddMinutes(-minutaOdPocetka);
        var sesija = new KvizSesija
        {
            TraziBrojPitanja = 5,
            VremenskoOgranicenje = 3,
            Status = status,
            StudentId = studentId,
            PredmetId = predmetId,
            OblastId = oblastId,
            BrojTacnih = brojTacnih,
            Procenat = procenat,
            DatumPocetka = pocetak,
            DatumZavrsetka = DateTime.UtcNow.AddMinutes(-minutaOdZavrsetka)
        };

        kontekst.KvizSesije.Add(sesija);
        TestDbFactory.Snimi(kontekst);
        return sesija;
    }

    /// <summary>Otvorene (neodgovorene) stavke sesije, redom kako su pitanja navedena.</summary>
    public static void DodajStavke(ApplicationDbContext kontekst, int sesijaId, params int[] pitanjeIds)
    {
        for (var i = 0; i < pitanjeIds.Length; i++)
        {
            DodajStavku(kontekst, sesijaId, pitanjeIds[i], i + 1);
        }
    }

    public static void DodajStavku(
        ApplicationDbContext kontekst,
        int sesijaId,
        int pitanjeId,
        int redniBroj,
        int? odgovorId = null,
        double tacno = 0,
        double bodovi = 0)
    {
        kontekst.KvizSesijaPitanja.Add(new KvizSesijaPitanje
        {
            KvizSesijaId = sesijaId,
            PitanjeId = pitanjeId,
            RedniBroj = redniBroj,
            OdgovorId = odgovorId,
            Tacno = tacno,
            BrojBodova = bodovi
        });
        TestDbFactory.Snimi(kontekst);
    }

    /// <summary>
    /// Aktivna sesija u kojoj je prvih N pitanja tačno odgovoreno, sljedećih M netačno,
    /// a ostala preskočena. Vraća Id sesije.
    /// </summary>
    public static int SesijaSaStanjimaOdgovora(ApplicationDbContext kontekst, int tacnih, int netacnih, int preskocenih)
    {
        var ukupno = tacnih + netacnih + preskocenih;
        var sesija = DodajSesiju(kontekst);

        for (var pitanjeId = 1; pitanjeId <= ukupno; pitanjeId++)
        {
            DodajPitanje(kontekst, pitanjeId);

            if (pitanjeId <= tacnih)
            {
                DodajStavku(kontekst, sesija.ID, pitanjeId, pitanjeId, TacanOdgovor(pitanjeId), 1, 0.1);
            }
            else if (pitanjeId <= tacnih + netacnih)
            {
                DodajStavku(kontekst, sesija.ID, pitanjeId, pitanjeId, NetacanOdgovor(pitanjeId), 0, -0.03);
            }
            else
            {
                DodajStavku(kontekst, sesija.ID, pitanjeId, pitanjeId, null, -1, 0);
            }
        }

        return sesija.ID;
    }
}
