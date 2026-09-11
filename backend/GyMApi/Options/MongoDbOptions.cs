namespace GymManager.API.Options;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDB";
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
    public string UsersCollectionName { get; set; } = "Usuarios";
}
