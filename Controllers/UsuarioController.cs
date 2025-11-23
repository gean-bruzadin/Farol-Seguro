using Farol_Seguro.Config;
using Farol_Seguro.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using BCrypt.Net;
using System;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using Microsoft.Extensions.Options;

namespace Farol_Seguro.Controllers
{
    [Authorize]
    public class UsuarioController : Controller
    {
        private readonly string _codigoSecreto;
        private readonly DbConfig _context;

        public UsuarioController(DbConfig context, IOptions<AcessoAdminConfig> acessoAdminConfig)
        {
            _context = context;
            _codigoSecreto = acessoAdminConfig.Value.CodigoSecreto;
        }

        // --- AÇÕES PROTEGIDAS POR [Authorize] ---
        public async Task<IActionResult> Index()
        {
            var usuarios = _context.Usuarios.Include(u => u.Nivel);
            return View(await usuarios.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var usuario = await _context.Usuarios
                .Include(u => u.Nivel)
                .FirstOrDefaultAsync(m => m.Id_Usuario == id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null) return NotFound();
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            usuario.Senha_Usuario = string.Empty;

            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Usuario usuario)
        {
            if (id != usuario.Id_Usuario) return NotFound();

            try
            {
                var usuarioParaAtualizar = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id_Usuario == id);

                if (usuarioParaAtualizar == null)
                {
                    TempData["MensagemErro"] = "Erro: Administrador não encontrado para atualização.";
                    return NotFound();
                }

                usuarioParaAtualizar.Nome_Usuario = usuario.Nome_Usuario;
                usuarioParaAtualizar.Email_Usuario = usuario.Email_Usuario;
                usuarioParaAtualizar.Id_Nivel = 3;

                if (!string.IsNullOrWhiteSpace(usuario.Senha_Usuario))
                {
                    usuarioParaAtualizar.Senha_Usuario = BCrypt.Net.BCrypt.HashPassword(usuario.Senha_Usuario);
                }
                else
                {
                    var senhaAntiga = await _context.Usuarios
                        .Where(u => u.Id_Usuario == id)
                        .Select(u => u.Senha_Usuario)
                        .SingleOrDefaultAsync();

                    usuarioParaAtualizar.Senha_Usuario = senhaAntiga;
                }

                _context.Entry(usuarioParaAtualizar).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                TempData["MensagemSucesso"] = $"O Administrador '{usuario.Nome_Usuario}' foi atualizado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsuarioExists(usuario.Id_Usuario))
                {
                    TempData["MensagemErro"] = "Erro de concorrência: Administrador não encontrado.";
                    return NotFound();
                }
                else throw;
            }
            catch (Exception ex)
            {
                TempData["MensagemErro"] = $"Falha ao atualizar o Administrador. Erro: {ex.Message}";
            }

            return View(usuario);
        }

        public async Task<IActionResult> Deletar(int? id)
        {
            if (id == null) return NotFound();
            var usuario = await _context.Usuarios
                .Include(u => u.Nivel)
                .FirstOrDefaultAsync(m => m.Id_Usuario == id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        [HttpPost, ActionName("Deletar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletarConfirmado(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                TempData["MensagemErro"] = "Erro: Administrador não encontrado para exclusão.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
                TempData["MensagemSucesso"] = $"O Administrador '{usuario.Nome_Usuario}' foi excluído com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["MensagemErro"] = $"Falha ao excluir o Administrador. Erro: {ex.Message}";
                return RedirectToAction(nameof(Deletar), new { id });
            }
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.Id_Usuario == id);
        }

        // --- AÇÕES DE CRIAÇÃO DO PRIMEIRO ADMIN (BOOTSTRAP) ---

        [AllowAnonymous]
        public async Task<IActionResult> AcessoAdmin()
        {
            if (await _context.Usuarios.AnyAsync())
            {
                TempData["MensagemErro"] = "O registro por código secreto está desativado. Por favor, faça login com uma conta de administrador.";
                return RedirectToRoute(new { controller = "Autenticacao", action = "Login" });
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcessoAdmin(string codigo)
        {
            if (await _context.Usuarios.AnyAsync())
            {
                TempData["MensagemErro"] = "O registro por código secreto está desativado. Por favor, faça login com uma conta de administrador.";
                return RedirectToRoute(new { controller = "Autenticacao", action = "Login" });
            }

            if (codigo == _codigoSecreto)
            {
                TempData["AcessoPermitido"] = true;
                return RedirectToAction(nameof(Criar));
            }

            TempData["MensagemErro"] = "Código de acesso incorreto. Você não pode acessar essa área, apenas administradores.";
            return View();
        }


        // GET: Usuario/Criar
        [AllowAnonymous]
        public async Task<IActionResult> Criar()
        {
            bool jaExistemUsuarios = await _context.Usuarios.AnyAsync();

            if (jaExistemUsuarios)
            {
                TempData["MensagemErro"] = "Registro de novos administradores está bloqueado. Por favor, faça login com uma conta de administrador existente para adicionar mais.";
                return RedirectToRoute(new { controller = "Autenticacao", action = "Login" });
            }

            if (TempData["AcessoPermitido"] == null || (bool)TempData["AcessoPermitido"] != true)
            {
                TempData["MensagemErro"] = "Você não pode acessar essa área. Insira o código de acesso de Administrador.";
                return RedirectToAction(nameof(AcessoAdmin));
            }

            TempData.Keep("AcessoPermitido");

            return View();
        }


        // POST: Usuario/Create
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Usuario usuario)
        {
            bool jaExistemUsuarios = await _context.Usuarios.AnyAsync();

            // Lógica de proteção (MANTIDA)
            if (jaExistemUsuarios)
            {
                if (!User.Identity.IsAuthenticated)
                {
                    TempData["MensagemErro"] = "Registro de novos administradores está desativado. Por favor, faça login com uma conta de Administrador existente para adicionar mais.";
                    return RedirectToRoute(new { controller = "Autenticacao", action = "Login" });
                }
            }
            else
            {
                if (TempData["AcessoPermitido"] == null || (bool)TempData["AcessoPermitido"] != true)
                {
                    TempData["MensagemErro"] = "Acesso negado. Necessário inserir o código de segurança antes de prosseguir.";
                    return RedirectToAction(nameof(AcessoAdmin));
                }
            }

            // ⭐ NOVO: VALIDAÇÃO MANUAL BÁSICA DE CAMPOS CRÍTICOS (SUBSTITUI O ModelState.IsValid)
            if (string.IsNullOrEmpty(usuario.Nome_Usuario) || string.IsNullOrEmpty(usuario.Email_Usuario) || string.IsNullOrEmpty(usuario.Senha_Usuario))
            {
                TempData["MensagemErro"] = "Erro! Por favor, **preencha todos os campos** (Nome, E-mail e Senha) e tente novamente.";
                return View(usuario);
            }

            // Lógica de Criação
            try
            {
                usuario.Id_Nivel = 3;

                // Aplica o Hash na Senha
                usuario.Senha_Usuario = BCrypt.Net.BCrypt.HashPassword(usuario.Senha_Usuario);

                _context.Add(usuario);
                await _context.SaveChangesAsync();

                TempData["MensagemSucesso"] = $"O Administrador '{usuario.Nome_Usuario}' foi criado com sucesso!";

                if (!jaExistemUsuarios)
                {
                    return RedirectToRoute(new { controller = "Autenticacao", action = "Login" });
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Este catch pegará erros de banco de dados, como violação de chave única (ex: e-mail já existe)
                TempData["MensagemErro"] = $"Falha ao criar o Administrador. Possível erro: E-mail já em uso ou dados inválidos para o banco. Erro: {ex.Message}";
            }

            // Retorna a view com os dados preenchidos em caso de erro no Try-Catch ou validação manual
            return View(usuario);
        }
    }
}