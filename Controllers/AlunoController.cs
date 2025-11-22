using Farol_Seguro.Config;
using Farol_Seguro.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System; // Necessário para Exception
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;

namespace Farol_Seguro.Controllers
{
    // Adicionando Authorize, presumindo que o gerenciamento de alunos é restrito
    [Authorize]
    public class AlunoController : Controller
    {
        private readonly DbConfig _context;

        public AlunoController(DbConfig context)
        {
            _context = context;
        }

        // GET: Aluno
        public async Task<IActionResult> Index()
        {
            var alunos = await _context.Alunos.ToListAsync();
            return View(alunos);
        }

        // GET: Aluno/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var aluno = await _context.Alunos.FirstOrDefaultAsync(a => a.Id_Aluno == id);
            if (aluno == null) return NotFound();

            return View(aluno);
        }

        // GET: Aluno/Create
        [AllowAnonymous] // Permite criação de aluno (registro) sem login
        public IActionResult Criar()
        {
            return View();
        }

        // POST: Aluno/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous] // Permite criação de aluno (registro) sem login
        public async Task<IActionResult> Criar(Aluno aluno)
        {
            try
            {
                if (aluno == null)
                {
                    TempData["MensagemErro"] = "Dados do aluno inválidos.";
                    return RedirectToAction(nameof(Criar));
                }

                aluno.Id_Nivel = 1; // Define nível do aluno (Manter a regra de negócio)

                // Criptografa a senha, se fornecida
                if (!string.IsNullOrWhiteSpace(aluno.Senha_Aluno))
                    aluno.Senha_Aluno = BCrypt.Net.BCrypt.HashPassword(aluno.Senha_Aluno);

                _context.Add(aluno);
                await _context.SaveChangesAsync();

                TempData["MensagemSucesso"] = $"Registro do aluno **{aluno.Nome_Aluno}** realizado com sucesso! Faça login.";

                // Redireciona para o Login (Autenticacao) após o registro
                return RedirectToAction("Login", "Autenticacao");
            }
            catch (Exception ex)
            {
                // Captura erro de validação (se o modelo for inválido) ou erro de BD
                TempData["MensagemErro"] = $"Erro ao registrar aluno. Detalhes: {ex.Message}";
                // Retorna a view para que os dados preenchidos não sejam perdidos
                return View(aluno);
            }
        }

        // GET: Aluno/Edit/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null) return NotFound();

            var aluno = await _context.Alunos.FindAsync(id);
            if (aluno == null) return NotFound();

            return View(aluno);
        }

        // POST: Aluno/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Aluno aluno)
        {
            if (id != aluno.Id_Aluno) return NotFound();

            // O MVC fará a validação, se falhar, o bloco try-catch pode não ser alcançado,
            // ou a exceção será capturada se houver falha de persistência.
            try
            {
                var alunoBD = await _context.Alunos.FirstOrDefaultAsync(a => a.Id_Aluno == id);
                if (alunoBD == null)
                {
                    TempData["MensagemErro"] = "Aluno não encontrado para edição.";
                    return RedirectToAction(nameof(Index));
                }

                // Atualiza campos editáveis
                alunoBD.Nome_Aluno = aluno.Nome_Aluno;
                alunoBD.Email_Aluno = aluno.Email_Aluno;
                alunoBD.Genero_Aluno = aluno.Genero_Aluno;
                alunoBD.DataNascimento_Aluno = aluno.DataNascimento_Aluno;

                // Se senha foi alterada, criptografa
                if (!string.IsNullOrWhiteSpace(aluno.Senha_Aluno))
                    alunoBD.Senha_Aluno = BCrypt.Net.BCrypt.HashPassword(aluno.Senha_Aluno);

                _context.Update(alunoBD); // Usamos update no objeto rastreado (alunoBD)
                await _context.SaveChangesAsync();

                TempData["MensagemSucesso"] = $"Dados do aluno **{aluno.Nome_Aluno}** atualizados com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AlunoExists(aluno.Id_Aluno))
                {
                    TempData["MensagemErro"] = "Aluno não encontrado ou conflito de concorrência.";
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                TempData["MensagemErro"] = $"Erro ao editar aluno. Detalhes: {ex.Message}";
                return View(aluno); // Retorna a view para que os dados preenchidos não sejam perdidos
            }
        }

        // POST: Aluno/Delete/5 (Modal de exclusão geralmente chama este método)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deletar(int id) // Renomeado para Deletar
        {
            var aluno = await _context.Alunos.FindAsync(id);

            if (aluno != null)
            {
                try
                {
                    var nomeAluno = aluno.Nome_Aluno;
                    _context.Alunos.Remove(aluno);
                    await _context.SaveChangesAsync();
                    TempData["MensagemSucesso"] = $"Aluno **{nomeAluno}** excluído com sucesso!";
                }
                catch (Exception ex)
                {
                    TempData["MensagemErro"] = $"Erro ao excluir aluno. Detalhes: {ex.Message}";
                }
            }
            else
            {
                TempData["MensagemErro"] = "Aluno não encontrado para exclusão.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Aluno/Delete/5
        public async Task<IActionResult> Deletar(int? id)
        {
            // Mantido, mas provavelmente não será usado se você usar modal no Index
            if (id == null) return NotFound();
            var aluno = await _context.Alunos.FirstOrDefaultAsync(a => a.Id_Aluno == id);
            if (aluno == null) return NotFound();
            return View(aluno);
        }

        private bool AlunoExists(int id)
        {
            return _context.Alunos.Any(e => e.Id_Aluno == id);
        }
    }
}