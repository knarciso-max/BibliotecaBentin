using BibliotecaBentin.Data;
using BibliotecaBentin.Models;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaBentin.Services;

public record UsuarioFila(UsuarioBiblioteca U, int Activos, int Vencidos, int Sanciones);

/// <summary>CU05, CU11: usuarios de la biblioteca e historial.</summary>
public class UsuarioBibliotecaService(AppDbContext db)
{
    public async Task<List<UsuarioFila>> ListarAsync(string? q)
    {
        var query = db.UsuariosBiblioteca.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(u => u.Nombres.Contains(q) || u.Codigo.Contains(q) || u.Dni.Contains(q));
        return await query.OrderBy(u => u.Nombres).Take(300).Select(u => new UsuarioFila(
            u,
            u.Prestamos.Count(p => p.Estado == EstadoPrestamo.Activo || p.Estado == EstadoPrestamo.Vencido),
            u.Prestamos.Count(p => p.Estado == EstadoPrestamo.Vencido),
            u.Sanciones.Count(s => s.Estado != EstadoSancion.Cumplida))).ToListAsync();
    }

    public Task<UsuarioBiblioteca?> ObtenerAsync(int id) => db.UsuariosBiblioteca.FindAsync(id).AsTask();

    public Task<UsuarioBiblioteca?> BuscarPorClaveAsync(string clave) =>
        db.UsuariosBiblioteca.FirstOrDefaultAsync(u => u.Activo && (u.Codigo == clave.Trim() || u.Dni == clave.Trim()));

    public async Task<Resultado> CrearAsync(UsuarioBiblioteca u)
    {
        u.Codigo = u.Codigo.Trim().ToUpper();
        if (await db.UsuariosBiblioteca.AnyAsync(x => x.Codigo == u.Codigo || x.Dni == u.Dni))
            return Resultado.Error("Ya existe un usuario con ese código o DNI.");
        db.UsuariosBiblioteca.Add(u);
        await db.SaveChangesAsync();
        return Resultado.Exito($"Usuario {u.Nombres} registrado.");
    }

    public async Task<Resultado> ActualizarAsync(UsuarioBiblioteca d)
    {
        var u = await db.UsuariosBiblioteca.FindAsync(d.Id);
        if (u is null) return Resultado.Error("Usuario no encontrado.");
        if (await db.UsuariosBiblioteca.AnyAsync(x => x.Id != d.Id && x.Dni == d.Dni))
            return Resultado.Error("Otro usuario ya tiene ese DNI.");
        u.Nombres = d.Nombres; u.Dni = d.Dni; u.Grado = d.Grado; u.Seccion = d.Seccion; u.Tipo = d.Tipo;
        await db.SaveChangesAsync();
        return Resultado.Exito("Usuario actualizado.");
    }

    public async Task<Resultado> CambiarEstadoAsync(int id)
    {
        var u = await db.UsuariosBiblioteca.FindAsync(id);
        if (u is null) return Resultado.Error("Usuario no encontrado.");
        if (u.Activo && await db.Prestamos.AnyAsync(p => p.UsuarioBibliotecaId == id &&
                (p.Estado == EstadoPrestamo.Activo || p.Estado == EstadoPrestamo.Vencido)))
            return Resultado.Error("No se puede desactivar: tiene préstamos pendientes de devolución.");
        u.Activo = !u.Activo;
        await db.SaveChangesAsync();
        return Resultado.Exito(u.Activo ? "Usuario activado." : "Usuario desactivado.");
    }

    public Task<List<Prestamo>> HistorialAsync(int id) =>
        db.Prestamos.Include(p => p.Detalles).ThenInclude(d => d.Libro)
            .Where(p => p.UsuarioBibliotecaId == id).OrderByDescending(p => p.FechaPrestamo).ToListAsync();
}
