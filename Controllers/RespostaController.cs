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
    public class RespostaController : Controller
    {
        private readonly DbConfig _context;

        public RespostaController(DbConfig context)
        {
            _context = context;
        }

        // GET: Resposta
        public async Task<IActionResult> Index()
        {
            var respostas = _context.Respostas.Include(r => r.Denuncia);
            return View(await respostas.ToListAsync());
        }

        // GET: Resposta/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var resposta = await _context.Respostas
                .Include(r => r.Denuncia)
                .FirstOrDefaultAsync(m => m.Id_Resposta == id);
            if (resposta == null) return NotFound();
            return View(resposta);
        }

        // GET: Resposta/Create
        public IActionResult Criar()
        {
            ViewData["Id_Denuncia"] = new SelectList(_context.Denuncias, "Id_Denuncia", "Descricao_Denuncia");
            return View();
        }

        // POST: Resposta/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Resposta resposta)
        {
            
                _context.Add(resposta);
                await _context.SaveChangesAsync();
                
            ViewData["Id_Denuncia"] = new SelectList(_context.Denuncias, "Id_Denuncia", "Descricao_Denuncia", resposta.Id_Denuncia);
            return View(resposta);
        }

        // GET: Resposta/Edit/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null) return NotFound();
            var resposta = await _context.Respostas.FindAsync(id);
            if (resposta == null) return NotFound();
            ViewData["Id_Denuncia"] = new SelectList(_context.Denuncias, "Id_Denuncia", "Descricao_Denuncia", resposta.Id_Denuncia);
            return View(resposta);
        }

        // POST: Resposta/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Resposta resposta)
        {
            if (id != resposta.Id_Resposta) return NotFound();

            
                try
                {
                    _context.Update(resposta);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RespostaExists(resposta.Id_Resposta)) return NotFound();
                    else throw;
                }
               
            ViewData["Id_Denuncia"] = new SelectList(_context.Denuncias, "Id_Denuncia", "Descricao_Denuncia", resposta.Id_Denuncia);
            return View(resposta);
        }

        // GET: Resposta/Delete/5
        public async Task<IActionResult> Deletar(int? id)
        {
            if (id == null) return NotFound();
            var resposta = await _context.Respostas
                .Include(r => r.Denuncia)
                .FirstOrDefaultAsync(m => m.Id_Resposta == id);
            if (resposta == null) return NotFound();
            return View(resposta);
        }

        // POST: Resposta/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var resposta = await _context.Respostas.FindAsync(id);
            _context.Respostas.Remove(resposta);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RespostaExists(int id)
        {
            return _context.Respostas.Any(e => e.Id_Resposta == id);
        }
    }
}