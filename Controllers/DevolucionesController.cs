using BibliotecaBentin.Services;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaBentin.Controllers;

public class DevolucionesController(PrestamoService svc) : BaseController
{
    public async Task<IActionResult> Index(string? q)
    {
        ViewBag.Q = q;
        return View(await svc.ListarAsync(q, null, soloPendientes: true));
    }

    [HttpPost]
    public async Task<IActionResult> Registrar(int id) => Flash(await svc.DevolverAsync(id));
}
