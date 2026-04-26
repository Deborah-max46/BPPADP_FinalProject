namespace ConsumersVoiceSystemPrototype.Models;

public enum UserRole
{
    Consumer,
    Advocate,
    Admin,
    Business
}

public static class RoleNames
{
    public const string Consumer = nameof(UserRole.Consumer);
    public const string Advocate = nameof(UserRole.Advocate);
    public const string Admin = nameof(UserRole.Admin);
    public const string Business = nameof(UserRole.Business);
}
