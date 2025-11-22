using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Farol_Seguro.Models
{
    public class Resposta
    {
        [Key]
        public int Id_Resposta { get; set; }

        public string Descricao_Resposta { get; set; }

        public DateTime Data_Resposta { get; set; }

        [ForeignKey("Denuncia")]
        public int Id_Denuncia { get; set; }

        // CORRIGIDO: Propriedade de navegação no singular
        public Denuncia Denuncia { get; set; }

        [ForeignKey("Funcionario")]
        public int Id_Funcionario { get; set; }
        public Funcionario Funcionario { get; set; } // P
    }
}