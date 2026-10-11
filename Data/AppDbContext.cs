using BibliotecaBentin.Models;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaBentin.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Bibliotecario> Bibliotecarios => Set<Bibliotecario>();
    public DbSet<UsuarioBiblioteca> UsuariosBiblioteca => Set<UsuarioBiblioteca>();
    public DbSet<Libro> Libros => Set<Libro>();
    public DbSet<Ejemplar> Ejemplares => Set<Ejemplar>();
    public DbSet<Prestamo> Prestamos => Set<Prestamo>();
    public DbSet<DetallePrestamo> DetallesPrestamo => Set<DetallePrestamo>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<Sancion> Sanciones => Set<Sancion>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Bibliotecario>().HasIndex(x => x.NombreUsuario).IsUnique();
        mb.Entity<UsuarioBiblioteca>().HasIndex(x => x.Codigo).IsUnique();
        mb.Entity<UsuarioBiblioteca>().HasIndex(x => x.Dni).IsUnique();
        mb.Entity<Libro>().HasIndex(x => x.Codigo).IsUnique();
        mb.Entity<Libro>().HasIndex(x => x.Titulo); // RNF02: búsquedas < 2 s
        mb.Entity<Ejemplar>().HasIndex(x => x.CodigoBarra).IsUnique();

        // Integridad referencial: todo RESTRICT salvo Prestamo -> Detalle (informe 5.6.5)
        foreach (var fk in mb.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        mb.Entity<DetallePrestamo>().HasOne(d => d.Prestamo).WithMany(p => p.Detalles)
            .HasForeignKey(d => d.PrestamoId).OnDelete(DeleteBehavior.Cascade);

        // Enums legibles en la base de datos
        mb.Entity<UsuarioBiblioteca>().Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20);
        mb.Entity<Ejemplar>().Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        mb.Entity<Prestamo>().Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        mb.Entity<Reserva>().Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        mb.Entity<Sancion>().Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
    }
}

public static class DbSeeder
{
    public static void Seed(AppDbContext db, bool datosDemo)
    {
        db.Database.EnsureCreated();

        if (!db.Bibliotecarios.Any())
        {
            db.Bibliotecarios.Add(new Bibliotecario
            {
                NombreUsuario = "bibliotecaria",
                NombreCompleto = "Bibliotecaria",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Bentin2026!"),
                Rol = "Administrador"
            });
        }

        if (datosDemo && !db.Libros.Any())
        {
            void L(string cod, string t, string a, string c, int n) => db.Libros.Add(new Libro
            {
                Codigo = cod, Titulo = t, Autor = a, Categoria = c,
                Ejemplares = Enumerable.Range(1, n).Select(i => new Ejemplar { CodigoBarra = $"{cod}-{i:000}" }).ToList()
            });
            L("LIB-001", "El principito", "Antoine de Saint-Exupéry", "Literatura", 4);
            L("LIB-002", "Cien años de soledad", "Gabriel García Márquez", "Literatura", 2);
            L("LIB-003", "La ciudad y los perros", "Mario Vargas Llosa", "Literatura", 3);
            L("LIB-004", "Historia del Perú", "Varios autores", "Historia", 5);
            L("LIB-005", "Matemática básica", "Varios autores", "Ciencias", 3);
            L("LIB-006", "Atlas escolar", "Varios autores", "Ciencias", 2);

            db.UsuariosBiblioteca.AddRange(
                new UsuarioBiblioteca { Codigo = "EST-1024", Dni = "70000001", Nombres = "Andrea López", Grado = "3°", Seccion = "A" },
                new UsuarioBiblioteca { Codigo = "EST-1150", Dni = "70000002", Nombres = "Diego Ramírez", Grado = "4°", Seccion = "B" },
                new UsuarioBiblioteca { Codigo = "DOC-0042", Dni = "70000003", Nombres = "María Torres", Tipo = TipoUsuario.Docente });
        }
        db.SaveChanges();
    }
}
