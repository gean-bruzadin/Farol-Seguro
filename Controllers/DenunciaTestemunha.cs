using Farol_Seguro.Config;
using Farol_Seguro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Farol_Seguro.Controllers
{
    [Authorize]
    public class DenunciaTestemunhaController : Controller
    {
        private readonly DbConfig _context;

        public DenunciaTestemunhaController(DbConfig context)
        {
            _context = context;
        }

        // GET: DenunciaTestemunha
        public async Task<IActionResult> Index()
        {
            var denunciaTestemunhas = _context.DenunciaTestemunhas.Include(d => d.Denuncias).Include(d => d.Testemunha);
            return View(await denunciaTestemunhas.ToListAsync());
        }

        // GET: DenunciaTestemunha/Details?idDenuncia=5&idTestemunha=1
        public async Task<IActionResult> Details(int? idDenuncia, int? idTestemunha)
        {
            if (idDenuncia == null || idTestemunha == null) return NotFound();

            var denunciaTestemunha = await _context.DenunciaTestemunhas
                .Include(d => d.Denuncias)
                .Include(d => d.Testemunha)
                .FirstOrDefaultAsync(m => m.Id_Denuncia == idDenuncia && m.Id_Testemunha == idTestemunha);

            if (denunciaTestemunha == null) return NotFound();

            return View(denunciaTestemunha);
        }

        // GET: DenunciaTestemunha/Create
        public IActionResult Criar()
        {
            ViewData["Id_Denuncia"] = new SelectList(_context.Denuncias, "Id_Denuncia", "Descricao_Denuncia");
            ViewData["Id_Testemunha"] = new SelectList(_context.Testemunhas, "Id_Testemunha", "Nome_Testemunha");
            return View();
        }

        // POST: DenunciaTestemunha/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(DenunciaTestemunha denunciaTestemunha)
        {
           
                _context.Add(denunciaTestemunha);
                await _context.SaveChangesAsync();
                
            
            ViewData["Id_Denuncia"] = new SelectList(_context.Denuncias, "Id_Denuncia", "Descricao_Denuncia", denunciaTestemunha.Id_Denuncia);
            ViewData["Id_Testemunha"] = new SelectList(_context.Testemunhas, "Id_Testemunha", "Nome_Testemunha", denunciaTestemunha.Id_Testemunha);
            return View(denunciaTestemunha);
        }

        // Não há um método de Edit simples para tabelas de junção. A prática comum é Deletar e Criar uma nova associação.
        // A edição direta é complexa porque envolve mudar a chave primária.

        // GET: DenunciaTestemunha/Delete?idDenuncia=5&idTestemunha=1
        public async Task<IActionResult> Deletar(int? idDenuncia, int? idTestemunha)
        {
            if (idDenuncia == null || idTestemunha == null) return NotFound();

            var denunciaTestemunha = await _context.DenunciaTestemunhas
                .Include(d => d.Denuncias)
                .Include(d => d.Testemunha)
                .FirstOrDefaultAsync(m => m.Id_Denuncia == idDenuncia && m.Id_Testemunha == idTestemunha);

            if (denunciaTestemunha == null) return NotFound();

            return View(denunciaTestemunha);
        }

        // POST: DenunciaTestemunha/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int idDenuncia, int idTestemunha)
        {
            var denunciaTestemunha = await _context.DenunciaTestemunhas.FindAsync(idDenuncia, idTestemunha);
            _context.DenunciaTestemunhas.Remove(denunciaTestemunha);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}