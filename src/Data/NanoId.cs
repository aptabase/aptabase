using NanoidDotNet;

namespace Aptabase.Data;

public class NanoId
{
    private const string ALPHABET = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string NUMBERS = "0123456789";

    public static string New(string preffix) => $"{preffix}{Nanoid.Generate(ALPHABET, 22 - preffix.Length)}";
    public static string New() => New("");
    public static string Numbers(int len) => Nanoid.Generate(NUMBERS, len);
}