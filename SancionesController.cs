using BibliotecaBentin.Models;
using BibliotecaBentin.Services;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaBentin.Controllers;

public class SancionesController(SancionService svc) : BaseController
{
    public async Task<IActionResult> Index() => View(await svc.ListarAsync());

    [HttpPost]
    public async Task<IActionResult> Crear(string usuario, string motivo, string? descripcion) =>
        Flash(await svc.CrearAsync(usuario ?? "", motivo, descripcion));

    [HttpPost] public async Task<IActionResult> Cumplida(int id) => Flash(await svc.CambiarEstadoAsync(id, EstadoSancion.Cumplida));
}
