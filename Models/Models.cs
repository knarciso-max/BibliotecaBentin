using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BibliotecaBentin.Models;

public enum TipoUsuario { Estudiante = 1, Docente = 2 }
public enum EstadoEjemplar { Disponible = 1, Prestado = 2, EnMantenimiento = 3, Perdido = 4 }
public enum EstadoPrestamo { Activo = 1, Vencido = 2, Devuelto = 3, Perdido = 4 }
public enum EstadoReserva { Pendiente = 1, Lista = 2, Atendida = 3, Cancelada = 4 }
public enum EstadoSancion { Pendiente = 1, EnRevision = 2, Cumplida = 3 }

/// <summary>Cuenta de acceso al sistema (CU01, CU15).</summary>
public class Bibliotecario
{
    public int Id { get; set; }
    [Required, StringLength(50)] public string NombreUsuario { get; set; } = "";
    [Required, StringLength(120)] public string NombreCompleto { get; set; } = "";
    [Required] public string PasswordHash { get; set; } = "";   // RNF03: hash BCrypt
    [StringLength(20)] public string Rol { get; set; } = "Bibliotecario"; // Administrador | Bibliotecario
    public bool Activo { get; set; } = true;
}

/// <summary>Estudiante o docente que pide libros (clases Estudiante/Docente del informe, con Tipo como discriminador).</summary>
public class UsuarioBiblioteca
{
    public int Id { get; set; }
    [Required, StringLength(20), Display(Name = "Código")] public string Codigo { get; set; } = "";
    [Required, RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI debe tener 8 dígitos.")] public string Dni { get; set; } = "";
    [Required, StringLength(120), Display(Name = "Nombres y apellidos")] public string Nombres { get; set; } = "";
    [StringLength(20)] public string? Grado { get; set; }
    [StringLength(10), Display(Name = "Sección")] public string? Seccion { get; set; }
    public TipoUsuario Tipo { get; set; } = TipoUsuario.Estudiante;
    public bool Activo { get; set; } = true;

    [NotMapped] public int DiasPrestamo => Tipo == TipoUsuario.Docente ? 14 : 7; // regla por rol
    public List<Prestamo> Prestamos { get; set; } = new();
    public List<Sancion> Sanciones { get; set; } = new();
}

public class Libro
{
    public int Id { get; set; }
    [Required, StringLength(20), Display(Name = "Código")] public string Codigo { get; set; } = "";
    [Required, StringLength(200), Display(Name = "Título")] public string Titulo { get; set; } = "";
    [Required, StringLength(150)] public string Autor { get; set; } = "";
    [Required, StringLength(60), Display(Name = "Categoría")] public string Categoria { get; set; } = "";
    [StringLength(100)] public string? Editorial { get; set; }
    [Range(1500, 2100, ErrorMessage = "Año no válido."), Display(Name = "Año")] public int? Anio { get; set; }
    public bool Activo { get; set; } = true;
    public List<Ejemplar> Ejemplares { get; set; } = new();

    [NotMapped, Range(0, 500), Display(Name = "Ejemplares a registrar")] public int EjemplaresNuevos { get; set; } = 1;
    [NotMapped] public int Disponibles => Ejemplares.Count(e => e.Estado == EstadoEjemplar.Disponible);
    [NotMapped] public int Total => Ejemplares.Count;
}

/// <summary>Copia física con código de barras único.</summary>
public class Ejemplar
{
    public int Id { get; set; }
    public int LibroId { get; set; }
    public Libro Libro { get; set; } = null!;
    [Required, StringLength(40)] public string CodigoBarra { get; set; } = "";
    public EstadoEjemplar Estado { get; set; } = EstadoEjemplar.Disponible;
}

public class Prestamo
{
    public int Id { get; set; }
    [StringLength(20)] public string Codigo { get; set; } = "";
    public int UsuarioBibliotecaId { get; set; }
    public UsuarioBiblioteca UsuarioBiblioteca { get; set; } = null!;
    public int BibliotecarioId { get; set; }
    public Bibliotecario Bibliotecario { get; set; } = null!;
    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public DateTime? FechaDevolucion { get; set; }
    public int Renovaciones { get; set; }
    public EstadoPrestamo Estado { get; set; } = EstadoPrestamo.Activo;
    [StringLength(200)] public string? Observacion { get; set; }
    public List<DetallePrestamo> Detalles { get; set; } = new();

    [NotMapped] public string Titulos => string.Join(", ", Detalles.Select(d => d.Libro.Titulo));
}

public class DetallePrestamo
{
    public int Id { get; set; }
    public int PrestamoId { get; set; }
    public Prestamo Prestamo { get; set; } = null!;
    public int EjemplarId { get; set; }
    public Ejemplar Ejemplar { get; set; } = null!;
    public int LibroId { get; set; }
    public Libro Libro { get; set; } = null!; // ON DELETE RESTRICT (informe 5.6.5)
}

public class Reserva
{
    public int Id { get; set; }
    public int UsuarioBibliotecaId { get; set; }
    public UsuarioBiblioteca UsuarioBiblioteca { get; set; } = null!;
    public int LibroId { get; set; }
    public Libro Libro { get; set; } = null!;
    public DateTime Fecha { get; set; }
    public EstadoReserva Estado { get; set; } = EstadoReserva.Pendiente;
}

public class Sancion
{
    public int Id { get; set; }
    public int UsuarioBibliotecaId { get; set; }
    public UsuarioBiblioteca UsuarioBiblioteca { get; set; } = null!;
    public int? PrestamoId { get; set; }
    public Prestamo? Prestamo { get; set; }
    [Required, StringLength(30)] public string Motivo { get; set; } = "Atraso"; // Atraso | Pérdida | Daño | Otro
    [StringLength(250)] public string? Descripcion { get; set; }
    public int DiasMora { get; set; }
    public DateTime Fecha { get; set; }
    public EstadoSancion Estado { get; set; } = EstadoSancion.Pendiente;
}

public static class Ui
{
    public static string Badge(object estado) => estado switch
    {
        EstadoPrestamo.Activo or EstadoEjemplar.Prestado => "loan",
        EstadoPrestamo.Vencido or EstadoPrestamo.Perdido or EstadoEjemplar.Perdido => "late",
        EstadoEjemplar.Disponible or EstadoPrestamo.Devuelto or EstadoReserva.Lista
            or EstadoReserva.Atendida or EstadoSancion.Cumplida => "available",
        _ => "reserved"
    };

    public static string Texto(object estado) => estado.ToString() switch
    {
        "EnRevision" => "En revisión",
        "EnMantenimiento" => "En mantenimiento",
        var s => s ?? ""
    };
}
