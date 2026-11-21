using ezZkvi.Models;

namespace ezZkvi.Services
{
    /// <summary>
    /// Računa niz uzastopnih dana u kojima je student završio bar jedan kviz (Task 3, funkcionalnost 1).
    /// </summary>
    public class NizDanaService
    {
        public int IzracunajTrenutniNiz(IEnumerable<KvizSesija> sesije, DateTime danas)
        {
            var dani = sesije
                .Where(s => s.Status == StatusSesije.ZAVRSEN || s.Status == StatusSesije.ISTEKAO)
                .Select(s => s.DatumZavrsetka.Date)
                .Distinct()
                .ToList();

            var niz = 0;
            var dan = danas.Date;
            while (dani.Contains(dan))
            {
                niz++;
                dan = dan.AddDays(-1);
            }

            return niz;
        }
    }
}
