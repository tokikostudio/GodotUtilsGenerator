using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Tokiko.SourceGenerators;

public static class StringExtensions
{
    private static readonly Regex _digit = new(@"[_0-9]+[a-z]", RegexOptions.IgnoreCase);

    public static string SnakeToPascalCase(this string word)
    {
        var splitChars = new[]
        {
            '_'
        };

        var pascalCase = word.Split(splitChars, StringSplitOptions.RemoveEmptyEntries)
                             .Select(s => char.ToUpperInvariant(s[0]) + s.Substring(1, s.Length - 1))
                             .Aggregate(string.Empty, (s1, s2) => s1  + s2);

        if (Regex.IsMatch(pascalCase, @"^[0-9]"))
            pascalCase = "_" + pascalCase;

        return _digit.Replace(pascalCase, m => m.ToString().ToUpperInvariant());
    }
}