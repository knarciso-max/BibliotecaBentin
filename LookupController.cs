using BibliotecaBentin.Data;
using BibliotecaBentin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaBentin.Controllers;

public class LookupController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Usuarios(string term) =>
        Json(await db.UsuariosBiblioteca.Where(u => u.Activo && (u.Nombres.Contains(term) || u.Codigo.Contains(term) || u.Dni.Contains(term)))
            .OrderBy(u => u.Nombres).Take(10).Select(u => new { value = u.Codigo, label = u.Nombres + " · " + u.Dni }).ToListAsync());

    public async Task<IActionResult> Libros(string term) =>
        Json(await db.Libros.Where(l => l.Activo && (l.Titulo.Contains(term) || l.Codigo.Contains(term) || l.Autor.Contains(term)))
            .OrderBy(l => l.Titulo).Take(10)
            .Select(l => new { value = l.Codigo, label = l.Titulo + " · disp. " + l.Ejemplares.Count(e => e.Estado == EstadoEjemplar.Disponible) }).ToListAsync());
}
