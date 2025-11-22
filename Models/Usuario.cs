using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Farol_Seguro.Models
{
    public class Usuario
    {
        [Key]
        public int Id_Usuario { get; set; }
        public string Nome_Usuario { get; set; }
        public string Email_Usuario { get; set; }
        public string Senha_Usuario { get; set; }

        [ForeignKey("Nivel")]
        public int Id_Nivel { get; set; }
        public Nivel Nivel { get; set; }

    }
}
