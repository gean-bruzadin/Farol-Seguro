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

        // --- CAMPOS NOVOS PARA BLOQUEIO ---
        // Contador de denúncias falsas
        public int ContadorDenunciasFalsas { get; set; } = 0;

        // Flag para indicar se o aluno está bloqueado de criar novas denúncias
        public bool IsBloqueado { get; set; } = false;
        // ---------------------------------

        public ICollection<Denuncia> Denuncias { get; set; }
        public ICollection<Notificacao> Notificacoes { get; set; }

    }
}