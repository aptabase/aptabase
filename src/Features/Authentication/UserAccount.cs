using System.Text;
using System.Security.Cryptography;

namespace Aptabase.Features.Authentication;

public class UserAccount
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string AvatarUrl => GetAvatarUrl();
    public char? LockReason { get; set; }

    public UserAccount()
    {

    }

    public UserAccount(UserIdentity identity)
    {
        Id = identity.Id;
        Name = identity.Name;
        Email = identity.Email;
    }

    private static MD5 md5 = MD5.Create();
    private string GetAvatarUrl()
    {
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(Email.Trim().ToLower()));
        var sb = new StringBuilder();
        foreach (var b in hash)
            sb.Append(b.ToString("x2"));

        return $"https://aptabase.com/avatar/{sb}?s=80&d=404";
    }
}