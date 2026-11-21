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
            return new List<PreporukaOblasti>();
        }
    }
}
