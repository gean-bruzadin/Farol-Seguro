using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Farol_Seguro.Models
{
    public class Aluno
    {
        [Key]
        public int Id_Aluno { get; set; }

        public string Nome_Aluno { get; set; }
        public string Email_Aluno { get; set; }
        public string Senha_Aluno { get; set; }
        public string Genero_Aluno { get; set; }
        public DateTime DataNascimento_Aluno { get; set; }

        public int Id_Nivel { get; set; }

        [ForeignKey("Id_Nivel")]
        public Nivel Nivel { get; set; }

        public ICollection<Denuncia> Denuncias { get; set; }
        public ICollection<Notificacao> Notificacoes { get; set; }

    }
}