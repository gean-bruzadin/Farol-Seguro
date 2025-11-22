using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Farol_Seguro.Models
{
    public class Cidade
    {
        [Key]
        public int Id_Cidade { get; set; }
        public string Nome_Cidade { get; set; }

        [ForeignKey("Estado")]
        public int Id_Estado { get; set; }
        public Estado Estado { get; set; }

        public ICollection<Escola> Escolas { get; set; }
    }
}
