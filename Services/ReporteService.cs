using System.Text;
using BibliotecaBentin.Data;
using BibliotecaBentin.Models;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaBentin.Services;

public record Resumen(int Libros, int Activos, int Vencidos, int Reservas, int DevolucionesHoy, List<(string Dia, int N)> Semana);
public record LibroUso(int LibroId, string Titulo, int Prestamos, int Disponibles, int Total);

/// <summary>RF15, RF16: reportes para la dirección académica y dashboard.</summary>
public class ReporteService(AppDbContext db, PrestamoService prestamos)
{
    public async Task<Resumen> ResumenAsync()
    {
        await prestamos.ActualizarVencidosAsync();
        var hoy = DateTime.Today;
        var desde = hoy.AddDays(-6);
        var porDia = await db.Prestamos.Where(p => p.FechaPrestamo >= desde)
            .GroupBy(p => p.FechaPrestamo).Select(g => new { g.Key, N = g.Count() }).ToListAsync();
        var semana = Enumerable.Range(0, 7).Select(i => desde.AddDays(i))
            .Select(d => (d.ToString("ddd"), porDia.FirstOrDefault(x => x.Key == d)?.N ?? 0)).ToList();
        return new Resumen(
            await db.Libros.CountAsync(l => l.Activo),
            await db.Prestamos.CountAsync(p => p.Estado == EstadoPrestamo.Activo || p.Estado == EstadoPrestamo.Vencido),
            await db.Prestamos.CountAsync(p => p.Estado == EstadoPrestamo.Vencido),
            await db.Reservas.CountAsync(r => r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Lista),
            await db.Prestamos.CountAsync(p => p.FechaDevolucion == hoy),
            semana);
    }

    public async Task<List<LibroUso>> MasPrestadosAsync(DateTime desde, DateTime hasta, int top = 10)
    {
        var uso = await db.DetallesPrestamo
            .Where(d => d.Prestamo.FechaPrestamo >= desde && d.Prestamo.FechaPrestamo <= hasta)
            .GroupBy(d => new { d.LibroId, d.Libro.Titulo })
            .Select(g => new { g.Key.LibroId, g.Key.Titulo, N = g.Count() })
            .OrderByDescending(x => x.N).Take(top).ToListAsync();
        var ids = uso.Select(x => x.LibroId).ToList();
        var stock = await db.Ejemplares.Where(e => ids.Contains(e.LibroId)).GroupBy(e => e.LibroId)
            .Select(g => new { Id = g.Key, Total = g.Count(), Disp = g.Count(e => e.Estado == EstadoEjemplar.Disponible) }).ToListAsync();
        return uso.Select(x =>
        {
            var s = stock.FirstOrDefault(y => y.Id == x.LibroId);
            return new LibroUso(x.LibroId, x.Titulo, x.N, s?.Disp ?? 0, s?.Total ?? 0);
        }).ToList();
    }

    public async Task<List<Prestamo>> VencidosAsync()
    {
        await prestamos.ActualizarVencidosAsync();
        return await db.Prestamos.Include(p => p.UsuarioBiblioteca).Include(p => p.Detalles).ThenInclude(d => d.Libro)
            .Where(p => p.Estado == EstadoPrestamo.Vencido).OrderBy(p => p.FechaVencimiento).ToListAsync();
    }

    public Task<int> PrestamosDelPeriodoAsync(DateTime desde, DateTime hasta) =>
        db.Prestamos.CountAsync(p => p.FechaPrestamo >= desde && p.FechaPrestamo <= hasta);

    public async Task<byte[]> CsvAsync(DateTime desde, DateTime hasta)
    {
        var sb = new StringBuilder("Prestamo;Usuario;Codigo usuario;Libro;Fecha prestamo;Vencimiento;Devolucion;Estado\n");
        var lista = await db.Prestamos.Include(p => p.UsuarioBiblioteca).Include(p => p.Detalles).ThenInclude(d => d.Libro)
            .Where(p => p.FechaPrestamo >= desde && p.FechaPrestamo <= hasta).OrderBy(p => p.FechaPrestamo).ToListAsync();
        static string C(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
        foreach (var p in lista)
            sb.AppendLine(string.Join(';', C(p.Codigo), C(p.UsuarioBiblioteca.Nombres), C(p.UsuarioBiblioteca.Codigo), C(p.Titulos),
                p.FechaPrestamo.ToString("dd/MM/yyyy"), p.FechaVencimiento.ToString("dd/MM/yyyy"),
                p.FechaDevolucion?.ToString("dd/MM/yyyy") ?? "", p.Estado));
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }
}
