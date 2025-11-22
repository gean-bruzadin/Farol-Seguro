using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Farol_Seguro.Models
{
    [Table("Notificacao")]
    public class Notificacao
    {
        [Key]
        public int Id_Notificacao { get; set; }

        [Required(ErrorMessage = "O texto da notificação é obrigatório.")]
        [StringLength(500)]
        public string Mensagem { get; set; }

        [Required]
        public bool Lida { get; set; } = false; // Valor padrão é não lida

        public DateTime DataCriacao { get; set; } = DateTime.Now;

        [StringLength(255)]
        public string UrlDestino { get; set; } // O link para onde a notificação leva (ex: a denúncia)

        // 🔑 NOVO: Chave Estrangeira para a Denúncia
        [ForeignKey("Denuncia")]
        public int Id_Denuncia { get; set; }
        public Denuncia Denuncia { get; set; } // Propriedade de Navegação

        // 🔑 Chave Estrangeira para o Aluno
        [Required(ErrorMessage = "O aluno destinatário é obrigatório.")]
        [ForeignKey("Aluno")]
        public int Id_Aluno { get; set; }
        public Aluno Aluno { get; set; }
    }
}