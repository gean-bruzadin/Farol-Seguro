using Farol_Seguro.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Configuração do DbContext para MySQL
builder.Services.AddDbContext<DbConfig>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
);

// Configuração de autenticação com Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(config =>
    {
        config.Cookie.Name = "FarolAuthCookie"; // Nome mais genérico
        config.LoginPath = "/Autenticacao/Login";

        // CORREÇÃO: Defina uma página diferente, ou comente/remova se não tiver uma.
        // Se você não definir uma, o ASP.NET Core apenas retornará um status 403 (Forbidden).
        config.AccessDeniedPath = "/Home/AcessoNegado"; // Crie uma view em Views/Home/AcessoNegado

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

// Rota padrão deve ser a Home, não o Login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=autenticacao}/{action=login}/{id?}");

app.Run();