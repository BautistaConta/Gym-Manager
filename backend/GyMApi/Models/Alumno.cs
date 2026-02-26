namespace GymManager.API.Models
{   
public class Alumno
{
    public string Id { get; set; }
    public string Nombre { get; set; }
    public string DNI { get; set; }
    public string Telefono { get; set; }
    public DateTime FechaAlta { get; set; }
    public bool Activo { get; set; }
}
}