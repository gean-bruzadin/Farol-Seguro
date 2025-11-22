using Farol_Seguro.Config;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Farol_Seguro.Models;

namespace SafeLink_TCC.Controllers
{
    public class AutenticacaoController : Controller
    {
        private readonly DbConfig _dbConfig;

        public AutenticacaoController(DbConfig dbconfig)
        {
            _dbConfig = dbconfig;
        }

        [HttpGet]
        public IActionResult Login() => View();


        [HttpPost]
        public async Task<IActionResult> Login(string email, string senha)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(senha))
            {
                ViewBag.Mensagem = "Preencha todos os campos.";
                return View();
            }

            // 1) Tenta autenticar como ADMIN (tabela Usuarios com Nível Admin)
            var admin = await _dbConfig.Usuarios
                .Include(u => u.Nivel)
                .FirstOrDefaultAsync(u => u.Email_Usuario == email && u.Nivel.Nome_Nivel == "Admin");

            if (admin != null && BCrypt.Net.BCrypt.Verify(senha, admin.Senha_Usuario))
            {
                await CriarSessao(admin.Id_Usuario.ToString(), admin.Nome_Usuario, admin.Email_Usuario, admin.Nivel.Nome_Nivel);
                return RedirectToAction("Index", "Home");
            }

            // 2) Tenta autenticar como FUNCIONÁRIO (tabela Funcionarios)
            var funcionario = await _dbConfig.Funcionarios
                .Include(f => f.Nivel)
                .FirstOrDefaultAsync(f => f.Email_Funcionario == email);

            if (funcionario != null && BCrypt.Net.BCrypt.Verify(senha, funcionario.Senha_Funcionario))
            {
                await CriarSessao(funcionario.Id_Funcionario.ToString(), funcionario.Nome_Funcionario, funcionario.Email_Funcionario, funcionario.Nivel.Nome_Nivel);
                return RedirectToAction("Index", "Home");
            }

            // 3) Tenta autenticar como ALUNO (tabela Alunos)
            var aluno = await _dbConfig.Alunos
                .Include(a => a.Nivel)
                .FirstOrDefaultAsync(a => a.Email_Aluno == email);

            if (aluno != null && BCrypt.Net.BCrypt.Verify(senha, aluno.Senha_Aluno))
            {
                // ===============================================
                // PASSO DE VERIFICAÇÃO DE BLOQUEIO PARA ALUNO (MANTIDO)
                // ===============================================
                if (aluno.IsBloqueado)
                {
                    ViewBag.Mensagem = "🚫 **Acesso Negado:** Sua conta foi bloqueada devido a múltiplas denúncias falsas.";
                    return View();
                }
                // ===============================================

                await CriarSessao(aluno.Id_Aluno.ToString(), aluno.Nome_Aluno, aluno.Email_Aluno, aluno.Nivel.Nome_Nivel);
                return RedirectToAction("Index", "Home");
            }

            // 4) Se nenhuma das opções acima funcionar:
            ViewBag.Mensagem = "E-mail ou senha incorretos.";
            return View();
        }

        private async Task CriarSessao(string id, string nome, string email, string role)
        {
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, id),
        new Claim(ClaimTypes.Name, nome),
        new Claim(ClaimTypes.Email, email),
        new Claim(ClaimTypes.Role, role) // Admin, Funcionario, Aluno
    };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties { IsPersistent = true }
            );
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
