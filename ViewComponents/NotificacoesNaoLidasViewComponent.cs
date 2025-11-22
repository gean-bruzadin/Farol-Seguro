using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Farol_Seguro.Config; // Use o namespace correto do seu DbConfig

public class NotificacoesNaoLidasViewComponent : ViewComponent
{
    private readonly DbConfig _context;

    // Injeção de dependência do seu DbContext
    public NotificacoesNaoLidasViewComponent(DbConfig context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        // Verifica se o usuário está logado
        if (!User.Identity.IsAuthenticated)
        {
            return View(0); // Retorna 0 se não estiver logado
        }

        // Obtém o ID do usuário logado (Aluno)
        // Assumindo que o ID do Aluno está armazenado no ClaimTypes.NameIdentifier
        var userIdClaim = UserClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int idAluno))
        {
            return View(0); // Retorna 0 se o ID do aluno não for encontrado ou for inválido
        }

        // Consulta para contar as notificações não lidas para este Aluno
        int contagemNaoLidas = await _context.Notificacao
            .Where(n => n.Id_Aluno == idAluno && n.Lida == false)
            .CountAsync();

        // Passa a contagem para a ViewComponent
        return View(contagemNaoLidas);
    }
}