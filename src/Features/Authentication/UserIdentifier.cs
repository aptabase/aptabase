namespace Aptabase.Features.Authentication;

public struct UserIdentity
{
    public string Id { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string Email { get; private set; } = "";

    public UserIdentity(string id, string name, string email)
    {
        Id = id;
        Name = name;
        Email = email;
    }
}