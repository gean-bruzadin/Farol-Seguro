using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Farol_Seguro.Models
{
    public class Anexo
    {
        [Key]
        public int Id_Anexo { get; set; }

        public string Tipo_Anexo { get; set; }

        public string Caminho_Anexo { get; set; }

        // NOVO CAMPO para o download funcionar corretamente
        public string? NomeOriginal_Anexo { get; set; }

        [ForeignKey("Denuncia")]
        public int Id_Denuncia { get; set; }
        public Denuncia Denuncia { get; set; }
    }
}