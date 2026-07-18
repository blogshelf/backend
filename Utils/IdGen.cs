using NanoidDotNet;

namespace backend.Utils;

public static class IdGen
{
    private const string Alphabet = Nanoid.Alphabets.SubAlphabets.Symbols +
                                    Nanoid.Alphabets.UppercaseLettersAndDigits;

    public static string New(string? self = null, int len = 64)
    {
        return self ?? Nanoid.Generate(Alphabet, len <= 0 ? 64 : len);
    }
}