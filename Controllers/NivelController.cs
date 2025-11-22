using Farol_Seguro.Config;
using Farol_Seguro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Farol_Seguro.Controllers
{
   
    public class NivelController : Controller
    {
        private readonly DbConfig _context;

        public NivelController(DbConfig context)
        {
            _context = context;
        }

        // GET: Nivel
        public async Task<IActionResult> Index()
        {
            return View(await _context.Niveis.ToListAsync());
        }

        // GET: Nivel/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var nivel = await _context.Niveis.FirstOrDefaultAsync(m => m.Id_Nivel == id);
            if (nivel == null) return NotFound();
            return View(nivel);
        }

        // GET: Nivel/Create
        public IActionResult Criar()
        {
            return View();
        }

        // POST: Nivel/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Nivel nivel)
        {
           
            
                _context.Add(nivel);
                await _context.SaveChangesAsync();
               
            
            return View(nivel);
        }

        // GET: Nivel/Edit/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null) return NotFound();
            var nivel = await _context.Niveis.FindAsync(id);
            if (nivel == null) return NotFound();
            return View(nivel);
        }

        // POST: Nivel/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Nivel nivel)
        {
            if (id != nivel.Id_Nivel) return NotFound();

            
                try
                {
                    _context.Update(nivel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NivelExists(nivel.Id_Nivel)) return NotFound();
                    else throw;
                }
              
            return View(nivel);
        }

        // GET: Nivel/Delete/5
        public async Task<IActionResult> Deletar(int? id)
        {
            if (id == null) return NotFound();
            var nivel = await _context.Niveis.FirstOrDefaultAsync(m => m.Id_Nivel == id);
            if (nivel == null) return NotFound();
            return View(nivel);
        }

        // POST: Nivel/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var nivel = await _context.Niveis.FindAsync(id);
            _context.Niveis.Remove(nivel);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool NivelExists(int id)
        {
            return _context.Niveis.Any(e => e.Id_Nivel == id);
        }
    }
}