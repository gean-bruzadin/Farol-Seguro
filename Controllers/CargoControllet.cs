using Farol_Seguro.Config;
using Farol_Seguro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System; // Necessário para Exception
using System.Linq;
using System.Threading.Tasks;

namespace Farol_Seguro.Controllers
{
    [Authorize]
    public class CargoController : Controller
    {
        private readonly DbConfig _context;

        public CargoController(DbConfig context)
        {
            _context = context;
        }

        // GET: Cargo
        public async Task<IActionResult> Index()
        {
            return View(await _context.Cargos.ToListAsync());
        }

        // GET: Cargo/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var cargo = await _context.Cargos.FirstOrDefaultAsync(c => c.Id_Cargo == id);
            if (cargo == null) return NotFound();

            return View(cargo);
        }

        // GET: Cargo/Create
        public IActionResult Criar()
        {
            return View();
        }

        // POST: Cargo/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Cargo cargo)
        {
            try
            {
                if (cargo == null)
                {
                    TempData["MensagemErro"] = "Dados do cargo inválidos.";
                    return RedirectToAction(nameof(Index));
                }

                // Nenhuma checagem if (!ModelState.IsValid) conforme solicitado.
                // A falha de validação ou de BD fará o código cair no catch.
                _context.Add(cargo);
                await _context.SaveChangesAsync();

                TempData["MensagemSucesso"] = $"Cargo **{cargo.Nome_Cargo}** criado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["MensagemErro"] = $"Erro ao criar o cargo. Detalhes: {ex.Message}";
                // Retorna à view para não perder os dados preenchidos, mas com a mensagem TempData
                return View(cargo);
            }
        }

        // GET: Cargo/Edit/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null) return NotFound();

            var cargo = await _context.Cargos.FindAsync(id);
            if (cargo == null) return NotFound();

            return View(cargo);
        }

        // POST: Cargo/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Cargo cargo)
        {
            if (id != cargo.Id_Cargo) return NotFound();

            try
            {
                // Nenhuma checagem if (!ModelState.IsValid) conforme solicitado.
                _context.Update(cargo);
                await _context.SaveChangesAsync();

                TempData["MensagemSucesso"] = $"Cargo **{cargo.Nome_Cargo}** atualizado com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CargoExists(cargo.Id_Cargo))
                {
                    TempData["MensagemErro"] = "Cargo não encontrado ou conflito de concorrência.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                TempData["MensagemErro"] = $"Erro ao editar o cargo. Detalhes: {ex.Message}";
                // Retorna à view para não perder os dados preenchidos, mas com a mensagem TempData
                return View(cargo);
            }
        }

        // GET: Cargo/Delete/5
        // Mantido para compatibilidade, embora a modal vá lidar com a exclusão
        public async Task<IActionResult> Deletar(int? id)
        {
            if (id == null) return NotFound();

            var cargo = await _context.Cargos.FirstOrDefaultAsync(c => c.Id_Cargo == id);
            if (cargo == null) return NotFound();

            return View(cargo);
        }

        // POST: Cargo/Delete/5 (Renomeado para 'Deletar' para corresponder ao formulário POST da modal)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deletar(int id) // <-- Usando "Deletar" aqui
        {
            var cargo = await _context.Cargos.FindAsync(id);

            if (cargo != null)
            {
                try
                {
                    var nomeCargo = cargo.Nome_Cargo;
                    _context.Cargos.Remove(cargo);
                    await _context.SaveChangesAsync();
                    TempData["MensagemSucesso"] = $"Cargo **{nomeCargo}** excluído com sucesso!";
                }
                catch (Exception ex)
                {
                    // Erro de FK ou outro erro de BD na exclusão
                    TempData["MensagemErro"] = $"Erro ao excluir o cargo **{cargo.Nome_Cargo}**. Detalhes: {ex.Message}";
                }
            }
            else
            {
                TempData["MensagemErro"] = "Cargo não encontrado para exclusão.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CargoExists(int id)
        {
            return _context.Cargos.Any(c => c.Id_Cargo == id);
        }
    }
}