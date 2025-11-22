using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Farol_Seguro.Models
{
    public class Escola
    {
        [Key]
        public int Id_Escola { get; set; }

        public string Nome_Escola { get; set; }
        public string Cnpj_Escola { get; set; }

        public string Endereco_Escola { get; set; }
        public string Telefone_Escola { get; set; }

        [ForeignKey("Cidade")]
        public int Id_Cidade { get; set; }
        public Cidade Cidade { get; set; }

        [ForeignKey("Funcionario")]
        public int Id_Funcionario { get; set; }
        public Funcionario Funcionario { get; set; }

        public ICollection<Denuncia> Denuncias { get; set; }
    }
}
