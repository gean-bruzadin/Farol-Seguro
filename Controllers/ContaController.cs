using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Farol_Seguro.Config; // Seu DbContext
using Farol_Seguro.Models; // Seus Models (Aluno, Funcionario, Usuario, AlterarSenhaViewModel)
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.Tasks;
using BCrypt.Net; // Necessário para a criptografia de senha

[Authorize]
public class ContaController : Controller
{
    private readonly DbConfig _context;

    public ContaController(DbConfig context)
    {
        _context = context;
    }

    // ======================================================= 1. CONFIGURAÇÕES (GET) =======================================================
    public async Task<IActionResult> Configuracoes()
    {
        string? idClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        string? roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int idUsuario))
        {
            // Se as Claims não existirem, força o logout e redireciona
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Autenticacao");
        }

        string nome = "Usuário Desconhecido";
        string email = "email@naoinformado.com";

        // Busca dados com base na Role
        if (roleClaim == "Aluno")
        {
            Aluno? aluno = await _context.Alunos.FirstOrDefaultAsync(a => a.Id_Aluno == idUsuario);
            if (aluno != null)
            {
                nome = aluno.Nome_Aluno;
                email = aluno.Email_Aluno;
            }
        }
        else if (roleClaim == "Funcionario")
        {
            Funcionario? funcionario = await _context.Funcionarios.FirstOrDefaultAsync(f => f.Id_Funcionario == idUsuario);
            if (funcionario != null)
            {
                nome = funcionario.Nome_Funcionario;
                email = funcionario.Email_Funcionario;
            }
        }
        else if (roleClaim == "Admin" || roleClaim == "Usuario")
        {
            Usuario? usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id_Usuario == idUsuario);
            if (usuario != null)
            {
                nome = usuario.Nome_Usuario;
                email = usuario.Email_Usuario;
            }
        }

        ViewData["NomeUsuario"] = nome;
        ViewData["EmailUsuario"] = email;
        ViewData["RoleUsuario"] = roleClaim;

        return View();
    }

    // ======================================================= 2. ALTERAR SENHA (POST) =======================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarSenha(AlterarSenhaViewModel model)
    {
        // Validação da Model View (Required, Compare)
        if (!ModelState.IsValid)
        {
            // Passa erros de validação para a View
            TempData["MensagemErro"] = ModelState.FirstOrDefault(m => m.Value!.Errors.Any())
                                       .Value!.Errors.First().ErrorMessage;
            return RedirectToAction(nameof(Configuracoes));
        }

        string? idClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        string? roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int idUsuario))
        {
            return Unauthorized();
        }

        bool sucesso = false;

        // Lógica de ALTERAÇÃO baseada na Role
        if (roleClaim == "Aluno")
        {
            Aluno? aluno = await _context.Alunos.FindAsync(idUsuario);
            // 1. Verifica a Senha Atual usando o BCrypt.Verify
            if (aluno != null && VerificarHashDaSenha(model.SenhaAtual, aluno.Senha_Aluno))
            {
                // 2. Cria o Hash da Nova Senha e atualiza
                aluno.Senha_Aluno = CriarHashDaSenha(model.NovaSenha);
                _context.Alunos.Update(aluno);
                sucesso = true;
            }
        }
        else if (roleClaim == "Funcionario")
        {
            Funcionario? funcionario = await _context.Funcionarios.FindAsync(idUsuario);
            if (funcionario != null && VerificarHashDaSenha(model.SenhaAtual, funcionario.Senha_Funcionario))
            {
                funcionario.Senha_Funcionario = CriarHashDaSenha(model.NovaSenha);
                _context.Funcionarios.Update(funcionario);
                sucesso = true;
            }
        }
        else if (roleClaim == "Admin" || roleClaim == "Usuario")
        {
            Usuario? usuario = await _context.Usuarios.FindAsync(idUsuario);
            if (usuario != null && VerificarHashDaSenha(model.SenhaAtual, usuario.Senha_Usuario))
            {
                usuario.Senha_Usuario = CriarHashDaSenha(model.NovaSenha);
                _context.Usuarios.Update(usuario);
                sucesso = true;
            }
        }

        if (sucesso)
        {
            await _context.SaveChangesAsync();
            TempData["MensagemSucesso"] = "Senha alterada com sucesso!";
        }
        else
        {
            // Este é o erro que indica que a Senha Atual estava incorreta (o mais comum)
            TempData["MensagemErro"] = "Erro ao alterar a senha. Verifique a senha atual.";
        }

        return RedirectToAction(nameof(Configuracoes));
    }

    // ======================================================= 3. EXCLUIR CONTA (POST) =======================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirConta()
    {
        string? idClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        string? roleClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int idUsuario))
        {
            return Unauthorized();
        }

        bool sucesso = false;

        // Lógica de EXCLUSÃO baseada na Role
        if (roleClaim == "Aluno")
        {
            Aluno? aluno = await _context.Alunos.FindAsync(idUsuario);
            if (aluno != null)
            {
                _context.Alunos.Remove(aluno);
                sucesso = true;
            }
        }
        else if (roleClaim == "Funcionario")
        {
            Funcionario? funcionario = await _context.Funcionarios.FindAsync(idUsuario);
            if (funcionario != null)
            {
                _context.Funcionarios.Remove(funcionario);
                sucesso = true;
            }
        }
        else if (roleClaim == "Admin" || roleClaim == "Usuario")
        {
            Usuario? usuario = await _context.Usuarios.FindAsync(idUsuario);
            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);
                sucesso = true;
            }
        }

        if (sucesso)
        {
            await _context.SaveChangesAsync();
            // 1. Desloga o usuário
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            // 2. Define a mensagem
            TempData["MensagemSucesso"] = "Sua conta foi excluída com sucesso.";
            // 3. Redireciona para o Login
            return RedirectToAction("Login", "Autenticacao"); // Vou usar "Auth" ou "Autenticacao" conforme o último código
        }

        TempData["MensagemErro"] = "Erro ao excluir a conta.";
        return RedirectToAction(nameof(Configuracoes));
    }

    // ======================================================= 4. SAIR/LOGOUT (POST) =======================================================
    [HttpPost]
    public async Task<IActionResult> Sair()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Autenticacao");
    }

    // ======================================================= MÉTODOS AUXILIARES DE HASHING (BCrypt) =======================================================

    /// <summary>
    /// Cria um hash da senha usando BCrypt.Net.Core, incluindo um salt aleatório.
    /// </summary>
    private string CriarHashDaSenha(string senha)
    {
        return BCrypt.Net.BCrypt.HashPassword(senha);
    }

    /// <summary>
    /// Verifica se a senha em texto puro corresponde ao hash salvo, extraindo o salt do hash.
    /// </summary>
    private bool VerificarHashDaSenha(string senhaDigitada, string hashSalvo)
    {
        if (string.IsNullOrEmpty(hashSalvo))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(senhaDigitada, hashSalvo);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Retorna false se o hash no banco não for um hash BCrypt válido
            return false;
        }
    }
    // ===================================================================================================================================
}