using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Farol_Seguro.Models
{
    public class DenunciaTestemunha
    {
        [ForeignKey("Testemunha")]
        public int Id_Testemunha { get; set; }
        public Testemunha Testemunha { get; set; }

        [ForeignKey("Denuncia")]
        public int Id_Denuncia { get; set; }
        public Denuncia Denuncias { get; set; }
    }
}
