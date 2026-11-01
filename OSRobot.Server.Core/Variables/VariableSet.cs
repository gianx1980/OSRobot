// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Core.Variables;

/// <summary>
/// An immutable snapshot of the defined variables and secrets. Being immutable, it can be read by
/// any number of tasks at once; a change replaces the whole snapshot.
/// </summary>
public sealed class VariableSet : IVariableResolver
{
    public const string RedactedText = "***";

    /// <summary>
    /// Shorter secret values are not redacted: replacing every "a" or "12" in a log would make it
    /// unreadable and would tell which characters the secret is made of.
    /// </summary>
    public const int MinRedactedLength = 4;

    public static readonly VariableSet Empty = new([], []);

    private readonly Dictionary<string, string> _variables;
    // A null value is a secret that exists but could not be decrypted.
    private readonly Dictionary<string, string?> _secrets;
    // Longest first, so a secret containing another one is replaced whole.
    private readonly string[] _redactedValues;

    public VariableSet(IEnumerable<KeyValuePair<string, string>> variables, IEnumerable<KeyValuePair<string, string?>> secrets)
    {
        _variables = new Dictionary<string, string>(variables, StringComparer.OrdinalIgnoreCase);
        _secrets = new Dictionary<string, string?>(secrets, StringComparer.OrdinalIgnoreCase);
        _redactedValues =
        [
            .. _secrets.Values.OfType<string>()
                              .Where(v => v.Length >= MinRedactedLength)
                              .Distinct(StringComparer.Ordinal)
                              .OrderByDescending(v => v.Length)
        ];
    }

    public bool IsDefined(VariableKind kind, string name)
    {
        return kind == VariableKind.Secret ? _secrets.ContainsKey(name) : _variables.ContainsKey(name);
    }

    public string Resolve(VariableKind kind, string name)
    {
        if (kind == VariableKind.Variable)
        {
            if (_variables.TryGetValue(name, out string? value))
                return value;

            throw new ApplicationException($"Variable '{name}' is not defined. Add it in Variables and secrets.");
        }

        if (!_secrets.TryGetValue(name, out string? secret))
            throw new ApplicationException($"Secret '{name}' is not defined. Add it in Variables and secrets.");

        return secret ?? throw new ApplicationException(
            $"Secret '{name}' cannot be decrypted: the encryption keys it was saved with are missing or unreadable. " +
            "Restore the keys folder from a backup, or enter the secret's value again.");
    }

    public string Redact(string text)
    {
        if (string.IsNullOrEmpty(text) || _redactedValues.Length == 0)
            return text;

        foreach (string value in _redactedValues)
            text = text.Replace(value, RedactedText, StringComparison.Ordinal);

        return text;
    }
}
