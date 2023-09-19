namespace Festejar.Models
{
    public class Reservas
    {
        public int Id { get; set; }
        public int Casa_id { get; set; }
        public decimal Valor { get; set; }
        public int QuantidadePessoas { get; set; }
        public int ClienteID { get; set; }
        public int usuarioID { get; set; }
        public string StatusPagamento { get; set; }
        public string StatusReserva { get; set; }
        public string Observacoes { get; set; }
        public DateTime DataFim { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataReserva { get; set; }
        
    }
}
