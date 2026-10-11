using BibliotecaBentin.Services;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaBentin.Controllers;

public class PrestamosController(PrestamoService svc) : BaseController
{
    public async Task<IActionResult> Index(string? q, string? estado)
    {
        ViewBag.Q = q; ViewBag.Estado = estado;
        return View(await svc.ListarAsync(q, estado));
    }

    public IActionResult Nuevo(string? usuario, string? libro)
    {
        ViewBag.Usuario = usuario; ViewBag.Libro = libro;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Nuevo(string usuario, string libro, string? observacion)
    {
        var r = await svc.RegistrarAsync(usuario ?? "", libro ?? "", BibliotecarioId, observacion);
        if (r.Ok) return Flash(r);
        TempData["err"] = r.Mensaje;
        return RedirectToAction(nameof(Nuevo), new { usuario, libro });
    }

    [HttpPost] public async Task<IActionResult> Renovar(int id) => Flash(await svc.RenovarAsync(id));
    [HttpPost] public async Task<IActionResult> Perdido(int id) => Flash(await svc.ReportarPerdidaAsync(id));
}
