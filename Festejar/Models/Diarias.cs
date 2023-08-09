namespace Festejar.Models
{
    public class Diarias
    {
        public int Id { get; set; }
        public int Casa_id { get; set; }
        public int Ano { get; set; }
        public int Mes { get; set; }
        public int Semana { get; set; }
        public int Dia { get; set; }
        public int Prioridade { get; set; }
        public decimal Valor { get; set; }
    }
}
