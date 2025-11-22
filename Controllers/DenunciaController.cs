using ClosedXML.Excel;
using Farol_Seguro.Config;
using Farol_Seguro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace Farol_Seguro.Controllers
{
    [Authorize]// obriga login em todas as ações
    public class DenunciaController : Controller
    {
        private readonly DbConfig _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DenunciaController(DbConfig context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // ============================================================== INDEX (FILTRO) =====================================================
        [Authorize(Roles = "Funcionario,Admin")]
        public async Task<IActionResult> Index(string status, string categoria, int? idBusca)
        {
            var denunciasQuery = _context.Denuncias
                .Include(d => d.Aluno)
                .Include(d => d.Escola)
                .Include(d => d.Anexos)
                .Include(d => d.DenunciaTestemunhas)
                    .ThenInclude(dt => dt.Testemunha)
                .Include(d => d.Respostas)
                .AsQueryable();

            if (idBusca.HasValue)
            {
                denunciasQuery = denunciasQuery.Where(d => d.Id_Denuncia == idBusca.Value);
            }

            if (!string.IsNullOrEmpty(status) && status != "Todos")
            {
                denunciasQuery = denunciasQuery.Where(d => d.Status_Denuncia == status);
            }

            if (!string.IsNullOrEmpty(categoria) && categoria != "Todas")
            {
                denunciasQuery = denunciasQuery.Where(d => d.Categoria_Denuncia == categoria);
            }

            ViewBag.CurrentStatus = status;
            ViewBag.CurrentCategoria = categoria;
            ViewBag.CurrentIdBusca = idBusca;

            ViewBag.CategoriasUnicas = await _context.Denuncias
                .Select(d => d.Categoria_Denuncia)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var denuncias = await denunciasQuery
                .OrderByDescending(d => d.DataCriacao_Denuncia)
                .ToListAsync();

            return View(denuncias);
        }

        // ============================================================== DETALHES (CORRIGIDO) ==================================================
        public async Task<IActionResult> Detalhes(int? id)
        {
            if (id == null) return NotFound();

            var denuncia = await _context.Denuncias
                .Include(d => d.Aluno)
                .Include(d => d.Escola)
                .Include(d => d.Anexos)
                .Include(d => d.DenunciaTestemunhas)
                    .ThenInclude(dt => dt.Testemunha)
                .Include(d => d.Respostas)
                    .ThenInclude(r => r.Funcionario)
                .FirstOrDefaultAsync(d => d.Id_Denuncia == id);

            if (denuncia == null)
            {
                TempData["MensagemErro"] = "Denúncia não encontrada.";
                return NotFound();
            }

            // Lógica: MARCAR NOTIFICAÇÃO COMO LIDA
            if (User.IsInRole("Aluno") && denuncia.Id_Aluno.HasValue)
            {
                if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int alunoId))
                {
                    var notificacao = await _context.Notificacao
                        .FirstOrDefaultAsync(n => n.Id_Denuncia == id && n.Id_Aluno == alunoId && n.Lida == false);

                    if (notificacao != null)
                    {
                        notificacao.Lida = true;
                        _context.Update(notificacao);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            ViewBag.UserRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

            return View(denuncia);
        }

        // ============================================================== CRIAR =====================================================
        [HttpGet]
        [Authorize(Roles = "Aluno")]
        public IActionResult Criar()
        {
            ViewData["Id_Escola"] = new SelectList(_context.Escolas, "Id_Escola", "Nome_Escola");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Aluno")]
        public async Task<IActionResult> Criar(
            [Bind("Titulo_Denuncia,Descricao_Denuncia,Categoria_Denuncia,Id_Escola,DenunciaAnonima")] Denuncia denuncia,
            List<IFormFile> anexosArquivos, // Alterado para List<IFormFile>
            List<string> Nome_Testemunha,
            List<string> Telefone_Testemunha)
        {
            // REMOÇÃO DO if (!ModelState.IsValid) conforme solicitado.

            try
            {
                denuncia.DataCriacao_Denuncia = DateTime.Now;
                denuncia.Status_Denuncia = "Aberta";

                // Vincula a denúncia ao aluno logado
                if (!int.TryParse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value, out int idAluno))
                {
                    TempData["MensagemErro"] = "Erro: Usuário não autenticado ou ID inválido.";
                    return RedirectToAction("Login", "Account");
                }
                denuncia.Id_Aluno = idAluno;

                _context.Add(denuncia);
                await _context.SaveChangesAsync(); // Salva a denúncia para obter o Id

                // Lógica de upload de MÚLTIPLOS anexos (Opcional)
                if (anexosArquivos != null && anexosArquivos.Count > 0)
                {
                    string uploadsPasta = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsPasta))
                        Directory.CreateDirectory(uploadsPasta);

                    foreach (var anexoArquivo in anexosArquivos.Where(f => f != null && f.Length > 0))
                    {
                        string nomeArquivoUnico = Guid.NewGuid() + "_" + Path.GetFileName(anexoArquivo.FileName);
                        string caminhoArquivo = Path.Combine(uploadsPasta, nomeArquivoUnico);

                        using (var fileStream = new FileStream(caminhoArquivo, FileMode.Create))
                        {
                            await anexoArquivo.CopyToAsync(fileStream);
                        }

                        var anexo = new Anexo
                        {
                            Tipo_Anexo = anexoArquivo.ContentType,
                            NomeOriginal_Anexo = Path.GetFileName(anexoArquivo.FileName), // Para download
                            Caminho_Anexo = "/uploads/" + nomeArquivoUnico,
                            Id_Denuncia = denuncia.Id_Denuncia
                        };

                        _context.Anexos.Add(anexo);
                    }
                    await _context.SaveChangesAsync();
                }

                // Lógica de testemunhas (mantida)
                if (Nome_Testemunha != null && Nome_Testemunha.Count > 0)
                {
                    for (int i = 0; i < Nome_Testemunha.Count; i++)
                    {
                        if (string.IsNullOrWhiteSpace(Nome_Testemunha[i])) continue;

                        var testemunha = new Testemunha
                        {
                            Nome_Testemunha = Nome_Testemunha[i],
                            Telefone_Testemunha = Telefone_Testemunha.ElementAtOrDefault(i)
                        };

                        _context.Testemunhas.Add(testemunha);
                        await _context.SaveChangesAsync(); // Salva para obter o ID

                        var relacao = new DenunciaTestemunha
                        {
                            Id_Denuncia = denuncia.Id_Denuncia,
                            Id_Testemunha = testemunha.Id_Testemunha
                        };
                        _context.DenunciaTestemunhas.Add(relacao);
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["MensagemSucesso"] = $"Denúncia #{denuncia.Id_Denuncia} registrada com sucesso!";
                return RedirectToAction(nameof(MinhasDenuncias));
            }
            catch (Exception ex)
            {
                TempData["MensagemErro"] = $"Ocorreu um erro ao salvar a denúncia: {ex.Message}";
                ViewData["Id_Escola"] = new SelectList(_context.Escolas, "Id_Escola", "Nome_Escola", denuncia.Id_Escola);
                return View(denuncia);
            }
        }

        // ============================================================== DOWNLOAD ANEXO ==================================================
        [Authorize]
        public async Task<IActionResult> DownloadAnexo(int idAnexo)
        {
            var anexo = await _context.Anexos
                .Include(a => a.Denuncia)
                .FirstOrDefaultAsync(a => a.Id_Anexo == idAnexo);

            if (anexo == null)
            {
                TempData["MensagemErro"] = "Anexo não encontrado.";
                return NotFound();
            }

            // Segurança: Aluno só pode baixar anexos das suas próprias denúncias.
            if (User.IsInRole("Aluno"))
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int alunoId) || anexo.Denuncia.Id_Aluno != alunoId)
                {
                    TempData["MensagemErro"] = "Você não tem permissão para baixar este anexo.";
                    return Forbid();
                }
            }

            try
            {
                string caminhoFisico = Path.Combine(_webHostEnvironment.WebRootPath, anexo.Caminho_Anexo.TrimStart('/'));

                if (!System.IO.File.Exists(caminhoFisico))
                {
                    TempData["MensagemErro"] = "Arquivo não encontrado no servidor.";
                    return NotFound();
                }

                // O NomeOriginal_Anexo deve ser usado para o nome do arquivo baixado
                string nomeDoArquivo = anexo.NomeOriginal_Anexo ?? Path.GetFileName(anexo.Caminho_Anexo);

                // Retorna o arquivo como FileResult
                var fileStream = new FileStream(caminhoFisico, FileMode.Open, FileAccess.Read);
                return File(fileStream, anexo.Tipo_Anexo, nomeDoArquivo);
            }
            catch (Exception ex)
            {
                TempData["MensagemErro"] = $"Erro ao processar o download: {ex.Message}";
                return RedirectToAction("Detalhes", new { id = anexo.Id_Denuncia });
            }
        }

        // ============================================================== REMOVER ANEXO (NOVA ACTION) ==================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Funcionario,Admin,Aluno")]
        public async Task<IActionResult> RemoverAnexo(int id)
        {
            var anexo = await _context.Anexos
                .Include(a => a.Denuncia)
                .FirstOrDefaultAsync(a => a.Id_Anexo == id);

            if (anexo == null)
            {
                // Retorna HTTP 404 com um corpo JSON
                return NotFound(new { message = "Anexo não encontrado." });
            }

            // Segurança: Garantir que apenas o dono da denúncia ou Admin/Funcionario possa remover
            if (User.IsInRole("Aluno"))
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int alunoId) || anexo.Denuncia.Id_Aluno != alunoId)
                {
                    // CORREÇÃO APLICADA: Retorna HTTP 403 com um corpo JSON
                    return StatusCode(403, new { message = "Você não tem permissão para remover este anexo." });
                }

                // Adicionalmente, verifica se a denúncia pode ser editada (status)
                if (anexo.Denuncia.Status_Denuncia == "Respondida" || anexo.Denuncia.Status_Denuncia == "Resolvida" || anexo.Denuncia.Status_Denuncia == "Encerrada")
                {
                    // CORREÇÃO APLICADA: Retorna HTTP 403 com um corpo JSON
                    return StatusCode(403, new { message = "A denúncia não pode mais ser editada (status: " + anexo.Denuncia.Status_Denuncia + ")." });
                }
            }

            try
            {
                // 1. Apaga o arquivo físico do servidor
                string caminhoFisico = Path.Combine(_webHostEnvironment.WebRootPath, anexo.Caminho_Anexo.TrimStart('/'));
                if (System.IO.File.Exists(caminhoFisico))
                {
                    System.IO.File.Delete(caminhoFisico);
                }

                // 2. Apaga o registro do banco de dados
                _context.Anexos.Remove(anexo);
                await _context.SaveChangesAsync();

                // Retorna um status de sucesso (200 OK) para o AJAX
                return Ok(new { message = "Anexo removido com sucesso." });
            }
            catch (Exception ex)
            {
                // Retorna um erro interno (500) para o AJAX
                return StatusCode(500, new { message = $"Erro ao remover anexo: {ex.Message}" });
            }
        }


        // ============================================================== EDITAR =====================================================
        [HttpGet]
        [Authorize(Roles = "Funcionario,Admin,Aluno")]
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null)
            {
                TempData["MensagemErro"] = "ID da denúncia não fornecido.";
                return NotFound();
            }

            var denuncia = await _context.Denuncias
                .Include(d => d.DenunciaTestemunhas)
                    .ThenInclude(dt => dt.Testemunha)
                .Include(d => d.Anexos)
                .FirstOrDefaultAsync(d => d.Id_Denuncia == id);

            if (denuncia == null)
            {
                TempData["MensagemErro"] = "Denúncia não encontrada.";
                return NotFound();
            }

            // 🔒 BLOQUEIO GERAL: Status = Pendente → NÃO EDITA
            if (denuncia.Status_Denuncia == "Pendente")
            {
                TempData["MensagemErro"] = "Denúncias marcadas como 'Pendente' não podem ser editadas.";
                return RedirectToAction(nameof(Detalhes), new { id = id });
            }

            if (User.IsInRole("Aluno"))
            {
                if (denuncia.Id_Aluno.HasValue && denuncia.Id_Aluno.Value.ToString() != User.FindFirstValue(ClaimTypes.NameIdentifier))
                {
                    TempData["MensagemErro"] = "Você não tem permissão para editar esta denúncia.";
                    return RedirectToAction(nameof(Detalhes), new { id = id });
                }

                if (denuncia.Status_Denuncia == "Respondida" || denuncia.Status_Denuncia == "Resolvida" || denuncia.Status_Denuncia == "Encerrada")
                {
                    TempData["MensagemErro"] = "Esta denúncia não pode mais ser editada.";
                    return RedirectToAction(nameof(Detalhes), new { id = id });
                }
            }

            ViewData["Id_Aluno"] = new SelectList(_context.Alunos, "Id_Aluno", "Nome_Aluno", denuncia.Id_Aluno);
            ViewData["Id_Escola"] = new SelectList(_context.Escolas, "Id_Escola", "Nome_Escola", denuncia.Id_Escola);

            return View(denuncia);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Funcionario,Admin,Aluno")]
        public async Task<IActionResult> Editar(
    int id,
    Denuncia denunciaAtualizada,
    List<IFormFile> novosAnexosArquivos,
    List<string> Nome_Testemunha,
    List<string> Telefone_Testemunha)
        {
            if (id != denunciaAtualizada.Id_Denuncia) return NotFound();

            var denuncia = await _context.Denuncias
                .Include(d => d.DenunciaTestemunhas)
                    .ThenInclude(dt => dt.Testemunha)
                .FirstOrDefaultAsync(d => d.Id_Denuncia == id);

            if (denuncia == null)
            {
                TempData["MensagemErro"] = "Denúncia não encontrada para edição.";
                return NotFound();
            }

            // 🔒 Bloqueio no POST também
            if (denuncia.Status_Denuncia == "Pendente")
            {
                TempData["MensagemErro"] = "Denúncias marcadas como 'Pendente' não podem ser editadas.";
                return RedirectToAction(nameof(Detalhes), new { id = id });
            }

            try
            {
                // Atualiza campos
                denuncia.Titulo_Denuncia = denunciaAtualizada.Titulo_Denuncia;
                denuncia.Descricao_Denuncia = denunciaAtualizada.Descricao_Denuncia;
                denuncia.Categoria_Denuncia = denunciaAtualizada.Categoria_Denuncia;
                denuncia.DenunciaAnonima = denunciaAtualizada.DenunciaAnonima; // Adicionado
                denuncia.Id_Escola = denunciaAtualizada.Id_Escola;

                // Funcionários/Admin podem alterar Aluno e Status (Alunos não podem)
                if (User.IsInRole("Funcionario") || User.IsInRole("Admin"))
                {
                    denuncia.Id_Aluno = denunciaAtualizada.Id_Aluno;
                    denuncia.Status_Denuncia = denunciaAtualizada.Status_Denuncia ?? denuncia.Status_Denuncia;
                }

                #region Lógica de Múltiplos Anexos
                if (novosAnexosArquivos != null && novosAnexosArquivos.Count > 0)
                {
                    string uploadsPasta = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsPasta))
                        Directory.CreateDirectory(uploadsPasta);

                    foreach (var anexoArquivo in novosAnexosArquivos.Where(f => f != null && f.Length > 0))
                    {
                        string nomeArquivoUnico = Guid.NewGuid() + "_" + Path.GetFileName(anexoArquivo.FileName);
                        string caminhoArquivo = Path.Combine(uploadsPasta, nomeArquivoUnico);

                        using (var fileStream = new FileStream(caminhoArquivo, FileMode.Create))
                        {
                            await anexoArquivo.CopyToAsync(fileStream);
                        }

                        var novoAnexo = new Anexo
                        {
                            Tipo_Anexo = anexoArquivo.ContentType,
                            NomeOriginal_Anexo = Path.GetFileName(anexoArquivo.FileName),
                            Caminho_Anexo = "/uploads/" + nomeArquivoUnico,
                            Id_Denuncia = denuncia.Id_Denuncia
                        };

                        _context.Anexos.Add(novoAnexo);
                    }
                }
                #endregion

                #region Lógica de Testemunhas
                // Remove relações antigas e coleta IDs de testemunhas órfãs
                var relacoesAntigas = _context.DenunciaTestemunhas
                    .Where(dt => dt.Id_Denuncia == denuncia.Id_Denuncia)
                    .ToList();

                var idsTestemunhasAntigas = relacoesAntigas.Select(dt => dt.Id_Testemunha).ToList();

                _context.DenunciaTestemunhas.RemoveRange(relacoesAntigas);

                // Adiciona novas testemunhas
                if (Nome_Testemunha != null && Nome_Testemunha.Count > 0)
                {
                    for (int i = 0; i < Nome_Testemunha.Count; i++)
                    {
                        var nome = Nome_Testemunha[i];
                        if (string.IsNullOrWhiteSpace(nome)) continue;

                        var novaTestemunha = new Testemunha
                        {
                            Nome_Testemunha = nome,
                            Telefone_Testemunha = Telefone_Testemunha.ElementAtOrDefault(i)
                        };

                        _context.Testemunhas.Add(novaTestemunha);
                        await _context.SaveChangesAsync();

                        var novaRelacao = new DenunciaTestemunha
                        {
                            Id_Denuncia = denuncia.Id_Denuncia,
                            Id_Testemunha = novaTestemunha.Id_Testemunha
                        };
                        _context.DenunciaTestemunhas.Add(novaRelacao);
                    }
                }
                #endregion

                _context.Update(denuncia);
                await _context.SaveChangesAsync();

                // Limpar testemunhas órfãs (que não estão mais ligadas a nenhuma denúncia)
                var testemunhasParaRemover = _context.Testemunhas
                    .Where(t => idsTestemunhasAntigas.Contains(t.Id_Testemunha) && !_context.DenunciaTestemunhas.Any(dt => dt.Id_Testemunha == t.Id_Testemunha));
                _context.Testemunhas.RemoveRange(testemunhasParaRemover);
                await _context.SaveChangesAsync();


                TempData["MensagemSucesso"] = $"Denúncia #{denuncia.Id_Denuncia} atualizada com sucesso!";
                return RedirectToAction(nameof(Detalhes), new { id = id });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Denuncias.Any(e => e.Id_Denuncia == id))
                {
                    TempData["MensagemErro"] = "Denúncia não encontrada. Concorrência falhou.";
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                TempData["MensagemErro"] = $"Ocorreu um erro ao atualizar a denúncia: {ex.Message}";
                ViewData["Id_Aluno"] = new SelectList(_context.Alunos, "Id_Aluno", "Nome_Aluno", denunciaAtualizada.Id_Aluno);
                ViewData["Id_Escola"] = new SelectList(_context.Escolas, "Id_Escola", "Nome_Escola", denunciaAtualizada.Id_Escola);
                return View(denunciaAtualizada);
            }
        }



        // ============================================================== DELETAR ====================================================
        [HttpGet]
        [Authorize(Roles = "Funcionario,Admin")]
        public async Task<IActionResult> Deletar(int? id)
        {
            if (id == null) return NotFound();

            var denuncia = await _context.Denuncias
                .Include(d => d.Aluno)
                .Include(d => d.Escola)
                .FirstOrDefaultAsync(d => d.Id_Denuncia == id);

            if (denuncia == null) return NotFound();

            return View(denuncia);
        }

        [HttpPost, ActionName("Deletar")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Funcionario,Admin")]
        public async Task<IActionResult> DeletarConfirmado(int id)
        {
            var denuncia = await _context.Denuncias.FindAsync(id);
            if (denuncia != null)
            {
                // TODO: Adicionar lógica para remover anexos e relações (testemunhas, respostas) para evitar órfãos.

                _context.Denuncias.Remove(denuncia);
                await _context.SaveChangesAsync();
                TempData["MensagemSucesso"] = "Denúncia excluída com sucesso!";
            }
            else
            {
                TempData["MensagemErro"] = "Erro: Denúncia não encontrada para exclusão.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ============================================================== RESPONDER (REDIRECIONAMENTO ALTERADO) ==================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Funcionario,Admin")]
        public async Task<IActionResult> Responder(int Id_Denuncia, string Descricao_Resposta)
        {
            if (string.IsNullOrWhiteSpace(Descricao_Resposta))
            {
                TempData["MensagemErro"] = "A descrição da resposta é obrigatória.";
                return RedirectToAction("Detalhes", new { id = Id_Denuncia });
            }

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int idFuncionario))
            {
                TempData["MensagemErro"] = "Erro: Não foi possível identificar o usuário que está respondendo.";
                return RedirectToAction("Index");
            }

            var denuncia = await _context.Denuncias
                .Include(d => d.Aluno)
                .FirstOrDefaultAsync(d => d.Id_Denuncia == Id_Denuncia);

            if (denuncia == null)
            {
                TempData["MensagemErro"] = "Denúncia não encontrada.";
                return RedirectToAction("Index");
            }

            try
            {
                var resposta = new Resposta
                {
                    Id_Denuncia = Id_Denuncia,
                    Descricao_Resposta = Descricao_Resposta,
                    Data_Resposta = DateTime.Now,
                    Id_Funcionario = idFuncionario
                };
                _context.Respostas.Add(resposta);

                if (denuncia.Status_Denuncia != "Resolvida" && denuncia.Status_Denuncia != "Encerrada")
                {
                    denuncia.Status_Denuncia = "Respondida";
                    _context.Update(denuncia);
                }

                if (denuncia.Id_Aluno.HasValue)
                {
                    var notificacao = new Notificacao
                    {
                        Id_Aluno = denuncia.Id_Aluno.Value,
                        Id_Denuncia = Id_Denuncia,
                        Mensagem = $"Sua denúncia #{Id_Denuncia} foi respondida. Status: {denuncia.Status_Denuncia}",
                        Lida = false,
                        DataCriacao = DateTime.Now,
                        UrlDestino = $"/Denuncia/Detalhes/{Id_Denuncia}"
                    };
                    _context.Notificacao.Add(notificacao);
                }

                await _context.SaveChangesAsync();

                TempData["MensagemSucesso"] = "Resposta enviada e status atualizado com sucesso.";
            }
            catch (Exception ex)
            {
                TempData["MensagemErro"] = $"Erro ao salvar a resposta: {ex.Message}";
            }

            // ⚠️ MUDANÇA SOLICITADA: Redireciona para Index
            return RedirectToAction(nameof(Index));
        }

        // ============================================================== ALTERAR STATUS (REDIRECIONAMENTO ALTERADO) ==================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Funcionario,Admin")]
        // Dentro do método [HttpPost] AlterarStatus
        public async Task<IActionResult> AlterarStatus(int id, string novoStatus)
        {
            // 1. Buscar a Denúncia.
            var denuncia = await _context.Denuncias
                .FirstOrDefaultAsync(d => d.Id_Denuncia == id);

            if (denuncia == null) return NotFound();

            string statusAntigo = denuncia.Status_Denuncia;

            // 1. OBTENÇÃO DA CHAVE DO USUÁRIO (Funcionário/Admin)
            if (!int.TryParse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier), out int idUsuarioFuncionario))
            {
                TempData["MensagemErro"] = "Falha na autenticação do usuário. Log não registrado.";
                return RedirectToAction("index", new { id = id });
            }

            // 2. BUSCAR USUÁRIO E O NÍVEL (Funcionário/Admin)
            var usuario = await _context.Usuarios
                .Include(u => u.Nivel)
                .FirstOrDefaultAsync(u => u.Id_Usuario == idUsuarioFuncionario);

            if (usuario == null || usuario.Nivel == null || usuario.Id_Nivel == 1) // ID 1 é Aluno.
            {
                TempData["MensagemErro"] = "Somente funcionários/administradores podem alterar o status.";
                return RedirectToAction("index", new { id = id });
            }

            // 3. GRAVAR O NOVO REGISTRO DE LOG E CRIAR NOTIFICAÇÃO
            if (statusAntigo != novoStatus)
            {
                // 3.1. CRIAÇÃO DO LOG DE STATUS (Lógica existente)
                var logEntry = new LogStatus
                {
                    Id_Denuncia = id,
                    Status_Anterior = statusAntigo,
                    Status_Novo = novoStatus,
                    Timestamp = DateTime.Now,
                    Id_Nivel = usuario.Id_Nivel,
                    Nome_Nivel = usuario.Nivel.Nome_Nivel
                };
                _context.LogStatus.Add(logEntry);

                // 3.2. CRIAÇÃO DA NOTIFICAÇÃO PARA O ALUNO 🔔
                if (denuncia.Id_Aluno.HasValue)
                {
                    // O caminho completo para a página de detalhes da denúncia
                    var urlDestino = $"/Denuncia/Detalhes/{denuncia.Id_Denuncia}";

                    var novaNotificacao = new Notificacao
                    {
                        Id_Aluno = denuncia.Id_Aluno.Value, // O ID do Aluno é quem recebe a notificação
                        Id_Denuncia = denuncia.Id_Denuncia,
                        Mensagem = $"O status da sua denúncia #{denuncia.Id_Denuncia} foi atualizado para: {novoStatus}.",
                        DataCriacao = DateTime.Now,
                        Lida = false, // O aluno precisa ler!
                        UrlDestino = urlDestino // **CAMPO OBRIGATÓRIO ADICIONADO AQUI**
                    };
                    _context.Notificacao.Add(novaNotificacao);
                }
                else
                {
                    // Lidar com denúncia sem aluno associado, se necessário.
                }

                // 4. ATUALIZAR STATUS
                denuncia.Status_Denuncia = novoStatus;
                _context.Denuncias.Update(denuncia);

                // 5. SALVAR TUDO: Denúncia, LogStatus e Notificação
                await _context.SaveChangesAsync(); // Deve funcionar agora que 'UrlDestino' está preenchido.

                TempData["MensagemSucesso"] = $"Status da denúncia alterado para '{novoStatus}', log registrado e **notificação enviada ao aluno**.";
            }

            return RedirectToAction("index", new { id = id });
        }
        // ======================================================= MINHAS DENÚNCIAS / NOTIFICAÇÕES =======================================================

        [Authorize(Roles = "Aluno")]
        public async Task<IActionResult> MinhasDenuncias()
        {
            var alunoIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(alunoIdStr) || !int.TryParse(alunoIdStr, out int alunoId))
            {
                TempData["MensagemErro"] = "Falha de autenticação. Por favor, faça login novamente.";
                return RedirectToAction("Login", "Account");
            }

            var denuncias = await _context.Denuncias
                .Where(d => d.Id_Aluno == alunoId)
                .Include(d => d.Aluno)
                .Include(d => d.Escola)
                .Include(d => d.Anexos)
                .Include(d => d.DenunciaTestemunhas)
                    .ThenInclude(dt => dt.Testemunha)
                .Include(d => d.Respostas)
                    .ThenInclude(r => r.Funcionario)
                .OrderByDescending(d => d.DataCriacao_Denuncia)
                .ToListAsync();

            return View(denuncias);
        }

        [Authorize(Roles = "Aluno")]
        public async Task<IActionResult> MinhasNotificacoes()
        {
            var alunoIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(alunoIdStr) || !int.TryParse(alunoIdStr, out int alunoId))
            {
                TempData["MensagemErro"] = "Erro de autenticação. Tente logar novamente.";
                return RedirectToAction("Login", "Account");
            }

            var notificacoes = await _context.Notificacao
                .Include(n => n.Denuncia)
                .Where(n => n.Id_Aluno == alunoId)
                .OrderByDescending(n => n.DataCriacao)
                .ToListAsync();

            return View(notificacoes);
        }

        // ======================================================= DASHBOARD =======================================================
        // Dashboard principal (view)
        [Authorize(Roles = "Funcionario,Admin")]
        public async Task<IActionResult> Dashboard(int ano = 0)
        {
            if (ano == 0) ano = DateTime.Now.Year;

            // Totais
            var totalDenuncias = await _context.Denuncias.CountAsync();
            // A denúncia com status "Aberta" na imagem deve ser contabilizada aqui se não tiver um tratamento específico
            var totalPendentes = await _context.Denuncias.CountAsync(d => d.Status_Denuncia == "Pendente");
            var denunciasMes = await _context.Denuncias.CountAsync(d =>
                d.DataCriacao_Denuncia.Month == DateTime.Now.Month && d.DataCriacao_Denuncia.Year == DateTime.Now.Year);

            // Categorias
            var statusFinais = new[] { "Resolvida", "Encerrada", "Concluída" };

            var totalResolvidas = await _context.Denuncias.CountAsync(d =>
                statusFinais.Contains(d.Status_Denuncia));

            var totalEmAnalise = await _context.Denuncias.CountAsync(d =>
                d.Status_Denuncia == "Em Análise" || d.Status_Denuncia == "Respondida");

            var totalOutros = totalDenuncias - (totalPendentes + totalResolvidas + totalEmAnalise);
            if (totalOutros < 0) totalOutros = 0;

            // KPI global: média (reusa seu método)
            double mediaTempoHoras = await CalcularMediaTempoResolucaoAsync();
            double mediaTempoDias = mediaTempoHoras / 24.0;

            // Dados mensais: criadas por mês (ano selecionado)
            var criadasPorMes = await _context.Denuncias
                .Where(d => d.DataCriacao_Denuncia.Year == ano)
                .GroupBy(d => d.DataCriacao_Denuncia.Month)
                .Select(g => new { Mes = g.Key, Quantidade = g.Count() })
                .ToListAsync();

            // Logs de status finais no ano
            var logsFinal = await _context.LogStatus
                .Where(l => statusFinais.Contains(l.Status_Novo) && l.Timestamp.Year == ano)
                .ToListAsync();

            // ****************************************************
            // CORREÇÃO FINAL APLICADA AQUI: Contagem de resolvidas por mês
            // Filtra pelo último log final, mas SÓ SE O STATUS ATUAL for um status final.
            // ****************************************************
            // 1. Encontrar o ÚLTIMO log de status final para cada denúncia no ano
            var ultimosLogsDeResolucao = logsFinal
                .GroupBy(l => l.Id_Denuncia)
                .Select(g => g.OrderByDescending(l => l.Timestamp).FirstOrDefault())
                .Where(l => l != null)
                .ToList();

            // 2. Filtrar os IDs de denúncias que estão ATUALMENTE em um status final
            var idsAtualmenteResolvidas = await _context.Denuncias
                .Where(d => statusFinais.Contains(d.Status_Denuncia))
                .Select(d => d.Id_Denuncia)
                .ToListAsync();

            // 3. Filtrar o conjunto de logs de resolução para incluir apenas aquelas que permanecem resolvidas
            var logsResolvidasAtuais = ultimosLogsDeResolucao
                .Where(l => idsAtualmenteResolvidas.Contains(l.Id_Denuncia))
                .ToList();

            // 4. Agrupar por Mês usando apenas os logs das denúncias que ainda estão resolvidas
            var resolvidasPorMes = logsResolvidasAtuais
                .GroupBy(l => l.Timestamp.Month)
                .Select(g => new { Mes = g.Key, Quantidade = g.Count() })
                .ToList();
            // ****************************************************

            // arrays 12 meses
            int[] arrCriadas = Enumerable.Range(1, 12).Select(m => 0).ToArray();
            int[] arrResolvidas = Enumerable.Range(1, 12).Select(m => 0).ToArray();
            foreach (var c in criadasPorMes) arrCriadas[c.Mes - 1] = c.Quantidade;
            foreach (var r in resolvidasPorMes) arrResolvidas[r.Mes - 1] = r.Quantidade;

            // MEDIA HORAS POR MES (log-based)
            double[] mediaHorasPorMes = new double[12];

            // ids que tiveram final no ano
            // OBS: Para a média, é mais comum contar o tempo de TODAS que atingiram a conclusão, 
            // mesmo que reabertas, mas manteremos o escopo original.
            var idsResolvidasNoAno = logsFinal.Select(l => l.Id_Denuncia).Distinct().ToList();

            // buscar logs completos só para esses ids
            var logsDasDenuncias = await _context.LogStatus
                .Where(l => idsResolvidasNoAno.Contains(l.Id_Denuncia))
                .OrderBy(l => l.Id_Denuncia).ThenBy(l => l.Timestamp)
                .ToListAsync();

            for (int mes = 1; mes <= 12; mes++)
            {
                // Aqui, o cálculo da média de tempo continua usando todas as que foram concluídas no mês
                // (idsNoMes), independentemente do status atual, pois mede o desempenho do processo de conclusão.
                var idsNoMes = logsFinal.Where(l => l.Timestamp.Month == mes).Select(l => l.Id_Denuncia).Distinct().ToList();
                double somaHoras = 0;
                int cont = 0;

                foreach (var id in idsNoMes)
                {
                    var logs = logsDasDenuncias.Where(l => l.Id_Denuncia == id).ToList();

                    // START: primeiro "Em Análise" OU se não existir, a primeira alteração de status
                    var inicio = logs.FirstOrDefault(l => l.Status_Novo == "Em Análise") ?? logs.FirstOrDefault();
                    // END: último log com status final
                    var fim = logs.LastOrDefault(l => statusFinais.Contains(l.Status_Novo));

                    if (inicio != null && fim != null && fim.Timestamp > inicio.Timestamp)
                    {
                        somaHoras += (fim.Timestamp - inicio.Timestamp).TotalHours;
                        cont++;
                    }
                }

                mediaHorasPorMes[mes - 1] = cont > 0 ? somaHoras / cont : 0;
            }

            // ViewBags
            ViewBag.Total = totalDenuncias;
            ViewBag.Pendentes = totalPendentes;
            ViewBag.Mes = denunciasMes;
            ViewBag.MediaTempoResolucaoDias = mediaTempoDaysToString(mediaTempoDias); // helper abaixo

            ViewBag.DadosGrafico = new
            {
                Pendentes = totalPendentes,
                Resolvidas = totalResolvidas,
                EmAnalise = totalEmAnalise,
                Outros = totalOutros
            };

            ViewBag.MensalCriadas = arrCriadas;
            ViewBag.MensalResolvidas = arrResolvidas;
            ViewBag.MediaHorasPorMes = mediaHorasPorMes;
            ViewBag.AnoSelecionado = ano;

            return View();
        }

        private string mediaTempoDaysToString(double mediaTempoDias)
        {
            if (mediaTempoDias <= 0) return "N/A";

            // Converte o total de dias (em double) em um TimeSpan
            TimeSpan tempo = TimeSpan.FromDays(mediaTempoDias);

            int dias = (int)tempo.TotalDays;
            int horas = tempo.Hours;

            // Se o total de horas for menor que 1, mostra apenas a fração em minutos (opcional, mas mais preciso)
            if (dias == 0 && horas == 0)
            {
                int minutos = tempo.Minutes;
                if (minutos > 0)
                {
                    return $"{minutos} min";
                }
                return "0 horas";
            }

            // Formata a string de saída
            string resultado = "";
            if (dias > 0)
            {
                resultado += $"{dias} dia{(dias > 1 ? "s" : "")}";
            }

            if (horas > 0)
            {
                // Se já tiver dias, adiciona um separador
                if (dias > 0)
                {
                    resultado += " e ";
                }
                resultado += $"{horas} hora{(horas > 1 ? "s" : "")}";
            }

            return resultado.Trim();
        }

        // AJAX: retorna JSON com arrays para o ano solicitado
        [HttpGet]
        [Authorize(Roles = "Funcionario,Admin")]
        public async Task<IActionResult> GetDashboardData(int ano = 0)
        {
            if (ano == 0) ano = DateTime.Now.Year;

            // Criadas por mês
            var criadasPorMes = await _context.Denuncias
                .Where(d => d.DataCriacao_Denuncia.Year == ano)
                .GroupBy(d => d.DataCriacao_Denuncia.Month)
                .Select(g => new { Mes = g.Key, Quantidade = g.Count() })
                .ToListAsync();

            // Status finais
            var statusFinais = new[] { "Resolvida", "Encerrada", "Concluída" };
            var logsFinal = await _context.LogStatus
                .Where(l => statusFinais.Contains(l.Status_Novo) && l.Timestamp.Year == ano)
                .ToListAsync();

            // ****************************************************
            // CORREÇÃO FINAL APLICADA AQUI: Contagem de resolvidas por mês
            // ****************************************************
            var ultimosLogsDeResolucao = logsFinal
                .GroupBy(l => l.Id_Denuncia)
                .Select(g => g.OrderByDescending(l => l.Timestamp).FirstOrDefault())
                .Where(l => l != null)
                .ToList();

            var idsAtualmenteResolvidas = await _context.Denuncias
                .Where(d => statusFinais.Contains(d.Status_Denuncia))
                .Select(d => d.Id_Denuncia)
                .ToListAsync();

            var logsResolvidasAtuais = ultimosLogsDeResolucao
                .Where(l => idsAtualmenteResolvidas.Contains(l.Id_Denuncia))
                .ToList();

            var resolvidasPorMes = logsResolvidasAtuais
                .GroupBy(l => l.Timestamp.Month)
                .Select(g => new { Mes = g.Key, Quantidade = g.Count() })
                .ToList();
            // ****************************************************

            // Inicializar arrays completos
            int[] arrCriadas = Enumerable.Range(1, 12).Select(m => 0).ToArray();
            int[] arrResolvidas = Enumerable.Range(1, 12).Select(m => 0).ToArray();

            foreach (var c in criadasPorMes)
                arrCriadas[c.Mes - 1] = c.Quantidade;

            foreach (var r in resolvidasPorMes)
                arrResolvidas[r.Mes - 1] = r.Quantidade;

            // Média por mês
            double[] mediaHoras = new double[12];

            var logsDasDenuncias = await _context.LogStatus
                .Where(l => logsFinal.Select(x => x.Id_Denuncia).Contains(l.Id_Denuncia))
                .OrderBy(l => l.Id_Denuncia).ThenBy(l => l.Timestamp)
                .ToListAsync();

            foreach (var mes in Enumerable.Range(1, 12))
            {
                var idsNoMes = logsFinal.Where(l => l.Timestamp.Month == mes)
                                         .Select(l => l.Id_Denuncia)
                                         .Distinct()
                                         .ToList();

                double soma = 0;
                int cont = 0;

                foreach (var id in idsNoMes)
                {
                    var logs = logsDasDenuncias.Where(l => l.Id_Denuncia == id).ToList();
                    var inicio = logs.FirstOrDefault(l => l.Status_Novo == "Em Análise") ?? logs.First();
                    var fim = logs.LastOrDefault(l => statusFinais.Contains(l.Status_Novo));

                    if (inicio != null && fim != null && fim.Timestamp > inicio.Timestamp)
                    {
                        soma += (fim.Timestamp - inicio.Timestamp).TotalHours;
                        cont++;
                    }
                }

                mediaHoras[mes - 1] = cont > 0 ? soma / cont : 0;
            }

            return Json(new
            {
                ano = ano,
                criadas = arrCriadas,
                resolvidas = arrResolvidas,
                mediaHoras = mediaHoras
            });
        }

        // EXPORT CSV
        [HttpGet]
        [Authorize(Roles = "Funcionario,Admin")]
        public async Task<FileResult> ExportCsv(int ano = 0)
        {
            if (ano == 0) ano = DateTime.Now.Year;

            // Recupera arrays iguais aos do Dashboard
            var criadasPorMes = await _context.Denuncias
                .Where(d => d.DataCriacao_Denuncia.Year == ano)
                .GroupBy(d => d.DataCriacao_Denuncia.Month)
                .Select(g => new { Mes = g.Key, Quantidade = g.Count() })
                .ToListAsync();

            var statusFinais = new[] { "Resolvida", "Encerrada", "Concluída" };
            var logsFinal = await _context.LogStatus
                .Where(l => statusFinais.Contains(l.Status_Novo) && l.Timestamp.Year == ano)
                .ToListAsync();

            // ****************************************************
            // CORREÇÃO FINAL APLICADA AQUI: Contagem de resolvidas por mês
            // ****************************************************
            var ultimosLogsDeResolucao = logsFinal
                .GroupBy(l => l.Id_Denuncia)
                .Select(g => g.OrderByDescending(l => l.Timestamp).FirstOrDefault())
                .Where(l => l != null)
                .ToList();

            var idsAtualmenteResolvidas = await _context.Denuncias
                .Where(d => statusFinais.Contains(d.Status_Denuncia))
                .Select(d => d.Id_Denuncia)
                .ToListAsync();

            var logsResolvidasAtuais = ultimosLogsDeResolucao
                .Where(l => idsAtualmenteResolvidas.Contains(l.Id_Denuncia))
                .ToList();

            var resolvidasPorMes = logsResolvidasAtuais
                .GroupBy(l => l.Timestamp.Month)
                .Select(g => new { Mes = g.Key, Quantidade = g.Count() })
                .ToList();
            // ****************************************************

            int[] arrCriadas = Enumerable.Range(1, 12).Select(m => 0).ToArray();
            int[] arrResolvidas = Enumerable.Range(1, 12).Select(m => 0).ToArray();
            foreach (var c in criadasPorMes) arrCriadas[c.Mes - 1] = c.Quantidade;
            foreach (var r in resolvidasPorMes) arrResolvidas[r.Mes - 1] = r.Quantidade;

            var sb = new StringBuilder();
            sb.AppendLine("Mês;Criadas;Resolvidas");
            var meses = new[] { "Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez" };
            for (int i = 0; i < 12; i++)
            {
                sb.AppendLine($"{meses[i]};{arrCriadas[i]};{arrResolvidas[i]}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"Denuncias_{ano}.csv";
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }

        // EXPORT EXCEL usando ClosedXML 
        [HttpGet]
        [Authorize(Roles = "Funcionario,Admin")]
        public async Task<FileResult> ExportExcel(int ano = 0)
        {
            if (ano == 0) ano = DateTime.Now.Year;

            // Cria os mesmos arrays
            var criadasPorMes = await _context.Denuncias
                .Where(d => d.DataCriacao_Denuncia.Year == ano)
                .GroupBy(d => d.DataCriacao_Denuncia.Month)
                .Select(g => new { Mes = g.Key, Quantidade = g.Count() })
                .ToListAsync();

            var statusFinais = new[] { "Resolvida", "Encerrada", "Concluída" };
            var logsFinal = await _context.LogStatus
                .Where(l => statusFinais.Contains(l.Status_Novo) && l.Timestamp.Year == ano)
                .ToListAsync();

            // ****************************************************
            // CORREÇÃO FINAL APLICADA AQUI: Contagem de resolvidas por mês
            // ****************************************************
            var ultimosLogsDeResolucao = logsFinal
                .GroupBy(l => l.Id_Denuncia)
                .Select(g => g.OrderByDescending(l => l.Timestamp).FirstOrDefault())
                .Where(l => l != null)
                .ToList();

            var idsAtualmenteResolvidas = await _context.Denuncias
                .Where(d => statusFinais.Contains(d.Status_Denuncia))
                .Select(d => d.Id_Denuncia)
                .ToListAsync();

            var logsResolvidasAtuais = ultimosLogsDeResolucao
                .Where(l => idsAtualmenteResolvidas.Contains(l.Id_Denuncia))
                .ToList();

            var resolvidasPorMes = logsResolvidasAtuais
                .GroupBy(l => l.Timestamp.Month)
                .Select(g => new { Mes = g.Key, Quantidade = g.Count() })
                .ToList();
            // ****************************************************

            int[] arrCriadas = Enumerable.Range(1, 12).Select(m => 0).ToArray();
            int[] arrResolvidas = Enumerable.Range(1, 12).Select(m => 0).ToArray();
            foreach (var c in criadasPorMes) arrCriadas[c.Mes - 1] = c.Quantidade;
            foreach (var r in resolvidasPorMes) arrResolvidas[r.Mes - 1] = r.Quantidade;

            // Gera planilha
            // (O restante do código de exportação de Excel permanece o mesmo)
            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Denúncias");
                ws.Cell(1, 1).Value = "Mês";
                ws.Cell(1, 2).Value = "Criadas";
                ws.Cell(1, 3).Value = "Resolvidas";
                var meses = new[] { "Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez" };
                for (int i = 0; i < 12; i++)
                {
                    ws.Cell(i + 2, 1).Value = meses[i];
                    ws.Cell(i + 2, 2).Value = arrCriadas[i];
                    ws.Cell(i + 2, 3).Value = arrResolvidas[i];
                }

                using (var ms = new MemoryStream())
                {
                    workbook.SaveAs(ms);
                    var bytes = ms.ToArray();
                    return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Denuncias_{ano}.xlsx");
                }
            }
        }

        // ... (Outros métodos: RegistrarLogStatusAsync e CalcularMediaTempoResolucaoAsync) ...

        private async Task RegistrarLogStatusAsync(int idDenuncia, string novoStatus, string statusAnterior)
        {
            // 1. Obter o ID do usuário logado (claim)
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int idUsuario))
            {
                return; // não encontrou o ID
            }

            // 2. Buscar usuário na tabela Usuarios (incluindo o Nivel)
            var usuario = await _context.Usuarios
                .Include(u => u.Nivel)
                .FirstOrDefaultAsync(u => u.Id_Usuario == idUsuario);

            if (usuario == null)
                return; // usuário não existe

            // 3. Bloquear apenas ALUNO (nivel 1)
            if (usuario.Id_Nivel == 1)
                return;

            // 4. Registrar log se mudou o status
            if (statusAnterior != novoStatus)
            {
                // 🚨 CORREÇÃO APLICADA: Instanciar o objeto LogStatus
                var logEntry = new LogStatus
                {
                    Id_Denuncia = idDenuncia,
                    Status_Anterior = statusAnterior,
                    Status_Novo = novoStatus,
                    Timestamp = DateTime.Now,
                    // Usando os dados CORRETOS do usuário buscado no DB:
                    Id_Nivel = usuario.Id_Nivel,
                    Nome_Nivel = usuario.Nivel.Nome_Nivel // Vai ser "Funcionario" ou "Admin"
                };

                // 🚨 CORREÇÃO APLICADA: Adicionar ao contexto e salvar as mudanças
                _context.LogStatus.Add(logEntry);
                await _context.SaveChangesAsync();
            }
        }


        private async Task<double> CalcularMediaTempoResolucaoAsync()
        {
            // 1. DEFINIÇÃO DOS STATUS DE TEMPO:
            const string STATUS_INICIO_ANALISE = "Em Análise";
            var statusFinais = new[] { "Resolvida", "Encerrada", "Concluída" };

            // 2. IDENTIFICAR DENÚNCIAS RESOLVIDAS
            var idsDenunciasResolvidas = await _context.LogStatus
                .Where(l => statusFinais.Contains(l.Status_Novo))
                .Select(l => l.Id_Denuncia)
                .Distinct()
                .ToListAsync();

            if (!idsDenunciasResolvidas.Any())
            {
                return 0.0;
            }

            // 3. BUSCA DE TODOS OS LOGS PERTINENTES
            var logsCompletos = await _context.LogStatus
                .Where(l => idsDenunciasResolvidas.Contains(l.Id_Denuncia))
                .OrderBy(l => l.Id_Denuncia)
                .ThenBy(l => l.Timestamp)
                .ToListAsync();

            double totalHoras = 0;
            int denunciasContadas = 0;

            // 4. ITERAÇÃO E CÁLCULO DA DURAÇÃO (Timestamp Final - Timestamp Inicial)
            foreach (var id in idsDenunciasResolvidas)
            {
                var logsDenuncia = logsCompletos.Where(l => l.Id_Denuncia == id).ToList();

                // Ponto de início: O primeiro registro onde o status muda para "Em Análise"
                var logInicio = logsDenuncia.FirstOrDefault(l => l.Status_Novo == STATUS_INICIO_ANALISE);

                // Se não encontrou 'Em Análise', usa o primeiro log disponível
                if (logInicio == null)
                {
                    logInicio = logsDenuncia.FirstOrDefault();
                }

                // Ponto final: O último registro onde o status muda para um dos status finais
                var logFim = logsDenuncia.LastOrDefault(l => statusFinais.Contains(l.Status_Novo));

                if (logInicio != null && logFim != null && logFim.Timestamp > logInicio.Timestamp)
                {
                    TimeSpan duracao = logFim.Timestamp - logInicio.Timestamp;
                    totalHoras += duracao.TotalHours;
                    denunciasContadas++;
                }
            }

            // Retorna a média em horas
            return denunciasContadas > 0 ? totalHoras / denunciasContadas : 0.0;
        }

        [Authorize(Roles = "Aluno")]
        public async Task<IActionResult> Remover(int idNotificacao)
        {
            var alunoIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(alunoIdStr) || !int.TryParse(alunoIdStr, out int alunoId))
            {
                TempData["MensagemErro"] = "Erro de autenticação. Tente logar novamente.";
                return RedirectToAction("Login", "Account");
            }

            // Busca a notificação e verifica se ela pertence ao aluno logado para segurança
            var notificacao = await _context.Notificacao
                .FirstOrDefaultAsync(n => n.Id_Notificacao == idNotificacao && n.Id_Aluno == alunoId);

            if (notificacao == null)
            {
                TempData["MensagemErro"] = "Notificação não encontrada ou acesso negado.";
                return RedirectToAction(nameof(MinhasNotificacoes));
            }

            _context.Notificacao.Remove(notificacao);
            await _context.SaveChangesAsync();
            TempData["MensagemSucesso"] = "Notificação removida com sucesso.";

            return RedirectToAction(nameof(MinhasNotificacoes));
        }

        // ----------------------------------------------------
        // AÇÃO 2: Limpar Todas as Notificações do Aluno
        // ----------------------------------------------------
        [Authorize(Roles = "Aluno")]
        public async Task<IActionResult> LimparTodas()
        {
            var alunoIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(alunoIdStr) || !int.TryParse(alunoIdStr, out int alunoId))
            {
                TempData["MensagemErro"] = "Erro de autenticação. Tente logar novamente.";
                return RedirectToAction("Login", "Account");
            }

            // Busca todas as notificações do aluno
            var notificacoesParaRemover = await _context.Notificacao
                .Where(n => n.Id_Aluno == alunoId)
                .ToListAsync();

            if (notificacoesParaRemover.Any())
            {
                _context.Notificacao.RemoveRange(notificacoesParaRemover);
                await _context.SaveChangesAsync();
                TempData["MensagemSucesso"] = $"Total de {notificacoesParaRemover.Count} notificações foram removidas.";
            }
            else
            {
                TempData["MensagemAviso"] = "Você não tem notificações para remover.";
            }

            return RedirectToAction(nameof(MinhasNotificacoes));
        }

    }
}