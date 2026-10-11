using BibliotecaBentin.Models;
using BibliotecaBentin.Services;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaBentin.Controllers;

public class LibrosController(LibroService svc) : BaseController
{
    public async Task<IActionResult> Index(string? q, string? categoria)
    {
        ViewBag.Q = q; ViewBag.Categoria = categoria;
        ViewBag.Categorias = await svc.CategoriasAsync();
        return View(await svc.BuscarAsync(q, categoria));
    }

    public IActionResult Crear() => View(new Libro { Anio = DateTime.Today.Year });

    [HttpPost]
    public async Task<IActionResult> Crear(Libro m)
    {
        if (!ModelState.IsValid) return View(m);
        var r = await svc.CrearAsync(m);
        if (!r.Ok) { ModelState.AddModelError("", r.Mensaje); return View(m); }
        return Flash(r);
    }

    public async Task<IActionResult> Editar(int id)
    {
        var l = await svc.ObtenerAsync(id);
        if (l is null) return NotFound();
        l.EjemplaresNuevos = 0;
        return View(l);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(Libro m)
    {
        if (!ModelState.IsValid) return View(m);
        return Flash(await svc.ActualizarAsync(m));
    }

    [HttpPost]
    public async Task<IActionResult> Desactivar(int id) => Flash(await svc.DesactivarAsync(id));
}
