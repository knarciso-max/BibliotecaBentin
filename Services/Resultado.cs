namespace BibliotecaBentin.Services;

public record Resultado(bool Ok, string Mensaje)
{
    public static Resultado Exito(string m) => new(true, m);
    public static Resultado Error(string m) => new(false, m);
}
