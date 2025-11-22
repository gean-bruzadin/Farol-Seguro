using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Farol_Seguro.Models
{
    public class Funcionario
    {
        [Key]
        public int Id_Funcionario { get; set; }

        public string Nome_Funcionario { get; set; }
        public string Email_Funcionario { get; set; } // Campo de login
        public string Senha_Funcionario { get; set; } // Campo de login
        public string Departamento_Funcionario { get; set; }

        [ForeignKey("Cargo")]
        public int Id_Cargo { get; set; }
        public Cargo Cargo { get; set; }

        // ***** ALTERAÇÃO AQUI *****
        // Removemos a referência ao Usuario
        // [ForeignKey("Usuario")]
        // public int Id_Usuario { get; set; }
        // public Usuario Usuario { get; set; }

        // Adicionamos a referência direta ao Nivel
        public int Id_Nivel { get; set; }

        [ForeignKey("Id_Nivel")]
        public Nivel Nivel { get; set; }

        public ICollection<Resposta> Respostas { get; set; }
    }
}