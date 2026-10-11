using BibliotecaBentin.Data;
using BibliotecaBentin.Models;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaBentin.Services;

/// <summary>Reglas de negocio del préstamo (CU07, CU08, CU09, CU14). Máquina de estados: Activo/Vencido/Devuelto/Perdido.</summary>
public class PrestamoService(AppDbContext db)
{
    public const int MaxRenovaciones = 2;
    public const int DiasRenovacion = 7;

    /// <summary>Activo -> Vencido cuando hoy > fechaVencimiento (RF10).</summary>
    public Task ActualizarVencidosAsync()
    {
        var hoy = DateTime.Today;
        return db.Prestamos.Where(p => p.Estado == EstadoPrestamo.Activo && p.FechaVencimiento < hoy)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Estado, EstadoPrestamo.Vencido));
    }

    public async Task<List<Prestamo>> ListarAsync(string? q, string? estado, bool soloPendientes = false)
    {
        await ActualizarVencidosAsync();
        var query = db.Prestamos.Include(p => p.UsuarioBiblioteca)
            .Include(p => p.Detalles).ThenInclude(d => d.Libro).AsQueryable();
        if (soloPendientes)
            query = query.Where(p => p.Estado == EstadoPrestamo.Activo || p.Estado == EstadoPrestamo.Vencido);
        if (Enum.TryParse<EstadoPrestamo>(estado, out var e)) query = query.Where(p => p.Estado == e);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Codigo.Contains(q) || p.UsuarioBiblioteca.Nombres.Contains(q) ||
                p.UsuarioBiblioteca.Codigo.Contains(q) || p.UsuarioBiblioteca.Dni.Contains(q) ||
                p.Detalles.Any(d => d.Libro.Titulo.Contains(q)));
        return await query.OrderByDescending(p => p.Id).Take(200).ToListAsync();
    }

    public async Task<Resultado> RegistrarAsync(string claveUsuario, string claveLibro, int bibliotecarioId, string? observacion)
    {
        await ActualizarVencidosAsync();
        claveUsuario = claveUsuario.Trim(); claveLibro = claveLibro.Trim().ToUpper();

        var u = await db.UsuariosBiblioteca.FirstOrDefaultAsync(x => x.Activo && (x.Codigo == claveUsuario || x.Dni == claveUsuario));
        if (u is null) return Resultado.Error("Usuario no encontrado o inactivo.");

        if (await db.Sanciones.AnyAsync(s => s.UsuarioBibliotecaId == u.Id && s.Estado != EstadoSancion.Cumplida))
            return Resultado.Error($"{u.Nombres} tiene una sanción pendiente. No se puede registrar el préstamo.");
        if (await db.Prestamos.AnyAsync(p => p.UsuarioBibliotecaId == u.Id && p.Estado == EstadoPrestamo.Vencido))
            return Resultado.Error($"{u.Nombres} tiene un préstamo vencido sin devolver.");

        var libro = await db.Libros.FirstOrDefaultAsync(l => l.Activo && l.Codigo == claveLibro);
        if (libro is null) return Resultado.Error("Libro no encontrado en el catálogo.");

        var ejemplar = await db.Ejemplares.Where(e => e.LibroId == libro.Id && e.Estado == EstadoEjemplar.Disponible)
            .OrderBy(e => e.Id).FirstOrDefaultAsync();
        if (ejemplar is null)
            return Resultado.Error($"\"{libro.Titulo}\" no tiene ejemplares disponibles. Puede registrar una reserva.");

        var hoy = DateTime.Today;
        var p = new Prestamo
        {
            UsuarioBibliotecaId = u.Id, BibliotecarioId = bibliotecarioId,
            FechaPrestamo = hoy, FechaVencimiento = hoy.AddDays(u.DiasPrestamo),
            Estado = EstadoPrestamo.Activo, Observacion = observacion
        };
        p.Detalles.Add(new DetallePrestamo { EjemplarId = ejemplar.Id, LibroId = libro.Id });
        ejemplar.Estado = EstadoEjemplar.Prestado;

        // Si el usuario tenía una reserva vigente de este libro, queda atendida.
        var reserva = await db.Reservas.FirstOrDefaultAsync(r => r.UsuarioBibliotecaId == u.Id && r.LibroId == libro.Id &&
            (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Lista));
        if (reserva is not null) reserva.Estado = EstadoReserva.Atendida;

        await using var tx = await db.Database.BeginTransactionAsync();
        db.Prestamos.Add(p);
        await db.SaveChangesAsync();
        p.Codigo = $"PRE-{10000 + p.Id}";
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return Resultado.Exito($"Préstamo {p.Codigo} registrado. Vence el {p.FechaVencimiento:dd/MM/yyyy}.");
    }

    public async Task<Resultado> DevolverAsync(int id)
    {
        var p = await CargarAsync(id);
        if (p is null || p.Estado is EstadoPrestamo.Devuelto or EstadoPrestamo.Perdido)
            return Resultado.Error("El préstamo no existe o ya está cerrado.");

        var hoy = DateTime.Today;
        p.FechaDevolucion = hoy;
        p.Estado = EstadoPrestamo.Devuelto;
        foreach (var d in p.Detalles) d.Ejemplar.Estado = EstadoEjemplar.Disponible;

        var msg = $"Devolución de {p.Codigo} registrada.";
        if (hoy > p.FechaVencimiento)
        {
            var dias = (hoy - p.FechaVencimiento).Days;
            db.Sanciones.Add(new Sancion
            {
                UsuarioBibliotecaId = p.UsuarioBibliotecaId, PrestamoId = p.Id, Motivo = "Atraso",
                Descripcion = $"Devolución tardía de {p.Titulos}: {dias} día(s) de mora.",
                DiasMora = dias, Fecha = hoy, Estado = EstadoSancion.Pendiente
            });
            msg += $" Devolución tardía: {dias} día(s) de mora, se generó una sanción.";
        }

        // Si hay reservas en espera, la más antigua pasa a "Lista".
        foreach (var libroId in p.Detalles.Select(d => d.LibroId).Distinct())
        {
            var r = await db.Reservas.Where(x => x.LibroId == libroId && x.Estado == EstadoReserva.Pendiente)
                .OrderBy(x => x.Fecha).FirstOrDefaultAsync();
            if (r is not null) r.Estado = EstadoReserva.Lista;
        }
        await db.SaveChangesAsync();
        return Resultado.Exito(msg);
    }

    public async Task<Resultado> RenovarAsync(int id)
    {
        await ActualizarVencidosAsync();
        var p = await db.Prestamos.FindAsync(id);
        if (p is null) return Resultado.Error("Préstamo no encontrado.");
        if (p.Estado != EstadoPrestamo.Activo) return Resultado.Error("Solo se pueden renovar préstamos activos (no vencidos).");
        if (p.Renovaciones >= MaxRenovaciones) return Resultado.Error($"Se alcanzó el máximo de {MaxRenovaciones} renovaciones.");
        p.FechaVencimiento = p.FechaVencimiento.AddDays(DiasRenovacion);
        p.Renovaciones++;
        await db.SaveChangesAsync();
        return Resultado.Exito($"Préstamo renovado. Nuevo vencimiento: {p.FechaVencimiento:dd/MM/yyyy}.");
    }

    public async Task<Resultado> ReportarPerdidaAsync(int id)
    {
        var p = await CargarAsync(id);
        if (p is null || p.Estado is EstadoPrestamo.Devuelto or EstadoPrestamo.Perdido)
            return Resultado.Error("El préstamo no existe o ya está cerrado.");
        p.Estado = EstadoPrestamo.Perdido;
        foreach (var d in p.Detalles) d.Ejemplar.Estado = EstadoEjemplar.Perdido;
        db.Sanciones.Add(new Sancion
        {
            UsuarioBibliotecaId = p.UsuarioBibliotecaId, PrestamoId = p.Id, Motivo = "Pérdida",
            Descripcion = $"Pérdida de {p.Titulos}.", Fecha = DateTime.Today, Estado = EstadoSancion.Pendiente
        });
        await db.SaveChangesAsync();
        return Resultado.Exito($"{p.Codigo} marcado como perdido. Se registró la sanción correspondiente.");
    }

    private Task<Prestamo?> CargarAsync(int id) =>
        db.Prestamos.Include(x => x.Detalles).ThenInclude(d => d.Ejemplar)
            .Include(x => x.Detalles).ThenInclude(d => d.Libro).FirstOrDefaultAsync(x => x.Id == id);
}
