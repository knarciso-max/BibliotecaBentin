using System.Security.Claims;
using BibliotecaBentin.Services;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaBentin.Controllers;

public abstract class BaseController : Controller
{
    protected int BibliotecarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Guarda el mensaje (éxito o error) y redirige.</summary>
    protected IActionResult Flash(Resultado r, string action = "Index", string? controller = null)
    {
        TempData[r.Ok ? "ok" : "err"] = r.Mensaje;
        return RedirectToAction(action, controller);
    }
}
