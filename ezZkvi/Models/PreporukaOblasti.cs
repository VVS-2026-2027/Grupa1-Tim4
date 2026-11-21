namespace ezZkvi.Models
{
    /// <summary>
    /// Oblast koju student treba ponoviti: broj odgovora u završenim kvizovima i tačnost u procentima.
    /// </summary>
    public class PreporukaOblasti
    {
        public int OblastId { get; set; }
        public string Naziv { get; set; } = string.Empty;
        public int BrojOdgovora { get; set; }
        public int Tacnost { get; set; }
    }
}
