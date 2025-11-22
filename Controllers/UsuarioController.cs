using Farol_Seguro.Config;
using Farol_Seguro.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using BCrypt.Net;
using System;
using Microsoft.AspNetCore.Authorization; // Adicionado para [Authorize] e [AllowAnonymous]

namespace Farol_Seguro.Controllers
{
    // APLICA [Authorize] EM TODOS OS MÉTODOS POR PADRÃO
    [Authorize]
    public class UsuarioController : Controller
    {
        private readonly DbConfig _context;

        public UsuarioController(DbConfig context)
        {
            _context = context;
        }

        // GET: Usuario
        // Requer autenticação, pois [Authorize] está no nível do Controller
        public async Task<IActionResult> Index()
        {
            var usuarios = _context.Usuarios.Include(u => u.Nivel);
            return View(await usuarios.ToListAsync());
        }

        // GET: Usuario/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var usuario = await _context.Usuarios
                .Include(u => u.Nivel)
                .FirstOrDefaultAsync(m => m.Id_Usuario == id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        // GET: Usuario/Create
        // PERMITE ACESSO SEM AUTENTICAÇÃO
        [AllowAnonymous]
        public IActionResult Criar()
        {
            return View();
        }

        // POST: Usuario/Create
        // PERMITE ACESSO SEM AUTENTICAÇÃO
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Usuario usuario)
        {
            try
            {
                // 1. Força o Nível = 3 (Admin)
                usuario.Id_Nivel = 3;

                // 2. Hashing da senha
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
                // Em caso de erro (ex: violação de constraint de e-mail único no banco)
                TempData["MensagemErro"] = $"Falha ao criar o Administrador. Erro: {ex.Message}";
            }

            // Retorna à View em caso de erro
            return View(usuario);
        }

        // GET: Usuario/Edit/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null) return NotFound();
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            // Limpa o campo de senha para não expor o hash na View
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
                // Busca o usuário existente (sem rastreamento para evitar conflitos de ID)
                var usuarioParaAtualizar = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id_Usuario == id);

                if (usuarioParaAtualizar == null)
                {
                    TempData["MensagemErro"] = "Erro: Administrador não encontrado para atualização.";
                    return NotFound();
                }

                // 1. Atualiza os campos que vieram do formulário
                usuarioParaAtualizar.Nome_Usuario = usuario.Nome_Usuario;
                usuarioParaAtualizar.Email_Usuario = usuario.Email_Usuario;

                // 2. Garante que o Nível permaneça 3
                usuarioParaAtualizar.Id_Nivel = 3;

                // 3. Lógica de senha: Só atualiza/faz Hash se uma nova senha for fornecida
                if (!string.IsNullOrWhiteSpace(usuario.Senha_Usuario))
                {
                    usuarioParaAtualizar.Senha_Usuario = BCrypt.Net.BCrypt.HashPassword(usuario.Senha_Usuario);
                }
                else
                {
                    // Se a senha estiver vazia, busca a senha antiga no banco para manter
                    var senhaAntiga = await _context.Usuarios
                        .Where(u => u.Id_Usuario == id)
                        .Select(u => u.Senha_Usuario)
                        .SingleOrDefaultAsync();

                    usuarioParaAtualizar.Senha_Usuario = senhaAntiga;
                }

                // 4. Anexa o objeto com a senha (nova ou antiga) e marca como modificado
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

            // Retorna à View em caso de erro
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
                // Captura erro de banco (ex: violação de chave estrangeira, se houver)
                TempData["MensagemErro"] = $"Falha ao excluir o Administrador. Erro: {ex.Message}";
                return RedirectToAction(nameof(Deletar), new { id }); // Retorna para a tela de exclusão com erro
            }
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.Id_Usuario == id);
        }
    }
}