using System.ComponentModel.DataAnnotations;

namespace Farol_Seguro.Models
{
    public class Nivel
    {
        [Key]
        public int Id_Nivel { get; set; }

        public string Nome_Nivel { get; set; }

        public ICollection<Usuario> Usuarios { get; set; }
    }
}
