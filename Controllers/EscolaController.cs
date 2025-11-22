using Farol_Seguro.Config;
using Farol_Seguro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Farol_Seguro.Controllers
{
    [Authorize]
    public class EscolaController : Controller
    {
        private readonly DbConfig _context;

        public EscolaController(DbConfig context)
        {
            _context = context;
        }

        // GET: Escola
        public async Task<IActionResult> Index()
        {
            var escolas = _context.Escolas.Include(e => e.Cidade).Include(e => e.Funcionario);
            return View(await escolas.ToListAsync());
        }

        // GET: Escola/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var escola = await _context.Escolas
                .Include(e => e.Cidade)
                .Include(e => e.Funcionario)
                .FirstOrDefaultAsync(m => m.Id_Escola == id);
            if (escola == null) return NotFound();
            return View(escola);
        }

        // GET: Escola/Create
        public IActionResult Criar()
        {
            ViewData["Id_Cidade"] = new SelectList(_context.Cidades, "Id_Cidade", "Nome_Cidade");
            ViewData["Id_Funcionario"] = new SelectList(_context.Funcionarios, "Id_Funcionario", "Nome_Funcionario");
            return View();
        }

        // POST: Escola/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Escola escola)
        {
            // O bloco ModelState.IsValid foi removido.
            try
            {
                _context.Add(escola);
                await _context.SaveChangesAsync();
                TempData["MensagemSucesso"] = $"A escola '{escola.Nome_Escola}' foi cadastrada com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Captura erros de banco (ex: violação de constraint, validação de dados)
                TempData["MensagemErro"] = $"Falha ao cadastrar a escola. Erro: {ex.Message}";
            }

            // Se houver erro, retorna à View com dados
            ViewData["Id_Cidade"] = new SelectList(_context.Cidades, "Id_Cidade", "Nome_Cidade", escola.Id_Cidade);
            ViewData["Id_Funcionario"] = new SelectList(_context.Funcionarios, "Id_Funcionario", "Nome_Funcionario", escola.Id_Funcionario);
            return View(escola);
        }

        // GET: Escola/Edit/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null) return NotFound();
            var escola = await _context.Escolas.FindAsync(id);
            if (escola == null) return NotFound();
            ViewData["Id_Cidade"] = new SelectList(_context.Cidades, "Id_Cidade", "Nome_Cidade", escola.Id_Cidade);
            ViewData["Id_Funcionario"] = new SelectList(_context.Funcionarios, "Id_Funcionario", "Nome_Funcionario", escola.Id_Funcionario);
            return View(escola);
        }

        // POST: Escola/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Escola escola)
        {
            if (id != escola.Id_Escola) return NotFound();

            // O bloco ModelState.IsValid foi removido.
            try
            {
                _context.Update(escola);
                await _context.SaveChangesAsync();
                TempData["MensagemSucesso"] = $"A escola '{escola.Nome_Escola}' foi atualizada com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EscolaExists(escola.Id_Escola))
                {
                    TempData["MensagemErro"] = "Erro: Escola não encontrada durante a atualização.";
                    return NotFound();
                }
                else throw;
            }
            catch (Exception ex)
            {
                TempData["MensagemErro"] = $"Ocorreu um erro ao editar a escola. Erro: {ex.Message}";
            }

            // Se houver erro, retorna à View
            ViewData["Id_Cidade"] = new SelectList(_context.Cidades, "Id_Cidade", "Nome_Cidade", escola.Id_Cidade);
            ViewData["Id_Funcionario"] = new SelectList(_context.Funcionarios, "Id_Funcionario", "Nome_Funcionario", escola.Id_Funcionario);
            return View(escola);
        }

        // GET: Escola/Delete/5
        public async Task<IActionResult> Deletar(int? id)
        {
            if (id == null) return NotFound();

            var escola = await _context.Escolas
                .Include(e => e.Cidade)
                .Include(e => e.Funcionario)
                .FirstOrDefaultAsync(m => m.Id_Escola == id);

            if (escola == null) return NotFound();

            return View(escola);
        }

        // POST: Escola/Delete/5
        [HttpPost, ActionName("Deletar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var escola = await _context.Escolas.FindAsync(id);

            if (escola != null)
            {
                try
                {
                    _context.Escolas.Remove(escola);
                    await _context.SaveChangesAsync();
                    TempData["MensagemSucesso"] = $"A escola '{escola.Nome_Escola}' foi excluída com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["MensagemErro"] = $"Ocorreu um erro ao excluir a escola: {ex.Message}";
                    return RedirectToAction(nameof(Deletar), new { id });
                }
            }

            TempData["MensagemErro"] = "Erro: A escola que você tentou excluir não foi encontrada.";
            return RedirectToAction(nameof(Index));
        }

        private bool EscolaExists(int id)
        {
            return _context.Escolas.Any(e => e.Id_Escola == id);
        }
    }
}