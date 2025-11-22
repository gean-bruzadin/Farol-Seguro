using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Farol_Seguro.Models
{
    public class LogStatus
    {
        [Key]
        public int Id_Log { get; set; }

        [Required]
        public int Id_Denuncia { get; set; }
        public string Status_Anterior { get; set; }

        [Required]
        public string Status_Novo { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.Now;

       

        // CHAVE ESTRANGEIRA para a Model Nivel (ex: 2 ou 3)
        [Required]
        public int Id_Nivel { get; set; }

        // Nome do Nível (ex: "Funcionario", "Admin")
        [Required]
        public string Nome_Nivel { get; set; }

        [ForeignKey("Id_Denuncia")]
        public Denuncia Denuncia { get; set; }

        [ForeignKey("Id_Nivel")]
        public Nivel Nivel { get; set; }
    }
}
