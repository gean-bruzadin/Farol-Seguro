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
    public class AnexoController : Controller
    {
        private readonly DbConfig _context;

        public AnexoController(DbConfig context)
        {
            _context = context;
        }

        // GET: Anexo
        public async Task<IActionResult> Index()
        {
            var anexos = _context.Anexos.Include(a => a.Denuncia);
            return View(await anexos.ToListAsync());
        }

        // GET: Anexo/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var anexo = await _context.Anexos
                .Include(a => a.Denuncia)
                .FirstOrDefaultAsync(m => m.Id_Anexo == id);
            if (anexo == null) return NotFound();
            return View(anexo);
        }

        // GET: Anexo/Create
        public IActionResult Criar()
        {
            ViewData["Id_Denuncia"] = new SelectList(_context.Denuncias, "Id_Denuncia", "Descricao_Denuncia");
            return View();
        }

        // POST: Anexo/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Anexo anexo)
        {
            
            
                _context.Add(anexo);
                await _context.SaveChangesAsync();
               
            
                ViewData["Id_Denuncia"] = new SelectList(_context.Denuncias, "Id_Denuncia", "Descricao_Denuncia", anexo.Id_Denuncia);
                 return View(anexo);
        }

        // GET: Anexo/Edit/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null) return NotFound();
            var anexo = await _context.Anexos.FindAsync(id);
            if (anexo == null) return NotFound();
            ViewData["Id_Denuncia"] = new SelectList(_context.Denuncias, "Id_Denuncia", "Descricao_Denuncia", anexo.Id_Denuncia);
            return View(anexo);
        }

        // POST: Anexo/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>Editar(int id, Anexo anexo)
        {
            if (id != anexo.Id_Anexo) return NotFound();

           
            
                try
                {
                    _context.Update(anexo);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AnexoExists(anexo.Id_Anexo)) return NotFound();
                    else throw;
                }
              
            
            ViewData["Id_Denuncia"] = new SelectList(_context.Denuncias, "Id_Denuncia", "Descricao_Denuncia", anexo.Id_Denuncia);
            return View(anexo);
        }

        // GET: Anexo/Delete/5
        public async Task<IActionResult> Deletar(int? id)
        {
            if (id == null) return NotFound();
            var anexo = await _context.Anexos
                .Include(a => a.Denuncia)
                .FirstOrDefaultAsync(m => m.Id_Anexo == id);
            if (anexo == null) return NotFound();
            return View(anexo);
        }

        // POST: Anexo/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var anexo = await _context.Anexos.FindAsync(id);
            _context.Anexos.Remove(anexo);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AnexoExists(int id)
        {
            return _context.Anexos.Any(e => e.Id_Anexo == id);
        }
    }
}