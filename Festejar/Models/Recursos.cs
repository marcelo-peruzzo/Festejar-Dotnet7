namespace Festejar.Models
{
    public class Recursos
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Descricao { get; set; }
        public string Foto { get; set; }
        public decimal Valor { get; set; }
        public int Quantidade { get; set; }
        public DateTime? Deleted_at { get; set; }
    }
}
