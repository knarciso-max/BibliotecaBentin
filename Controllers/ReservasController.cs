using BibliotecaBentin.Models;
using BibliotecaBentin.Services;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaBentin.Controllers;

public class ReservasController(ReservaService svc) : BaseController
{
    public async Task<IActionResult> Index(string? libro) { ViewBag.Libro = libro; return View(await svc.ListarAsync()); }

    [HttpPost]
    public async Task<IActionResult> Crear(string usuario, string libro) => Flash(await svc.CrearAsync(usuario ?? "", libro ?? ""));

    [HttpPost] public async Task<IActionResult> Lista(int id) => Flash(await svc.CambiarEstadoAsync(id, EstadoReserva.Lista));
    [HttpPost] public async Task<IActionResult> Cancelar(int id) => Flash(await svc.CambiarEstadoAsync(id, EstadoReserva.Cancelada));
}
