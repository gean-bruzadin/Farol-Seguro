using Farol_Seguro.Config;
using Farol_Seguro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Farol_Seguro.Controllers
{
    [Authorize]
    public class CidadeController : Controller
    {
        private readonly DbConfig _context;

        public CidadeController(DbConfig context)
        {
            _context = context;
        }

        // GET: Cidade
        public async Task<IActionResult> Index()
        {
            var cidades = _context.Cidades.Include(c => c.Estado);
            return View(await cidades.ToListAsync());
        }

        // GET: Cidade/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var cidade = await _context.Cidades
                .Include(c => c.Estado)
                .FirstOrDefaultAsync(c => c.Id_Cidade == id);

            if (cidade == null) return NotFound();

            return View(cidade);
        }

        // GET: Cidade/Create
        public IActionResult Criar()
        {
            ViewData["Id_Estado"] = new SelectList(_context.Estados, "Id_Estado", "Nome_Estado");
            return View();
        }

        // POST: Cidade/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Cidade cidade)
        {
            try
            {
                if (cidade != null)
                {
                    // Não checaremos ModelState.IsValid aqui para seguir o fluxo de erro via try-catch
                    _context.Add(cidade);
                    await _context.SaveChangesAsync();

                    TempData["MensagemSucesso"] = $"Cidade **{cidade.Nome_Cidade}** criada com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                TempData["MensagemErro"] = $"Erro ao criar a cidade **{cidade?.Nome_Cidade}**. Detalhes: {ex.Message}";
            }

            // Recria a SelectList em caso de erro para manter o dropdown preenchido
            ViewData["Id_Estado"] = new SelectList(_context.Estados, "Id_Estado", "Nome_Estado", cidade?.Id_Estado);
            return View(cidade);
        }

        // GET: Cidade/Edit/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null) return NotFound();

            var cidade = await _context.Cidades.FindAsync(id);
            if (cidade == null) return NotFound();

            ViewData["Id_Estado"] = new SelectList(_context.Estados, "Id_Estado", "Nome_Estado", cidade.Id_Estado);
            return View(cidade);
        }

        // POST: Cidade/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Cidade cidade)
        {
            if (id != cidade.Id_Cidade) return NotFound();

            try
            {
                _context.Update(cidade);
                await _context.SaveChangesAsync();

                TempData["MensagemSucesso"] = $"Cidade **{cidade.Nome_Cidade}** atualizada com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CidadeExists(cidade.Id_Cidade))
                {
                    TempData["MensagemErro"] = "Cidade não encontrada ou conflito de concorrência.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    throw; // Lança exceção para outros erros de concorrência
                }
            }
            catch (Exception ex)
            {
                TempData["MensagemErro"] = $"Erro ao editar a cidade **{cidade.Nome_Cidade}**. Detalhes: {ex.Message}";
                // Recria a SelectList em caso de erro
                ViewData["Id_Estado"] = new SelectList(_context.Estados, "Id_Estado", "Nome_Estado", cidade.Id_Estado);
                return View(cidade);
            }
        }

        // GET: Cidade/Delete/5
        public async Task<IActionResult> Deletar(int? id)
        {
            if (id == null) return NotFound();

            var cidade = await _context.Cidades
                .Include(c => c.Estado)
                .FirstOrDefaultAsync(c => c.Id_Cidade == id);

            if (cidade == null) return NotFound();

            return View(cidade);
        }

        // POST: Cidade/Delete/5 (Usamos Deletar no Controller para simplificar a View)
        [HttpPost, ActionName("DeleteConfirmed")] // Mantendo o ActionName original do formulário
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cidade = await _context.Cidades.FindAsync(id);

            if (cidade != null)
            {
                try
                {
                    var nomeCidade = cidade.Nome_Cidade;
                    _context.Cidades.Remove(cidade);
                    await _context.SaveChangesAsync();
                    TempData["MensagemSucesso"] = $"Cidade **{nomeCidade}** excluída com sucesso!";
                }
                catch (Exception ex)
                {
                    // Erro de FK ou outro erro de BD na exclusão
                    TempData["MensagemErro"] = $"Erro ao excluir a cidade **{cidade.Nome_Cidade}**. Detalhes: {ex.Message}";
                }
            }
            else
            {
                TempData["MensagemErro"] = "Cidade não encontrada para exclusão.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CidadeExists(int id)
        {
            return _context.Cidades.Any(c => c.Id_Cidade == id);
        }
    }
}