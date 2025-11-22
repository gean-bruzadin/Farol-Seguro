using Farol_Seguro.Config;
using Farol_Seguro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Farol_Seguro.Controllers
{
    [Authorize]
    public class TestemunhaController : Controller
    {
        private readonly DbConfig _context;

        public TestemunhaController(DbConfig context)
        {
            _context = context;
        }

        // GET: Testemunha
        public async Task<IActionResult> Index()
        {
            return View(await _context.Testemunhas.ToListAsync());
        }

        // GET: Testemunha/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var testemunha = await _context.Testemunhas.FirstOrDefaultAsync(m => m.Id_Testemunha == id);
            if (testemunha == null) return NotFound();
            return View(testemunha);
        }

        // GET: Testemunha/Create
        public IActionResult Criar()
        {
            return View();
        }

        // POST: Testemunha/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Testemunha testemunha)
        {
            
                _context.Add(testemunha);
                await _context.SaveChangesAsync();
               
            return View(testemunha);
        }

        // GET: Testemunha/Edit/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null) return NotFound();
            var testemunha = await _context.Testemunhas.FindAsync(id);
            if (testemunha == null) return NotFound();
            return View(testemunha);
        }

        // POST: Testemunha/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Testemunha testemunha)
        {
            if (id != testemunha.Id_Testemunha) return NotFound();

           
                try
                {
                    _context.Update(testemunha);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TestemunhaExists(testemunha.Id_Testemunha)) return NotFound();
                    else throw;
                }
               
            return View(testemunha);
        }

        // GET: Testemunha/Delete/5
        public async Task<IActionResult> Deletar(int? id)
        {
            if (id == null) return NotFound();
            var testemunha = await _context.Testemunhas.FirstOrDefaultAsync(m => m.Id_Testemunha == id);
            if (testemunha == null) return NotFound();
            return View(testemunha);
        }

        // POST: Testemunha/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var testemunha = await _context.Testemunhas.FindAsync(id);
            _context.Testemunhas.Remove(testemunha);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TestemunhaExists(int id)
        {
            return _context.Testemunhas.Any(e => e.Id_Testemunha == id);
        }
    }
}