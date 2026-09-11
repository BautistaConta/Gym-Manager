using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using GymApi.Models.Roles;

namespace GymManager.API.Models
{
    public class Usuario : IGymOwned
    {
        public string GymId { get; set; } = null!;
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        [BsonElement("nombre")]
        public string Nombre { get; set; } = null!;

        [BsonElement("email")]
        public string Email { get; set; } = null!;

        [BsonElement("emailNormalizado")]
        public string EmailNormalizado { get; set; } = null!;

        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; } = null!;

        [BsonElement("rol")]
        public RolUsuario Rol { get; set; } = RolUsuario.Alumno;

        [BsonElement("fechaAlta")]
        public DateTime FechaAlta { get; set; } = DateTime.UtcNow;

        public string? AlumnoId { get; set; }
    }
}
