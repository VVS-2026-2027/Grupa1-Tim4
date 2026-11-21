using ezZkvi.Models;

namespace ezZkvi.Services
{
    /// <summary>
    /// Računa niz uzastopnih dana u kojima je student završio bar jedan kviz (Task 3, funkcionalnost 1).
    /// </summary>
    public class NizDanaService
    {
        /// <summary>
        /// Trenutni niz: broji se unazad od danas, a ako danas još nema kviza, od jučer
        /// (dan još nije gotov, pa niz nije prekinut).
        /// </summary>
        public int IzracunajTrenutniNiz(IEnumerable<KvizSesija> sesije, DateTime danas)
        {
            var dani = DaniSaZavrsenimKvizom(sesije);
            var posljednjiDan = dani.Contains(danas.Date) ? danas.Date : danas.Date.AddDays(-1);
            return BrojUzastopnihDanaDo(dani, posljednjiDan);
        }

        /// <summary>
        /// Najduži niz u historiji: broji se samo od dana kojima niz završava
        /// (sljedeći dan nema kviza), pa se svaki niz prođe tačno jednom.
        /// </summary>
        public int IzracunajNajduziNiz(IEnumerable<KvizSesija> sesije)
        {
            var dani = DaniSaZavrsenimKvizom(sesije);
            return dani
                .Where(dan => !dani.Contains(dan.AddDays(1)))
                .Select(krajNiza => BrojUzastopnihDanaDo(dani, krajNiza))
                .DefaultIfEmpty(0)
                .Max();
        }

        private static HashSet<DateTime> DaniSaZavrsenimKvizom(IEnumerable<KvizSesija> sesije)
        {
            return sesije
                .Where(JeZavrsena)
                .Select(s => s.DatumZavrsetka.Date)
                .ToHashSet();
        }

        private static bool JeZavrsena(KvizSesija sesija)
        {
            return sesija.Status == StatusSesije.ZAVRSEN || sesija.Status == StatusSesije.ISTEKAO;
        }

        /// <summary>Broji uzastopne dane unazad, počevši od zadanog dana.</summary>
        private static int BrojUzastopnihDanaDo(HashSet<DateTime> dani, DateTime posljednjiDan)
        {
            var niz = 0;
            while (dani.Contains(posljednjiDan.AddDays(-niz)))
            {
                niz++;
            }

            return niz;
        }
    }
}
