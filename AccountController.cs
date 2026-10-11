using System.Security.Claims;
using BibliotecaBentin.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaBentin.Controllers;

[AllowAnonymous]
public class AccountController(AppDbContext db) : Controller
{
    [HttpGet]
    public IActionResult Login() =>
        User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Home") : View();

    [HttpPost]
    public async Task<IActionResult> Login(string usuario, string clave)
    {
        var b = await db.Bibliotecarios.FirstOrDefaultAsync(x => x.NombreUsuario == usuario && x.Activo);
        if (b is null || !BCrypt.Net.BCrypt.Verify(clave ?? "", b.PasswordHash))
        {
            ViewBag.Error = "Usuario o contraseña incorrectos.";
            return View();
        }
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, b.Id.ToString()),
            new(ClaimTypes.Name, b.NombreCompleto),
            new(ClaimTypes.Role, b.Rol)
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }
}
