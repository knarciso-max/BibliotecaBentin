using BibliotecaBentin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaBentin.Controllers;

public class HomeController(ReporteService reportes) : Controller
{
    public async Task<IActionResult> Index() => View(await reportes.ResumenAsync());

    [AllowAnonymous]
    public IActionResult Error() => Content("Ocurrió un error inesperado. Intente nuevamente.");
}
