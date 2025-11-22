using System.ComponentModel.DataAnnotations;

namespace Farol_Seguro.Models
{
    public class Cargo
    {
        [Key]
        public int Id_Cargo { get; set; }

        public string Nome_Cargo { get; set; }

        public ICollection<Funcionario> Funcionarios { get; set; }
    }
}
