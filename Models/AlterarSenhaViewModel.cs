using System.ComponentModel.DataAnnotations;

namespace Farol_Seguro.Models
{
    public class AlterarSenhaViewModel
    {
        // Garante que o usuário forneça a senha atual para validação de segurança no Controller.
        [Required(ErrorMessage = "A senha atual é obrigatória.")]
        [DataType(DataType.Password)]
        public string SenhaAtual { get; set; }

        // Garante que o usuário forneça uma nova senha.
        [Required(ErrorMessage = "A nova senha é obrigatória.")]
        [StringLength(100, ErrorMessage = "A {0} deve ter pelo menos {2} e no máximo {1} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string NovaSenha { get; set; }

        // Garante que a senha de confirmação seja fornecida e corresponda à NovaSenha.
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Nova Senha")]
        [Compare("NovaSenha", ErrorMessage = "A nova senha e a confirmação de senha não coincidem.")]
        public string ConfirmarSenha { get; set; }
    }
}