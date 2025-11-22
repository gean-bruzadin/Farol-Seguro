using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Farol_Seguro.Models
{
    public class Denuncia
    {
        [Key]
        public int Id_Denuncia { get; set; }

        public string Titulo_Denuncia { get; set; }

        public string Descricao_Denuncia { get; set; }

        public DateTime DataCriacao_Denuncia { get; set; } = DateTime.Now;
        public string Categoria_Denuncia { get; set; }
        public string Status_Denuncia { get; set; }

        
        public int? Id_Aluno { get; set; }
        public Aluno? Aluno { get; set; }

        [ForeignKey("Escola")]
        public int Id_Escola { get; set; }
        public Escola Escola { get; set; }

        public bool DenunciaAnonima { get; set; } = false; // Novo campo

        public ICollection<Anexo> Anexos { get; set; }

       public ICollection<Resposta> Respostas { get; set; }
        public ICollection<DenunciaTestemunha> DenunciaTestemunhas { get; set; }
        public ICollection<Notificacao> Notificacoes { get; set; }

    }
}
