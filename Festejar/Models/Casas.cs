using Google.Protobuf.WellKnownTypes;

namespace Festejar.Models
{
    public class Casas
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Descricao { get; set; }
        public string Status { get; set; }
        public int Capacidade { get; set; }
        public int Cidade_id { get; set; }
        public string Endereco { get; set; }
        public string Telefone  { get; set; }
    }
}
