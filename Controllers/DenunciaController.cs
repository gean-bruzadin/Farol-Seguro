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
        // Método auxiliar para obter o Id_Aluno logado (baseado na sua lógica de Claims)
        private int ObterIdAlunoLogado()
        {
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int idAluno))
            {
                return idAluno;
            }
            return 0; // Retorna 0 se não encontrar ou falhar
        }

        // =======================================================================================================
        // ACTIONS CRUD BÁSICAS (Com Bloqueio)
        // =======================================================================================================

        [HttpGet]
        [Authorize(Roles = "Aluno")]
        public async Task<IActionResult> Criar()
        {
            // === LÓGICA DE VERIFICAÇÃO DE BLOQUEIO (GET) ===
            int idAluno = ObterIdAlunoLogado();
            if (idAluno > 0)
            {
                var aluno = await _context.Alunos.FindAsync(idAluno);

                if (aluno != null && aluno.IsBloqueado)
                {
                    TempData["MensagemErro"] = "🚫 **ALUNO BLOQUEADO:** Você está impedido de criar novas denúncias devido a um histórico de 3 ou mais denúncias falsas.";
                    return RedirectToAction(nameof(MinhasDenuncias));
                }
            }
            // ===============================================

            ViewData["Id_Escola"] = new SelectList(_context.Escolas, "Id_Escola", "Nome_Escola");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Aluno")]
        public async Task<IActionResult> Criar(
    [Bind("Titulo_Denuncia,Descricao_Denuncia,Categoria_Denuncia,Id_Escola,DenunciaAnonima")] Denuncia denuncia,
    List<IFormFile> anexosArquivos, // Esta lista virá vazia se o Model Binding falhar!
    List<string> Nome_Testemunha,
    List<string> Telefone_Testemunha)
        {
            int idAluno = ObterIdAlunoLogado();
            if (idAluno <= 0)
            {
                TempData["MensagemErro"] = "Erro: Usuário não autenticado ou ID inválido.";
                return RedirectToAction("Login", "Account");
            }

            // === LÓGICA DE VERIFICAÇÃO DE BLOQUEIO (POST) ===
            var aluno = await _context.Alunos.FindAsync(idAluno);
            if (aluno != null && aluno.IsBloqueado)
            {
                TempData["MensagemErro"] = "🚫 **ALUNO BLOQUEADO:** Você está impedido de criar novas denúncias devido a um histórico de 3 ou mais denúncias falsas.";
                return RedirectToAction(nameof(MinhasDenuncias));
            }
            // ===============================================

            try
            {
                // Configurações básicas da denúncia
                denuncia.DataCriacao_Denuncia = DateTime.Now;
                denuncia.Status_Denuncia = "Aberta";
                denuncia.Id_Aluno = idAluno;

                // Salva a denúncia para obter o Id_Denuncia (PRIMEIRO SAVE)
                _context.Add(denuncia);
                await _context.SaveChangesAsync();

                // 1. Lógica de upload de MÚLTIPLOS anexos
                if (anexosArquivos != null && anexosArquivos.Count > 0)
                {
                    string uploadsPasta = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsPasta))
                        Directory.CreateDirectory(uploadsPasta);

                    var anexosFalhos = new List<string>(); // Para rastrear falhas de I/O

                    foreach (var anexoArquivo in anexosArquivos.Where(f => f != null && f.Length > 0))
                    {
                        try
                        {
                            string nomeArquivoUnico = Guid.NewGuid() + "_" + Path.GetFileName(anexoArquivo.FileName);
                            string caminhoArquivo = Path.Combine(uploadsPasta, nomeArquivoUnico);

                            // Tenta salvar no disco (Ponto mais comum de falha de I/O/Permissão)
                            using (var fileStream = new FileStream(caminhoArquivo, FileMode.Create))
                            {
                                await anexoArquivo.CopyToAsync(fileStream);
                            }

                            // Se o disco funcionou, prepara o registro no banco
                            var anexo = new Anexo
                            {
                                Tipo_Anexo = anexoArquivo.ContentType,
                                NomeOriginal_Anexo = Path.GetFileName(anexoArquivo.FileName),
                                Caminho_Anexo = "/uploads/" + nomeArquivoUnico,
                                Id_Denuncia = denuncia.Id_Denuncia
                            };
                            _context.Anexos.Add(anexo);
                        }
                        catch (Exception ex)
                        {
                            // Captura falha de upload de UM único arquivo e continua.
                            anexosFalhos.Add(anexoArquivo.FileName);
                            // O erro de I/O está aqui, mas o loop continua.
                        }
                    }

                    // Salva todos os anexos que foram adicionados com sucesso (SEGUNDO SAVE)
                    await _context.SaveChangesAsync();

                    // Adiciona a mensagem de erro de anexo ao TempData, se houver falhas.
                    if (anexosFalhos.Any())
                    {
                        // Note: Esta mensagem será combinada com a MensagemSucesso.
                        TempData["MensagemErro"] = $"⚠️ **ALERTA DE ANEXO:** Os arquivos ({string.Join(", ", anexosFalhos)}) falharam ao ser enviados. Verifique as permissões da pasta 'uploads' no servidor.";
                    }
                }

                // 2. Lógica de testemunhas (TERCEIRO SAVE) - Mantida inalterada
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
                        await _context.SaveChangesAsync(); // Salva para obter o ID (melhor mover este save para fora do loop se for possível, mas mantendo sua lógica atual)

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
                // Captura falhas de DB (Constraint) ou erros críticos de lógica.
                TempData["MensagemErro"] = $"Ocorreu um erro CRÍTICO ao salvar a denúncia: {ex.Message}";
                ViewData["Id_Escola"] = new SelectList(_context.Escolas, "Id_Escola", "Nome_Escola", denuncia.Id_Escola);
                return View(denuncia);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Aluno,Funcionario,Admin")] // Deixa todos verem o form, mas aplica o bloqueio de edição
        public async Task<IActionResult> Editar(int? id)
        {
            int idAlunoLogado = ObterIdAlunoLogado();

            if (id == null) { return NotFound(); }

            var denuncia = await _context.Denuncias
                .Include(d => d.DenunciaTestemunhas)
                    .ThenInclude(dt => dt.Testemunha)
                .Include(d => d.Anexos)
                .FirstOrDefaultAsync(d => d.Id_Denuncia == id);

            if (denuncia == null) { return NotFound(); }

            // --- Regras de Bloqueio para EDIÇÃO ---
            string status = denuncia.Status_Denuncia?.ToLower();
            bool isAluno = User.IsInRole("Aluno");
            bool isStatusAberta = status == "aberta";

            // 1. BLOQUEIO GERAL: Denúncia Falsa
            if (denuncia.IsFalsa)
            {
                TempData["MensagemErro"] = "⚠️ **DENÚNCIA FALSA:** Não é possível editar denúncias que foram marcadas como Falsas.";
                return RedirectToAction(nameof(Detalhes), new { id });
            }

            // 2. BLOQUEIO POR STATUS (Aplicado a TODOS que editam)
            if (!isStatusAberta)
            {
                TempData["MensagemErro"] = $"Denúncias com status '{denuncia.Status_Denuncia}' não podem mais ter seu conteúdo editado.";
                return RedirectToAction(nameof(Detalhes), new { id });
            }

            // 3. BLOQUEIO ALUNO: Só pode editar a própria denúncia
            if (isAluno && denuncia.Id_Aluno.HasValue && denuncia.Id_Aluno.Value != idAlunoLogado)
            {
                TempData["MensagemErro"] = "Você não tem permissão para editar esta denúncia.";
                return RedirectToAction(nameof(Detalhes), new { id });
            }

            // 4. BLOQUEIO ALUNO: Aluno bloqueado
            if (isAluno)
            {
                var aluno = await _context.Alunos.FindAsync(idAlunoLogado);
                if (aluno != null && aluno.IsBloqueado)
                {
                    TempData["MensagemErro"] = "🚫 **ALUNO BLOQUEADO:** Você está impedido de editar denúncias devido ao histórico de denúncias falsas.";
                    return RedirectToAction(nameof(MinhasDenuncias));
                }
            }


            ViewData["Id_Aluno"] = new SelectList(_context.Alunos, "Id_Aluno", "Nome_Aluno", denuncia.Id_Aluno);
            ViewData["Id_Escola"] = new SelectList(_context.Escolas, "Id_Escola", "Nome_Escola", denuncia.Id_Escola);

            return View(denuncia);
        }

        // -------------------------------------------------------------------
        // AÇÃO HTTP POST: EDITAR (Salva as alterações)
        // -------------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Aluno,Funcionario,Admin")]
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
                .Include(d => d.Anexos)
                .FirstOrDefaultAsync(d => d.Id_Denuncia == id);

            if (denuncia == null) { return NotFound(); }

            string statusOriginal = denuncia.Status_Denuncia?.ToLower();
            int idAlunoLogado = ObterIdAlunoLogado();
            bool isAluno = User.IsInRole("Aluno");
            bool isFuncionarioAdmin = User.IsInRole("Funcionario") || User.IsInRole("Admin");

            // --- Regra de Bloqueio por Status (SÓ PODE EDITAR CONTEÚDO SE FOR "ABERTA") ---
            bool podeEditarConteudo = statusOriginal == "aberta";


            // --- BLOQUEIOS ANTES DE QUALQUER ATUALIZAÇÃO ---

            // 1. BLOQUEIO GERAL: Denúncia Falsa
            if (denuncia.IsFalsa)
            {
                TempData["MensagemErro"] = "⚠️ **DENÚNCIA FALSA:** Não é possível salvar denúncias que foram marcadas como Falsas.";
                return RedirectToAction(nameof(MinhasDenuncias));
            }

            // 2. BLOQUEIO ALUNO: Checa se é o dono e se está bloqueado
            if (isAluno)
            {
                var aluno = await _context.Alunos.FindAsync(idAlunoLogado);
                if (aluno != null && aluno.IsBloqueado)
                {
                    TempData["MensagemErro"] = "🚫 **ALUNO BLOQUEADO:** Você está impedido de editar denúncias devido ao histórico de denúncias falsas.";
                    return RedirectToAction(nameof(MinhasDenuncias));
                }

                // 3. BLOQUEIO ALUNO: Só pode editar a própria denúncia
                if (denuncia.Id_Aluno.HasValue && denuncia.Id_Aluno.Value != idAlunoLogado)
                {
                    TempData["MensagemErro"] = "Você não pode editar esta denúncia.";
                    return RedirectToAction(nameof(Detalhes), new { id });
                }
            }

            try
            {
                // --- ATUALIZAÇÃO DE CONTEÚDO (Permitida SOMENTE se status for "Aberta") ---
                if (podeEditarConteudo)
                {
                    // Atualiza campos básicos
                    denuncia.Titulo_Denuncia = denunciaAtualizada.Titulo_Denuncia;
                    denuncia.Descricao_Denuncia = denunciaAtualizada.Descricao_Denuncia;
                    denuncia.Categoria_Denuncia = denunciaAtualizada.Categoria_Denuncia;
                    denuncia.DenunciaAnonima = denunciaAtualizada.DenunciaAnonima;
                    denuncia.Id_Escola = denunciaAtualizada.Id_Escola;

                    #region Adição de Novos Anexos
                    if (novosAnexosArquivos != null && novosAnexosArquivos.Count > 0)
                    {
                        string uploadsPasta = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                        if (!Directory.Exists(uploadsPasta)) Directory.CreateDirectory(uploadsPasta);

                        foreach (var anexoArquivo in novosAnexosArquivos.Where(f => f != null && f.Length > 0))
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
                                NomeOriginal_Anexo = Path.GetFileName(anexoArquivo.FileName),
                                Caminho_Anexo = "/uploads/" + nomeArquivoUnico,
                                Id_Denuncia = denuncia.Id_Denuncia
                            };
                            _context.Anexos.Add(anexo);
                        }
                    }
                    #endregion

                    #region Atualização de Testemunhas
                    // Remove as relações DenunciaTestemunha antigas
                    if (denuncia.DenunciaTestemunhas != null && denuncia.DenunciaTestemunhas.Any())
                    {
                        _context.DenunciaTestemunhas.RemoveRange(denuncia.DenunciaTestemunhas);
                    }

                    // Adiciona as novas testemunhas e recria as relações
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
                            await _context.SaveChangesAsync();

                            var relacao = new DenunciaTestemunha
                            {
                                Id_Denuncia = denuncia.Id_Denuncia,
                                Id_Testemunha = testemunha.Id_Testemunha
                            };
                            _context.DenunciaTestemunhas.Add(relacao);
                        }
                    }

                    // Remove testemunhas órfãs
                    var testemunhasOrfas = await _context.Testemunhas
                        .Where(t => !_context.DenunciaTestemunhas.Any(dt => dt.Id_Testemunha == t.Id_Testemunha))
                        .ToListAsync();

                    _context.Testemunhas.RemoveRange(testemunhasOrfas);
                    #endregion
                }
                else if (isAluno)
                {
                    // Se o aluno tentou editar quando o status não é "Aberta"
                    TempData["MensagemErro"] = $"A edição de conteúdo não é permitida, pois o status da denúncia é '{denuncia.Status_Denuncia}'.";
                    return RedirectToAction(nameof(Detalhes), new { id });
                }


                // --- ATUALIZAÇÃO DE CAMPOS ADMINISTRATIVOS (Permitida SOMENTE por Funcionário/Admin) ---
                if (isFuncionarioAdmin)
                {
                    denuncia.Id_Aluno = denunciaAtualizada.Id_Aluno;
                    denuncia.Status_Denuncia = denunciaAtualizada.Status_Denuncia ?? denuncia.Status_Denuncia;
                }

                _context.Update(denuncia);
                await _context.SaveChangesAsync();

                TempData["MensagemSucesso"] = $"Denúncia #{denuncia.Id_Denuncia} atualizada com sucesso!";
                return RedirectToAction(nameof(Detalhes), new { id });
            }
            catch (Exception ex)
            {
                TempData["MensagemErro"] = $"Erro ao atualizar: {ex.Message}";

                ViewData["Id_Aluno"] = new SelectList(_context.Alunos, "Id_Aluno", "Nome_Aluno", denuncia.Id_Aluno);
                ViewData["Id_Escola"] = new SelectList(_context.Escolas, "Id_Escola", "Nome_Escola", denuncia.Id_Escola);

                return View(denuncia); // Retorna a denúncia original para evitar perda de dados
            }
        }

        // =======================================================================================================
        // AÇÃO EXCLUSIVA DO ADMIN/FUNCIONÁRIO (MARCAR COMO FALSA)
        // =======================================================================================================

        [HttpPost]
        [Authorize(Roles = "Funcionario,Admin")]
        [ValidateAntiForgeryToken] // Recomendável para todas as ações POST que modificam dados
        public async Task<IActionResult> MarcarComoFalsa(int idDenuncia)
        {
            var denuncia = await _context.Denuncias
                .Include(d => d.Aluno)
                .FirstOrDefaultAsync(d => d.Id_Denuncia == idDenuncia);

            if (denuncia == null)
            {
                TempData["MensagemErro"] = "Denúncia não encontrada.";
                return RedirectToAction(nameof(Index));
            }

            if (denuncia.IsFalsa)
            {
                TempData["WarningMessage"] = $"A Denúncia #{idDenuncia} já foi marcada como falsa.";
                return RedirectToAction(nameof(Index));
            }

            // 1. Marca a denúncia como falsa e ATUALIZA O STATUS.
            denuncia.IsFalsa = true;
            denuncia.Status_Denuncia = "Rejeitada - Denúncia Falsa";

            string mensagemParaAdmin = $"Denúncia #{idDenuncia} marcada como FALSA com sucesso.";
            string mensagemParaAluno = "";

            // 2. Aplica a penalidade e cria a notificação para o aluno.
            if (denuncia.Id_Aluno.HasValue && denuncia.Aluno != null)
            {
                var aluno = denuncia.Aluno;
                aluno.ContadorDenunciasFalsas++;

                const int LIMITE_FALSAS = 3;

                // Define a mensagem e o status de bloqueio
                if (aluno.ContadorDenunciasFalsas >= LIMITE_FALSAS)
                {
                    aluno.IsBloqueado = true;

                    // Mensagem de ALERTA VERMELHO (para o TempData)
                    mensagemParaAluno = $"<strong>Atenção! Sua denúncia #{idDenuncia} foi marcada como FALSA.</strong> <br>" +
                                        $"Você atingiu o limite de {LIMITE_FALSAS} denúncias falsas. <br>" +
                                        $"Como consequência, seu acesso ao sistema está <strong>BLOQUEADO</strong> por tempo indeterminado. " +
                                        $"Entre em contato com a administração da escola para mais detalhes.";

                    // Mensagem para o funcionário/admin
                    mensagemParaAdmin = $"Denúncia #{idDenuncia} marcada como FALSA. Aluno <strong>{aluno.Nome_Aluno}</strong> atingiu o limite de {LIMITE_FALSAS} e foi <strong>BLOQUEADO</strong>!";
                }
                else
                {
                    // Mensagem de ALERTA AMARELO (para o TempData)
                    mensagemParaAluno = $"<strong>Atenção! Sua denúncia #{idDenuncia} foi marcada como FALSA.</strong> <br>" +
                                        $"Você acumula {aluno.ContadorDenunciasFalsas}/{LIMITE_FALSAS} denúncias falsas. " +
                                        $"O limite é de {LIMITE_FALSAS} denúncias falsas antes do <strong>BLOQUEIO</strong> da conta.";

                    // Mensagem para o funcionário/admin
                    mensagemParaAdmin = $"Denúncia #{idDenuncia} marcada como FALSA. Contador de denúncias falsas do aluno: {aluno.ContadorDenunciasFalsas}/{LIMITE_FALSAS}.";
                }

                // CRIA A NOTIFICAÇÃO NO BANCO DE DADOS (RESOLVE O ERRO 'UrlDestino' cannot be null)
                var novaNotificacao = new Farol_Seguro.Models.Notificacao
                {
                    Id_Aluno = aluno.Id_Aluno,
                    Id_Denuncia = denuncia.Id_Denuncia,
                    // Mensagem em texto simples para armazenamento no BD
                    Mensagem = $"Sua denúncia #{denuncia.Id_Denuncia} foi marcada como FALSA. Status: {aluno.ContadorDenunciasFalsas}/{LIMITE_FALSAS}. Bloqueio: {(aluno.IsBloqueado ? "Sim" : "Não")}.",
                    Lida = false,
                    DataCriacao = DateTime.Now,
                    // 🛑 CORREÇÃO APLICADA: Define o UrlDestino
                    UrlDestino = $"/Denuncia/Detalhes/{denuncia.Id_Denuncia}"
                };
                _context.Add(novaNotificacao);

                _context.Update(aluno); // Salva as mudanças do contador/bloqueio do aluno
            }

            _context.Update(denuncia); // Salva a mudança do IsFalsa=true e Status_Denuncia
            await _context.SaveChangesAsync(); // Deve funcionar sem erro MySqlException agora

            // LÓGICA DE REDIRECIONAMENTO E MENSAGEM:
            int? alunoLogadoId = GetCurrentAlunoId();

            if (alunoLogadoId.HasValue && denuncia.Id_Aluno.HasValue && alunoLogadoId.Value == denuncia.Id_Aluno.Value)
            {
                // Se o usuário logado for o autor da denúncia, exibe o ALERTA VERMELHO na sua lista.
                TempData["MensagemErro"] = mensagemParaAluno;
                return RedirectToAction("MinhasDenuncias");
            }
            else
            {
                // Se for o Admin/Funcionario, exibe a mensagem de sucesso padrão na lista geral.
                TempData["MensagemSucesso"] = mensagemParaAdmin;
                return RedirectToAction(nameof(Index));
            }
        }

        // -------------------------------------------------------------------------
        // Este método auxiliar DEVE ser adicionado ao seu DenunciaController.cs
        // -------------------------------------------------------------------------
        private int? GetCurrentAlunoId()
        {
            // Lógica para obter o ID do aluno logado a partir dos Claims.
            // Você deve garantir que o Claim "Id_Aluno" está sendo definido na sua autenticação.
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == "Id_Aluno");
            if (idClaim != null && int.TryParse(idClaim.Value, out int alunoId))
            {
                return alunoId;
            }
            return null;
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

            // Definições de Status
            var statusFinais = new[] { "Resolvida", "Encerrada" };
            const string STATUS_FALSA = "Rejeitada - Denúncia Falsa";
            const string STATUS_INVESTIGACAO = "Investigação";

            // Totais
            var totalDenuncias = await _context.Denuncias.CountAsync();
            var totalInvestigadas = await _context.Denuncias.CountAsync(d => d.Status_Denuncia == STATUS_INVESTIGACAO);
            var totalResolvidas = await _context.Denuncias.CountAsync(d => statusFinais.Contains(d.Status_Denuncia));
            var totalFalsas = await _context.Denuncias.CountAsync(d => d.Status_Denuncia == STATUS_FALSA);
            var denunciasMes = await _context.Denuncias.CountAsync(d =>
                d.DataCriacao_Denuncia.Month == DateTime.Now.Month && d.DataCriacao_Denuncia.Year == DateTime.Now.Year);

            var totalEmAnalise = await _context.Denuncias.CountAsync(d =>
                d.Status_Denuncia == STATUS_INVESTIGACAO || d.Status_Denuncia == "Respondida");

            var totalOutros = totalDenuncias - (totalInvestigadas + totalResolvidas + totalEmAnalise + totalFalsas);
            if (totalOutros < 0) totalOutros = 0;

            // KPI global
            double mediaTempoHoras = await CalcularMediaTempoResolucaoAsync();
            double mediaTempoDias = mediaTempoHoras / 24.0;

            // Dados para o Gráfico de Criadas, Resolvidas e Falsas por Mês
            var criadasPorMes = await _context.Denuncias
                .Where(d => d.DataCriacao_Denuncia.Year == ano)
                .GroupBy(d => d.DataCriacao_Denuncia.Month)
                .Select(g => new { Mes = g.Key, Quantidade = g.Count() })
                .ToListAsync();

            var falsasPorMes = await _context.Denuncias
                .Where(d => d.DataCriacao_Denuncia.Year == ano && d.Status_Denuncia == STATUS_FALSA)
                .GroupBy(d => d.DataCriacao_Denuncia.Month)
                .Select(g => new { Mes = g.Key, Quantidade = g.Count() })
                .ToListAsync();

            // Contagem de Resolvidas por Mês (Baseada no último Log de Status Final)
            var logsFinal = await _context.LogStatus
                .Where(l => statusFinais.Contains(l.Status_Novo) && l.Timestamp.Year == ano)
                .ToListAsync();

            var resolvidasPorMes = logsFinal
                .GroupBy(l => l.Id_Denuncia)
                .Select(g => g.OrderByDescending(l => l.Timestamp).FirstOrDefault())
                .Where(l => l != null)
                .GroupBy(l => l.Timestamp.Month)
                .Select(g => new { Mes = g.Key, Quantidade = g.Count() })
                .ToList();

            // Inicializar arrays de 12 meses
            int[] arrCriadas = Enumerable.Range(1, 12).Select(m => 0).ToArray();
            int[] arrResolvidas = Enumerable.Range(1, 12).Select(m => 0).ToArray();
            int[] arrFalsas = Enumerable.Range(1, 12).Select(m => 0).ToArray();

            foreach (var c in criadasPorMes) arrCriadas[c.Mes - 1] = c.Quantidade;
            foreach (var r in resolvidasPorMes) arrResolvidas[r.Mes - 1] = r.Quantidade;
            foreach (var f in falsasPorMes) arrFalsas[f.Mes - 1] = f.Quantidade;

            // Média de Tempo de Resolução em Dias por Mês
            double[] mediaDiasPorMes = new double[12];
            var idsResolvidasNoAno = logsFinal.Select(l => l.Id_Denuncia).Distinct().ToList();

            var logsDasDenuncias = await _context.LogStatus
                .Where(l => idsResolvidasNoAno.Contains(l.Id_Denuncia))
                .OrderBy(l => l.Id_Denuncia).ThenBy(l => l.Timestamp)
                .ToListAsync();

            for (int mes = 1; mes <= 12; mes++)
            {
                var idsNoMes = logsFinal.Where(l => l.Timestamp.Month == mes).Select(l => l.Id_Denuncia).Distinct().ToList();
                double somaHoras = 0;
                int cont = 0;

                foreach (var id in idsNoMes)
                {
                    var logs = logsDasDenuncias.Where(l => l.Id_Denuncia == id).ToList();

                    // START: primeiro log com status "Investigação" ou o primeiro log (se for o caso)
                    var inicio = logs.FirstOrDefault(l => l.Status_Novo == STATUS_INVESTIGACAO) ?? logs.FirstOrDefault();
                    // END: último log com status final
                    var fim = logs.LastOrDefault(l => statusFinais.Contains(l.Status_Novo));

                    if (inicio != null && fim != null && fim.Timestamp > inicio.Timestamp)
                    {
                        somaHoras += (fim.Timestamp - inicio.Timestamp).TotalHours;
                        cont++;
                    }
                }
                // Converte a média de horas para dias
                mediaDiasPorMes[mes - 1] = cont > 0 ? (somaHoras / cont) / 24.0 : 0;
            }

            // ViewBags
            ViewBag.Total = totalDenuncias;
            ViewBag.Pendentes = totalInvestigadas;
            ViewBag.Mes = denunciasMes;
            ViewBag.MediaTempoResolucaoDias = mediaTempoDaysToString(mediaTempoDias);

            ViewBag.DadosGrafico = new
            {
                Pendentes = totalInvestigadas,
                Resolvidas = totalResolvidas,
                EmAnalise = totalEmAnalise,
                Falsas = totalFalsas, // NOVO: Total de Falsas no Donut Chart
                Outros = totalOutros
            };

            ViewBag.MensalCriadas = arrCriadas;
            ViewBag.MensalResolvidas = arrResolvidas;
            ViewBag.MensalFalsas = arrFalsas; // NOVO: Denúncias Falsas por Mês
            ViewBag.MediaDiasPorMes = mediaDiasPorMes; // ALTERADO: Média em Dias por Mês
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

            // Definições de Status
            var statusFinais = new[] { "Resolvida", "Encerrada" };
            const string STATUS_FALSA = "Rejeitada - Denúncia Falsa";
            const string STATUS_INVESTIGACAO = "Investigação";

            // Criadas por mês
            var criadasPorMes = await _context.Denuncias
                .Where(d => d.DataCriacao_Denuncia.Year == ano)
                .GroupBy(d => d.DataCriacao_Denuncia.Month)
                .Select(g => new { Mes = g.Key, Quantidade = g.Count() })
                .ToListAsync();

            // Logs de Status para Resolvidas e Média
            var logsFinal = await _context.LogStatus
                .Where(l => statusFinais.Contains(l.Status_Novo) && l.Timestamp.Year == ano)
                .ToListAsync();

            // Contagem de Resolvidas por Mês
            var resolvidasPorMes = logsFinal
                .GroupBy(l => l.Id_Denuncia)
                .Select(g => g.OrderByDescending(l => l.Timestamp).FirstOrDefault())
                .Where(l => l != null)
                .GroupBy(l => l.Timestamp.Month)
                .Select(g => new { Mes = g.Key, Quantidade = g.Count() })
                .ToList();

            // Contagem de Falsas por Mês (NOVO)
            var falsasPorMes = await _context.Denuncias
                .Where(d => d.DataCriacao_Denuncia.Year == ano && d.Status_Denuncia == STATUS_FALSA)
                .GroupBy(d => d.DataCriacao_Denuncia.Month)
                .Select(g => new { Mes = g.Key, Quantidade = g.Count() })
                .ToListAsync();

            // Inicializar arrays completos
            int[] arrCriadas = Enumerable.Range(1, 12).Select(m => 0).ToArray();
            int[] arrResolvidas = Enumerable.Range(1, 12).Select(m => 0).ToArray();
            int[] arrFalsas = Enumerable.Range(1, 12).Select(m => 0).ToArray(); // NOVO

            foreach (var c in criadasPorMes)
                arrCriadas[c.Mes - 1] = c.Quantidade;

            foreach (var r in resolvidasPorMes)
                arrResolvidas[r.Mes - 1] = r.Quantidade;

            foreach (var f in falsasPorMes)
                arrFalsas[f.Mes - 1] = f.Quantidade; // NOVO

            // Média de Tempo de Resolução em Dias por Mês (ALTERADO)
            double[] mediaDias = new double[12];

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
                    // START: primeiro log com status "Investigação" ou o primeiro log (se for o caso)
                    var inicio = logs.FirstOrDefault(l => l.Status_Novo == STATUS_INVESTIGACAO) ?? logs.FirstOrDefault();
                    var fim = logs.LastOrDefault(l => statusFinais.Contains(l.Status_Novo));

                    if (inicio != null && fim != null && fim.Timestamp > inicio.Timestamp)
                    {
                        soma += (fim.Timestamp - inicio.Timestamp).TotalHours;
                        cont++;
                    }
                }
                // Converte a média de horas para dias
                mediaDias[mes - 1] = cont > 0 ? (soma / cont) / 24.0 : 0;
            }

            return Json(new
            {
                ano = ano,
                criadas = arrCriadas,
                resolvidas = arrResolvidas,
                falsas = arrFalsas, // NOVO: Denúncias Falsas
                mediaDias = mediaDias // ALTERADO: Média em Dias
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

            var statusFinais = new[] { "Resolvida", "Encerrada" };
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

            var statusFinais = new[] { "Resolvida", "Encerrada" };
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
            var statusFinais = new[] { "Resolvida", "Encerrada" };

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

        private string FormatarTempo(double totalHoras)
        {
            if (totalHoras <= 0)
            {
                return "N/A";
            }

            // Calcula os componentes
            int dias = (int)Math.Floor(totalHoras / 24);
            double horasRestantesDecimal = totalHoras % 24;
            int horas = (int)Math.Floor(horasRestantesDecimal);
            int minutos = (int)Math.Round((horasRestantesDecimal - horas) * 60);

            // Ajuste se os minutos arredondarem para 60
            if (minutos == 60)
            {
                minutos = 0;
                horas += 1;
            }
            if (horas == 24)
            {
                horas = 0;
                dias += 1;
            }

            var partes = new System.Collections.Generic.List<string>();

            if (dias > 0)
            {
                partes.Add($"{dias} dia{(dias > 1 ? "s" : "")}");
            }
            if (horas > 0)
            {
                partes.Add($"{horas} hora{(horas > 1 ? "s" : "")}");
            }

            // Se a duração for muito curta (menos de 1 hora), mostra minutos
            if (dias == 0 && horas == 0 && minutos > 0)
            {
                partes.Add($"{minutos} min");
            }

            if (partes.Count == 0 && totalHoras > 0)
            {
                // Se a duração for muito curta (menos de 1 minuto), mostra 'min'
                return "< 1 min";
            }

            // Combina as partes
            if (partes.Count > 1)
            {
                string last = partes[partes.Count - 1];
                partes.RemoveAt(partes.Count - 1);
                return string.Join(", ", partes) + " e " + last;
            }

            return partes.FirstOrDefault() ?? "N/A";
        }
        [HttpGet]
        [Authorize(Roles = "Funcionario,Admin")]
        public async Task<IActionResult> GetKpiData(int ano = 0)
        {
            if (ano == 0) ano = DateTime.Now.Year;

            var statusFinais = new[] { "Resolvida", "Encerrada" };

            // 1. KPI Total Denúncias (Total Acumulado)
            var totalDenuncias = await _context.Denuncias.CountAsync();

            // 2. KPI Denúncias no Mês (Mês Atual do Ano Selecionado)
            var denunciasMes = await _context.Denuncias.CountAsync(d =>
                d.DataCriacao_Denuncia.Month == DateTime.Now.Month && d.DataCriacao_Denuncia.Year == ano);

            // 3. KPI Média de Tempo de Resolução (APENAS para o ano selecionado)

            // Obter logs de status final (Resolvida/Encerrada) para o ano
            var logsFinal = await _context.LogStatus
                .Where(l => statusFinais.Contains(l.Status_Novo) && l.Timestamp.Year == ano)
                .ToListAsync();

            var idsResolvidasNoAno = logsFinal.Select(l => l.Id_Denuncia).Distinct().ToList();

            // Obter todos os logs relevantes para o cálculo do tempo
            var logsDasDenuncias = await _context.LogStatus
                .Where(l => idsResolvidasNoAno.Contains(l.Id_Denuncia))
                .OrderBy(l => l.Id_Denuncia).ThenBy(l => l.Timestamp)
                .ToListAsync();

            double somaHoras = 0;
            int cont = 0;
            const string STATUS_INVESTIGACAO = "Investigação";

            foreach (var id in idsResolvidasNoAno)
            {
                var logs = logsDasDenuncias.Where(l => l.Id_Denuncia == id).ToList();

                // START: primeiro log com status "Investigação" ou o primeiro log (se for o caso)
                var inicio = logs.FirstOrDefault(l => l.Status_Novo == STATUS_INVESTIGACAO) ?? logs.FirstOrDefault();
                // END: último log com status final
                var fim = logs.LastOrDefault(l => statusFinais.Contains(l.Status_Novo));

                if (inicio != null && fim != null && fim.Timestamp > inicio.Timestamp)
                {
                    somaHoras += (fim.Timestamp - inicio.Timestamp).TotalHours;
                    cont++;
                }
            }

            double mediaTempoHoras = cont > 0 ? somaHoras / cont : 0;

            // Função auxiliar para formatar em "X dias e Y horas" ou "Z min"
            // (Você deve ter uma função similar no seu controller, aqui simulamos a formatação)
            string mediaTempoFormatada = mediaTempoHoras == 0 ? "N/A" : FormatarTempo(mediaTempoHoras);

            // Retorna os dados como JSON
            return Json(new
            {
                totalDenuncias = totalDenuncias, // Geral
                denunciasMes = denunciasMes,     // Do Mês (no ano selecionado)
                mediaTempoDias = mediaTempoFormatada // Formatada
            });
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

        // Exemplo de como implementar a Action no seu Controller
        [HttpPost] // <-- OBRIGATÓRIO: Garante que só aceita o método POST
        [ValidateAntiForgeryToken] // Recomendado para segurança
        public async Task<IActionResult> MarcarComoLida(int idNotificacao)
        {
            // 1. Busca a notificação pelo ID
            // Certifique-se de incluir a referência ao modelo de Notificação
            var notificacao = await _context.Notificacao
                                            .FirstOrDefaultAsync(n => n.Id_Notificacao == idNotificacao);

            if (notificacao == null)
            {
                // Se a notificação não for encontrada, retorna 404
                return NotFound();
            }

            // 2. Verifica se já está lida para evitar processamento desnecessário (opcional)
            if (!notificacao.Lida)
            {
                // 3. Marca a propriedade 'Lida' como true
                notificacao.Lida = true;

                try
                {
                    // 4. Salva as alterações no banco de dados
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Trate erros de concorrência se necessário
                    // Para simplicidade, apenas logamos e continuamos/retornamos
                }
            }

            // 5. Redireciona o usuário de volta para a lista de notificações
            // Substitua "MinhasNotificacoes" pelo nome real da sua Action que exibe a lista.
            return RedirectToAction(nameof(MinhasNotificacoes));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirAnexo(int idAnexo, int idDenuncia)
        {
            // 1. Encontre o Anexo
            var anexo = await _context.Anexos.FirstOrDefaultAsync(a => a.Id_Anexo == idAnexo && a.Id_Denuncia == idDenuncia);

            if (anexo == null)
            {
                TempData["MensagemErro"] = "Anexo não encontrado.";
                return RedirectToAction(nameof(Detalhes), new { id = idDenuncia });
            }

            // 2. (OPCIONAL mas RECOMENDADO) Verifique a permissão (aluno dono ou funcionário)

            // 3. Exclua o arquivo físico do servidor (usando IWebHostEnvironment) - LÓGICA DO ARQUIVO FÍSICO AQUI

            // 4. Exclua o registro do banco de dados
            _context.Anexos.Remove(anexo);
            await _context.SaveChangesAsync();

            TempData["MensagemSucesso"] = "Anexo excluído com sucesso!";
            return RedirectToAction(nameof(Detalhes), new { id = idDenuncia });
        }

    }
}