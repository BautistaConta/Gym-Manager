using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using GymApi.Models.Roles;

namespace GymManager.API.Models
{
    public class Usuario
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("nombre")]
        public string Nombre { get; set; }

        [BsonElement("email")]
        public string Email { get; set; }

        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; }

        [BsonElement("rol")]
        public RolUsuario Rol { get; set; } = RolUsuario.Alumno;

        [BsonElement("fechaAlta")]
        public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
    }
}
