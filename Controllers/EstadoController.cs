using Farol_Seguro.Config;
using Farol_Seguro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Farol_Seguro.Controllers
{
    [Authorize]
    public class EstadoController : Controller
    {
        private readonly DbConfig _context;

        public EstadoController(DbConfig context)
        {
            _context = context;
        }

        // GET: Estado
        public async Task<IActionResult> Index()
        {
            return View(await _context.Estados.ToListAsync());
        }

        // GET: Estado/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var estado = await _context.Estados.FirstOrDefaultAsync(m => m.Id_Estado == id);
            if (estado == null) return NotFound();
            return View(estado);
        }

        // GET: Estado/Criar
        public IActionResult Criar()
        {
            return View();
        }

        // POST: Estado/Criar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Estado estado)
        {
            // O código de validação explícita foi removido conforme solicitado.
            // Erros de validação de modelo (Model State) ou erros de banco de dados
            // agora cairão diretamente no bloco catch, redirecionando para Index com erro.
            try
            {
                _context.Add(estado);
                await _context.SaveChangesAsync();
                TempData["MensagemSucesso"] = $"Estado '{estado.Nome_Estado}' criado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Se houver falha de validação ou de banco, o erro será exibido aqui.
                TempData["MensagemErro"] = $"Erro ao criar estado. Detalhes: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Estado/Editar/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null) return NotFound();
            var estado = await _context.Estados.FindAsync(id);
            if (estado == null) return NotFound();
            return View(estado);
        }

        // POST: Estado/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Estado estado)
        {
            if (id != estado.Id_Estado) return NotFound();

            // O código de validação explícita foi removido conforme solicitado.

            try
            {
                _context.Update(estado);
                await _context.SaveChangesAsync();
                TempData["MensagemSucesso"] = $"Estado '{estado.Nome_Estado}' atualizado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EstadoExists(estado.Id_Estado))
                {
                    TempData["MensagemErro"] = "Estado não encontrado durante a atualização.";
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                // Se houver falha de validação ou de banco, o erro será exibido aqui.
                TempData["MensagemErro"] = $"Erro ao atualizar estado. Detalhes: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Estado/Deletar/5
        public async Task<IActionResult> Deletar(int? id)
        {
            if (id == null) return NotFound();
            var estado = await _context.Estados.FirstOrDefaultAsync(m => m.Id_Estado == id);
            if (estado == null) return NotFound();
            return View(estado);
        }

        // POST: Estado/Deletar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deletar(int id) // <-- Mudança aqui
        {
            var estado = await _context.Estados.FindAsync(id);

            if (estado != null)
            {
                try
                {
                    var nomeEstado = estado.Nome_Estado;
                    _context.Estados.Remove(estado);
                    await _context.SaveChangesAsync();
                    TempData["MensagemSucesso"] = $"Estado '{nomeEstado}' excluído com sucesso!";
                }
                catch (Exception ex)
                {
                    TempData["MensagemErro"] = $"Erro ao excluir estado: {ex.Message}";
                }
            }
            else
            {
                TempData["MensagemErro"] = "Estado não encontrado para exclusão.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool EstadoExists(int id)
        {
            return _context.Estados.Any(e => e.Id_Estado == id);
        }
    }
}