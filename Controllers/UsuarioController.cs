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
using Microsoft.Extensions.Options; // NOVO

namespace Farol_Seguro.Controllers
{
    [Authorize]
    public class UsuarioController : Controller
    {
        // VARIÁVEL PARA GUARDAR A CHAVE INJETADA DO appsettings.json
        private readonly string _codigoSecreto;
        private readonly DbConfig _context;

        // NOVO CONSTRUTOR: Injetando DbConfig e IOptions para a configuração
        public UsuarioController(DbConfig context, IOptions<AcessoAdminConfig> acessoAdminConfig)
        {
            _context = context;
            // Carrega o valor do appsettings.json
            _codigoSecreto = acessoAdminConfig.Value.CodigoSecreto;
        }

        // --- AÇÕES PROTEGIDAS POR [Authorize] (PADRÃO DO CONTROLLER) ---

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

        // GET: Usuario/Edit/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null) return NotFound();
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            usuario.Senha_Usuario = string.Empty;

            return View(usuario);
        }

        // POST: Usuario/Edit/5
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

        // GET: Usuario/Delete/5
        public async Task<IActionResult> Deletar(int? id)
        {
            if (id == null) return NotFound();
            var usuario = await _context.Usuarios
                .Include(u => u.Nivel)
                .FirstOrDefaultAsync(m => m.Id_Usuario == id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        // POST: Usuario/Delete/5
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

        // GET: Usuario/AcessoAdmin (Nova tela para digitar o código)
        [AllowAnonymous]
        public async Task<IActionResult> AcessoAdmin()
        {
            // Se já existem usuários, redireciona para o login (fim do bootstrap)
            if (await _context.Usuarios.AnyAsync())
            {
                TempData["MensagemErro"] = "O registro por código secreto está desativado. Por favor, faça login com uma conta de administrador.";
                // Assume que a rota de Login é Conta/Login
                return RedirectToRoute(new { controller = "Conta", action = "Login" });
            }
            return View();
        }

        // POST: Usuario/AcessoAdmin - Checa o código
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcessoAdmin(string codigo)
        {
            // Se a tabela não estiver vazia, redireciona para o Login.
            if (await _context.Usuarios.AnyAsync())
            {
                TempData["MensagemErro"] = "O registro por código secreto está desativado. Por favor, faça login com uma conta de administrador.";
                return RedirectToRoute(new { controller = "Conta", action = "Login" });
            }

            // Compara com a chave injetada
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

            // 1. SE JÁ EXISTEM USUÁRIOS: Redireciona para Login
            if (jaExistemUsuarios)
            {
                TempData["MensagemErro"] = "Registro de novos administradores está bloqueado. Por favor, faça login com uma conta de administrador existente para adicionar mais.";
                return RedirectToRoute(new { controller = "Conta", action = "Login" });
            }

            // 2. SE É O PRIMEIRO REGISTRO (Bootstrap): Requer que o código secreto tenha sido inserido.
            if (TempData["AcessoPermitido"] == null || (bool)TempData["AcessoPermitido"] != true)
            {
                TempData["MensagemErro"] = "Você não pode acessar essa área. Insira o código de acesso de Administrador.";
                return RedirectToAction(nameof(AcessoAdmin));
            }

            return View();
        }


        // POST: Usuario/Create
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Usuario usuario)
        {
            bool jaExistemUsuarios = await _context.Usuarios.AnyAsync();

            if (jaExistemUsuarios)
            {
                // Se JÁ existem usuários, a criação SÓ pode ser feita por um admin LOGADO.
                if (!User.Identity.IsAuthenticated)
                {
                    TempData["MensagemErro"] = "Registro de novos administradores está desativado. Por favor, faça login com uma conta de Administrador existente para adicionar mais.";
                    return RedirectToRoute(new { controller = "Conta", action = "Login" });
                }
            }
            else
            {
                // Se é o PRIMEIRO USUÁRIO, exige que o código de acesso tenha sido inserido.
                if (TempData["AcessoPermitido"] == null || (bool)TempData["AcessoPermitido"] != true)
                {
                    TempData["MensagemErro"] = "Acesso negado. Necessário inserir o código de segurança antes de prosseguir.";
                    return RedirectToAction(nameof(AcessoAdmin));
                }
            }

            // Lógica de Criação
            try
            {
                usuario.Id_Nivel = 3;

                if (!string.IsNullOrEmpty(usuario.Senha_Usuario))
                {
                    usuario.Senha_Usuario = BCrypt.Net.BCrypt.HashPassword(usuario.Senha_Usuario);
                }

                _context.Add(usuario);
                await _context.SaveChangesAsync();

                TempData["MensagemSucesso"] = $"O Administrador '{usuario.Nome_Usuario}' foi criado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["MensagemErro"] = $"Falha ao criar o Administrador. Erro: {ex.Message}";
            }

            return View(usuario);
        }
    }
}