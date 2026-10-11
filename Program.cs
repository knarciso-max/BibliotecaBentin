using System.Globalization;
using BibliotecaBentin.Data;
using BibliotecaBentin.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Capa de datos (EF Core + SQL Server Express)
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Biblioteca")));

// Capa de negocio (Service Layer)
builder.Services.AddScoped<LibroService>();
builder.Services.AddScoped<UsuarioBibliotecaService>();
builder.Services.AddScoped<PrestamoService>();
builder.Services.AddScoped<ReservaService>();
builder.Services.AddScoped<SancionService>();
builder.Services.AddScoped<ReporteService>();

// Seguridad: cookies + todo protegido por defecto (RNF03, CAR-07)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = true;
        o.Cookie.HttpOnly = true;
    });
builder.Services.AddControllersWithViews(o =>
{
    o.Filters.Add(new AuthorizeFilter());
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

var cultura = new CultureInfo("es-PE");
CultureInfo.DefaultThreadCurrentCulture = cultura;
CultureInfo.DefaultThreadCurrentUICulture = cultura;

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    DbSeeder.Seed(scope.ServiceProvider.GetRequiredService<AppDbContext>(), datosDemo: app.Environment.IsDevelopment());

if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Home/Error");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
