using System.ComponentModel.DataAnnotations;

namespace Farol_Seguro.Models
{
    public class Estado
    {
        [Key]
        public int Id_Estado { get; set; }
        public string Nome_Estado { get; set; }
        public string Sigla_Estado { get; set; }

        public ICollection<Cidade> Cidades { get; set; }
    }
}
