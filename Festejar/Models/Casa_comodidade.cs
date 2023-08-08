using System.ComponentModel.DataAnnotations.Schema;

namespace Festejar.Models
{
    public class Casa_comodidade
    {
        public int Id { get; set; }
        [ForeignKey("Casas")] // Indica que Casa_id é uma chave estrangeira relacionada à tabela Casas
        public int Casa_id { get; set; }

        [ForeignKey("Comodidades")] // Indica que Comodidade_id é uma chave estrangeira relacionada à tabela Comodidades
        public int Comodidade_id { get; set; }
        public Casas Casas { get; set; }
        public Comodidades Comodidades { get; set; }

    }
}
