using Farol_Seguro.Config;
using Farol_Seguro.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using System; // Importação necessária para usar Exception

namespace Farol_Seguro.Controllers
{
    [Authorize]
    public class FuncionarioController : Controller
    {
        private readonly DbConfig _context;

        public FuncionarioController(DbConfig context)
        {
            _context = context;
        }

        // GET: Funcionario
        public async Task<IActionResult> Index()
        {
            // O tratamento de TempData na Index será feito na View, removendo a necessidade do ViewBag.
            var funcionarios = _context.Funcionarios
                .Include(f => f.Cargo)
                .Include(f => f.Nivel);

            return View(await funcionarios.ToListAsync());
        }

        // GET: Funcionario/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var funcionario = await _context.Funcionarios
                .Include(f => f.Cargo)
                .Include(f => f.Nivel)
                .FirstOrDefaultAsync(m => m.Id_Funcionario == id);

            if (funcionario == null) return NotFound();
            return View(funcionario);
        }

        // GET: Funcionario/Create
        public IActionResult Criar()
        {
            ViewData["Id_Cargo"] = new SelectList(_context.Cargos, "Id_Cargo", "Nome_Cargo");
            return View();
        }

        // POST: Funcionario/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar([Bind("Nome_Funcionario,Email_Funcionario,Senha_Funcionario,Departamento_Funcionario,Id_Cargo")] Funcionario funcionario)
        {
            try
            {
                // 1. Define o Id_Nivel fixo para Funcionario = 2
                funcionario.Id_Nivel = 2;

                // 2. Criptografa a senha antes de salvar
                if (!string.IsNullOrEmpty(funcionario.Senha_Funcionario))
                {
                    funcionario.Senha_Funcionario = BCrypt.Net.BCrypt.HashPassword(funcionario.Senha_Funcionario);
                }

                _context.Add(funcionario);
                await _context.SaveChangesAsync();

                TempData["MensagemSucesso"] = $"O funcionário '{funcionario.Nome_Funcionario}' foi criado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Em caso de erro (ex: violação de constraint no banco)
                TempData["MensagemErro"] = $"Falha ao criar o funcionário. Erro: {ex.Message}";
            }

            // Retorna à View em caso de erro para que o usuário possa corrigir
            ViewData["Id_Cargo"] = new SelectList(_context.Cargos, "Id_Cargo", "Nome_Cargo", funcionario.Id_Cargo);
            return View(funcionario);
        }

        // GET: Funcionario/Edit/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null) return NotFound();
            var funcionario = await _context.Funcionarios.FindAsync(id);
            if (funcionario == null) return NotFound();

            ViewData["Id_Cargo"] = new SelectList(_context.Cargos, "Id_Cargo", "Nome_Cargo", funcionario.Id_Cargo);

            // Limpa o campo de senha para não enviá-la para a view
            funcionario.Senha_Funcionario = string.Empty;

            return View(funcionario);
        }

        // POST: Funcionario/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, [Bind("Id_Funcionario,Nome_Funcionario,Email_Funcionario,Senha_Funcionario,Departamento_Funcionario,Id_Cargo")] Funcionario funcionario)
        {
            if (id != funcionario.Id_Funcionario) return NotFound();

            try
            {
                // Busca o funcionário existente para atualização
                var funcionarioParaAtualizar = await _context.Funcionarios.AsNoTracking().FirstOrDefaultAsync(f => f.Id_Funcionario == id);

                if (funcionarioParaAtualizar == null)
                {
                    TempData["MensagemErro"] = "Erro: Funcionário não encontrado para atualização.";
                    return NotFound();
                }

                // Mapeia os novos valores
                funcionarioParaAtualizar.Nome_Funcionario = funcionario.Nome_Funcionario;
                funcionarioParaAtualizar.Email_Funcionario = funcionario.Email_Funcionario;
                funcionarioParaAtualizar.Departamento_Funcionario = funcionario.Departamento_Funcionario;
                funcionarioParaAtualizar.Id_Cargo = funcionario.Id_Cargo;

                // Garante que o nível seja 2
                funcionarioParaAtualizar.Id_Nivel = 2;

                // Lógica de atualização de Senha: Só faz o HASH se o campo Senha_Funcionario tiver sido preenchido
                if (!string.IsNullOrWhiteSpace(funcionario.Senha_Funcionario))
                {
                    funcionarioParaAtualizar.Senha_Funcionario = BCrypt.Net.BCrypt.HashPassword(funcionario.Senha_Funcionario);
                }
                else
                {
                    // Se a senha estiver vazia, busca a senha antiga no banco
                    var senhaAntiga = await _context.Funcionarios
                        .Where(f => f.Id_Funcionario == id)
                        .Select(f => f.Senha_Funcionario)
                        .SingleOrDefaultAsync();

                    funcionarioParaAtualizar.Senha_Funcionario = senhaAntiga;
                }

                // Anexa e marca o objeto como modificado
                _context.Entry(funcionarioParaAtualizar).State = EntityState.Modified;

                await _context.SaveChangesAsync();

                TempData["MensagemSucesso"] = $"O funcionário '{funcionario.Nome_Funcionario}' foi atualizado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FuncionarioExists(funcionario.Id_Funcionario))
                {
                    TempData["MensagemErro"] = "Erro de concorrência: Funcionário não encontrado.";
                    return NotFound();
                }
                else throw;
            }
            catch (Exception ex)
            {
                TempData["MensagemErro"] = $"Falha ao atualizar o funcionário. Erro: {ex.Message}";
            }

            // Retorna à View em caso de erro para que o usuário possa corrigir
            ViewData["Id_Cargo"] = new SelectList(_context.Cargos, "Id_Cargo", "Nome_Cargo", funcionario.Id_Cargo);
            return View(funcionario);
        }

        // GET: Funcionario/Delete/5
        public async Task<IActionResult> Deletar(int? id)
        {
            if (id == null) return NotFound();
            var funcionario = await _context.Funcionarios
                .Include(f => f.Cargo)
                .FirstOrDefaultAsync(m => m.Id_Funcionario == id);

            if (funcionario == null) return NotFound();
            return View(funcionario);
        }

        // POST: Funcionario/Delete/5
        [HttpPost, ActionName("Deletar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletarConfirmado(int id)
        {
            var funcionario = await _context.Funcionarios.FindAsync(id);

            if (funcionario == null)
            {
                TempData["MensagemErro"] = "Erro: Funcionário não encontrado para exclusão.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Funcionarios.Remove(funcionario);
                await _context.SaveChangesAsync();

                TempData["MensagemSucesso"] = $"O funcionário '{funcionario.Nome_Funcionario}' foi excluído com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Captura erro de banco (ex: violação de chave estrangeira)
                TempData["MensagemErro"] = $"Falha ao excluir o funcionário. Erro: {ex.Message}";
                return RedirectToAction(nameof(Deletar), new { id }); // Retorna para a tela de exclusão com erro
            }
        }

        private bool FuncionarioExists(int id)
        {
            return _context.Funcionarios.Any(e => e.Id_Funcionario == id);
        }
    }
}