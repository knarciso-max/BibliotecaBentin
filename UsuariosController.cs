using BibliotecaBentin.Models;
using BibliotecaBentin.Services;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaBentin.Controllers;

public class UsuariosController(UsuarioBibliotecaService svc) : BaseController
{
    public async Task<IActionResult> Index(string? q)
    {
        ViewBag.Q = q;
        return View(await svc.ListarAsync(q));
    }

    public IActionResult Crear() => View(new UsuarioBiblioteca());

    [HttpPost]
    public async Task<IActionResult> Crear(UsuarioBiblioteca m)
    {
        if (!ModelState.IsValid) return View(m);
        var r = await svc.CrearAsync(m);
        if (!r.Ok) { ModelState.AddModelError("", r.Mensaje); return View(m); }
        return Flash(r);
    }

    public async Task<IActionResult> Editar(int id)
    {
        var u = await svc.ObtenerAsync(id);
        return u is null ? NotFound() : View(u);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(UsuarioBiblioteca m)
    {
        ModelState.Remove(nameof(UsuarioBiblioteca.Codigo));
        if (!ModelState.IsValid) return View(m);
        var r = await svc.ActualizarAsync(m);
        if (!r.Ok) { ModelState.AddModelError("", r.Mensaje); return View(m); }
        return Flash(r);
    }

    [HttpPost]
    public async Task<IActionResult> CambiarEstado(int id) => Flash(await svc.CambiarEstadoAsync(id));

    public async Task<IActionResult> Historial(int id)
    {
        var u = await svc.ObtenerAsync(id);
        if (u is null) return NotFound();
        ViewBag.Usuario = u;
        return View(await svc.HistorialAsync(id));
    }
}
