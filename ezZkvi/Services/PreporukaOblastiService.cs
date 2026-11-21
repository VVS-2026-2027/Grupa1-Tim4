using ezZkvi.Models;

namespace ezZkvi.Services
{
    /// <summary>
    /// Na osnovu odgovora iz završenih kvizova predlaže oblasti koje student treba ponoviti
    /// (Task 3, funkcionalnost 2).
    /// </summary>
    public class PreporukaOblastiService
    {
        public List<PreporukaOblasti> OblastiZaPonavljanje(IEnumerable<KvizSesijaPitanje> stavke)
        {
            var rezultat = new List<PreporukaOblasti>();

            foreach (var grupa in stavke.GroupBy(s => s.Pitanje!.OblastId))
            {
                var ukupno = grupa.Count();
                var tacnih = grupa.Count(s => s.Tacno == 1);
                var tacnost = tacnih * 100 / ukupno;

                if (tacnost < 60)
                {
                    rezultat.Add(new PreporukaOblasti
                    {
                        OblastId = grupa.Key,
                        Naziv = grupa.First().Pitanje!.Oblast?.Naziv ?? string.Empty,
                        BrojOdgovora = ukupno,
                        Tacnost = tacnost
                    });
                }
            }

            return rezultat;
        }
    }
}
