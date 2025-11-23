using Farol_Seguro.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options; // Necessário para a configuração

var builder = WebApplication.CreateBuilder(args);

// Configuração do DbContext para MySQL
builder.Services.AddDbContext<DbConfig>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
);

// --- NOVO: Configuração para o Código Secreto de Admin ---
builder.Services.Configure<AcessoAdminConfig>(
    builder.Configuration.GetSection("AcessoAdmin")
);
// ---------------------------------------------------------

// Configuração de autenticação com Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(config =>
    {
        config.Cookie.Name = "FarolAuthCookie";
        // Rota de Login conforme definido no seu código original
        config.LoginPath = "/Autenticacao/Login";
        config.AccessDeniedPath = "/Home/AcessoNegado";
        config.ExpireTimeSpan = TimeSpan.FromHours(1);
        config.SlidingExpiration = true;
    });

// Adiciona suporte a Controllers e Views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Rota padrão é Autenticacao/Login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Autenticacao}/{action=Login}/{id?}");

app.Run();