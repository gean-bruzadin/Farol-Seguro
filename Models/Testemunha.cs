using System.ComponentModel.DataAnnotations;

namespace Farol_Seguro.Models
{
    public class Testemunha
    {
        [Key]
        public int Id_Testemunha { get; set; }
        public string Nome_Testemunha { get; set; }
        public string Telefone_Testemunha { get; set; }

        public ICollection<DenunciaTestemunha> DenunciaTestemunhas { get; set; }
    }
}
