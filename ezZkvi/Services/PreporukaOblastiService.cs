using ezZkvi.Models;

namespace ezZkvi.Services
{
    /// <summary>
    /// Na osnovu odgovora iz završenih kvizova predlaže oblasti koje student treba ponoviti
    /// (Task 3, funkcionalnost 2).
    /// </summary>
    public class PreporukaOblastiService
    {
        private const int PragTacnosti = 60;
        private const int MinimalnoOdgovora = 5;

        public List<PreporukaOblasti> OblastiZaPonavljanje(IEnumerable<KvizSesijaPitanje> stavke)
        {
            return stavke
                .GroupBy(s => s.Pitanje!.OblastId)
                .Select(NapraviPreporuku)
                .Where(p => p.BrojOdgovora >= MinimalnoOdgovora && p.Tacnost < PragTacnosti)
                .OrderBy(p => p.Tacnost)
                .ThenByDescending(p => p.BrojOdgovora)
                .Take(3)
                .ToList();
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
