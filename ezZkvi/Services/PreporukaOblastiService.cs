using ezZkvi.Models;

namespace ezZkvi.Services
{
    /// <summary>
    /// Na osnovu odgovora iz završenih kvizova predlaže oblasti koje student treba ponoviti
    /// (Task 3, funkcionalnost 2).
    /// </summary>
    public class PreporukaOblastiService
    {
        /// <summary>Oblast se preporučuje kad je tačnost strogo manja od ovog praga (u procentima).</summary>
        private const int PragTacnosti = 60;

        /// <summary>Sa manje odgovora tačnost nije pouzdana, pa se oblast ne preporučuje.</summary>
        private const int MinimalnoOdgovora = 5;

        private const int MaksimalnoPreporuka = 3;

        /// <summary>
        /// Vraća najviše tri najslabije oblasti: prvo po tačnosti, a kod iste tačnosti
        /// prednost ima oblast sa više odgovora (rezultat je pouzdaniji).
        /// </summary>
        public List<PreporukaOblasti> OblastiZaPonavljanje(IEnumerable<KvizSesijaPitanje> stavke)
        {
            return stavke
                .GroupBy(s => s.Pitanje!.OblastId)
                .Select(NapraviPreporuku)
                .Where(TrebaPonoviti)
                .OrderBy(p => p.Tacnost)
                .ThenByDescending(p => p.BrojOdgovora)
                .Take(MaksimalnoPreporuka)
                .ToList();
        }

        private static bool TrebaPonoviti(PreporukaOblasti preporuka)
        {
            return preporuka.BrojOdgovora >= MinimalnoOdgovora && preporuka.Tacnost < PragTacnosti;
        }

        private static PreporukaOblasti NapraviPreporuku(IGrouping<int, KvizSesijaPitanje> grupa)
        {
            var ukupno = grupa.Count();
            return new PreporukaOblasti
            {
                OblastId = grupa.Key,
                Naziv = grupa.First().Pitanje!.Oblast?.Naziv ?? string.Empty,
                BrojOdgovora = ukupno,
                Tacnost = grupa.Count(JeTacna) * 100 / ukupno
            };
        }

        /// <summary>Tačan odgovor ima oznaku 1; netačan 0, a preskočeno pitanje -1, pa se oba računaju kao netačna.</summary>
        private static bool JeTacna(KvizSesijaPitanje stavka)
        {
            return stavka.Tacno == 1;
        }
    }
}
