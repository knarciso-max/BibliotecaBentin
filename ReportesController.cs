using BibliotecaBentin.Services;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaBentin.Controllers;

public class ReportesController(ReporteService svc) : BaseController
{
    public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta)
    {
        var d = desde ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var h = hasta ?? DateTime.Today;
        ViewBag.Desde = d; ViewBag.Hasta = h;
        ViewBag.Total = await svc.PrestamosDelPeriodoAsync(d, h);
        ViewBag.Vencidos = await svc.VencidosAsync();
        return View(await svc.MasPrestadosAsync(d, h));
    }

    public async Task<IActionResult> Exportar(DateTime desde, DateTime hasta) =>
        File(await svc.CsvAsync(desde, hasta), "text/csv", $"prestamos_{desde:yyyyMMdd}_{hasta:yyyyMMdd}.csv");
}
