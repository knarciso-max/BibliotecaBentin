using BibliotecaBentin.Data;
using BibliotecaBentin.Models;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaBentin.Services;

/// <summary>CU10 (reservas) y CU14 (sanciones).</summary>
public class ReservaService(AppDbContext db)
{
    public Task<List<Reserva>> ListarAsync() =>
        db.Reservas.Include(r => r.UsuarioBiblioteca).Include(r => r.Libro)
            .OrderBy(r => r.Estado).ThenByDescending(r => r.Fecha).Take(200).ToListAsync();

    public async Task<Resultado> CrearAsync(string claveUsuario, string claveLibro)
    {
        var u = await db.UsuariosBiblioteca.FirstOrDefaultAsync(x => x.Activo && (x.Codigo == claveUsuario.Trim() || x.Dni == claveUsuario.Trim()));
        if (u is null) return Resultado.Error("Usuario no encontrado o inactivo.");
        var l = await db.Libros.Include(x => x.Ejemplares).FirstOrDefaultAsync(x => x.Activo && x.Codigo == claveLibro.Trim().ToUpper());
        if (l is null) return Resultado.Error("Libro no encontrado.");
        if (l.Disponibles > 0) return Resultado.Error("El libro tiene ejemplares disponibles: registre el préstamo directamente.");
        if (await db.Reservas.AnyAsync(r => r.UsuarioBibliotecaId == u.Id && r.LibroId == l.Id &&
                (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Lista)))
            return Resultado.Error("El usuario ya tiene una reserva vigente de este libro.");
        db.Reservas.Add(new Reserva { UsuarioBibliotecaId = u.Id, LibroId = l.Id, Fecha = DateTime.Today });
        await db.SaveChangesAsync();
        return Resultado.Exito($"Reserva de \"{l.Titulo}\" registrada para {u.Nombres}.");
    }

    public async Task<Resultado> CambiarEstadoAsync(int id, EstadoReserva nuevo)
    {
        var r = await db.Reservas.FindAsync(id);
        if (r is null) return Resultado.Error("Reserva no encontrada.");
        if (r.Estado is EstadoReserva.Atendida or EstadoReserva.Cancelada) return Resultado.Error("La reserva ya está cerrada.");
        r.Estado = nuevo;
        await db.SaveChangesAsync();
        return Resultado.Exito($"Reserva marcada como {nuevo}.");
    }
}

public class SancionService(AppDbContext db)
{
    public Task<List<Sancion>> ListarAsync() =>
        db.Sanciones.Include(s => s.UsuarioBiblioteca).Include(s => s.Prestamo)
            .OrderBy(s => s.Estado).ThenByDescending(s => s.Fecha).Take(200).ToListAsync();

    public async Task<Resultado> CrearAsync(string claveUsuario, string motivo, string? descripcion)
    {
        var u = await db.UsuariosBiblioteca.FirstOrDefaultAsync(x => x.Codigo == claveUsuario.Trim() || x.Dni == claveUsuario.Trim());
        if (u is null) return Resultado.Error("Usuario no encontrado.");
        db.Sanciones.Add(new Sancion
        {
            UsuarioBibliotecaId = u.Id, Motivo = motivo, Descripcion = descripcion,
            Fecha = DateTime.Today, Estado = EstadoSancion.EnRevision
        });
        await db.SaveChangesAsync();
        return Resultado.Exito($"Sanción registrada para {u.Nombres}.");
    }

    public async Task<Resultado> CambiarEstadoAsync(int id, EstadoSancion nuevo)
    {
        var s = await db.Sanciones.FindAsync(id);
        if (s is null) return Resultado.Error("Sanción no encontrada.");
        s.Estado = nuevo;
        await db.SaveChangesAsync();
        return Resultado.Exito(nuevo == EstadoSancion.Cumplida ? "Sanción cumplida: el usuario queda habilitado." : "Sanción actualizada.");
    }
}
