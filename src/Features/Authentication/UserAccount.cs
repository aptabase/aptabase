using System.Security.Cryptography;
using System.Text;

namespace Aptabase.Features.Authentication;

public class UserAccount
{
    private static MD5 md5 = MD5.Create();

    public string Id { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string AvatarUrl { get; private set; } = "";

    public UserAccount(string id, string name, string email)
    {
        Id = id;
        Name = name;
        Email = email;
        AvatarUrl = GetAvatarUrl(email);
    }

    private string GetAvatarUrl(string email)
    {
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(email.Trim().ToLower()));
        var sb = new StringBuilder();
        foreach (var b in hash)
            sb.Append(b.ToString("x2"));

        return $"https://aptabase.com/avatar/{sb.ToString()}?s=80&d=404";
    }
}

public enum UserRole
{
    Admin = 1,
}