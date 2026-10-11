using BibliotecaBentin.Data;
using BibliotecaBentin.Models;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaBentin.Services;

/// <summary>RF01-RF03, RF05, RF14: catálogo, ejemplares y disponibilidad.</summary>
public class LibroService(AppDbContext db)
{
    public Task<List<Libro>> BuscarAsync(string? q, string? categoria)
    {
        var query = db.Libros.Include(l => l.Ejemplares).Where(l => l.Activo);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(l => l.Titulo.Contains(q) || l.Autor.Contains(q) || l.Categoria.Contains(q) || l.Codigo.Contains(q));
        if (!string.IsNullOrWhiteSpace(categoria))
            query = query.Where(l => l.Categoria == categoria);
        return query.OrderBy(l => l.Titulo).Take(300).ToListAsync();
    }

    public Task<List<string>> CategoriasAsync() =>
        db.Libros.Where(l => l.Activo).Select(l => l.Categoria).Distinct().OrderBy(c => c).ToListAsync();

    public Task<Libro?> ObtenerAsync(int id) =>
        db.Libros.Include(l => l.Ejemplares).FirstOrDefaultAsync(l => l.Id == id);

    public async Task<Resultado> CrearAsync(Libro l)
    {
        l.Codigo = l.Codigo.Trim().ToUpper();
        if (await db.Libros.AnyAsync(x => x.Codigo == l.Codigo))
            return Resultado.Error($"Ya existe un libro con el código {l.Codigo}.");
        l.Ejemplares = NuevosEjemplares(l.Codigo, 0, l.EjemplaresNuevos);
        db.Libros.Add(l);
        await db.SaveChangesAsync();
        return Resultado.Exito($"Libro \"{l.Titulo}\" registrado con {l.Ejemplares.Count} ejemplar(es).");
    }

    public async Task<Resultado> ActualizarAsync(Libro datos)
    {
        var l = await ObtenerAsync(datos.Id);
        if (l is null) return Resultado.Error("Libro no encontrado.");
        l.Titulo = datos.Titulo; l.Autor = datos.Autor; l.Categoria = datos.Categoria;
        l.Editorial = datos.Editorial; l.Anio = datos.Anio;
        if (datos.EjemplaresNuevos > 0)
            l.Ejemplares.AddRange(NuevosEjemplares(l.Codigo, l.Ejemplares.Count, datos.EjemplaresNuevos));
        await db.SaveChangesAsync();
        return Resultado.Exito("Libro actualizado.");
    }

    public async Task<Resultado> DesactivarAsync(int id)
    {
        var l = await ObtenerAsync(id);
        if (l is null) return Resultado.Error("Libro no encontrado.");
        if (l.Ejemplares.Any(e => e.Estado == EstadoEjemplar.Prestado))
            return Resultado.Error("No se puede desactivar: tiene ejemplares prestados.");
        l.Activo = false; // baja lógica: se conserva el historial
        await db.SaveChangesAsync();
        return Resultado.Exito($"Libro \"{l.Titulo}\" desactivado.");
    }

    private static List<Ejemplar> NuevosEjemplares(string codigo, int existentes, int cantidad) =>
        Enumerable.Range(existentes + 1, cantidad)
            .Select(i => new Ejemplar { CodigoBarra = $"{codigo}-{i:000}" }).ToList();
}
