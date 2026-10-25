// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace OSRobot.Server.Core.Variables;

public enum VariableKind
{
    /// <summary>{var['Name']}: a plain value, visible in the editor.</summary>
    Variable,
    /// <summary>{secret['Name']}: stored encrypted, never sent back to the editor, redacted from logs.</summary>
    Secret
}

/// <summary>
/// The values of the {var['Name']} and {secret['Name']} placeholders. The host passes its
/// implementation to the JobEngine (in the server: the variables stored in config.db), which
/// installs it as Core.Variables; without one no variable is defined.
/// Names are case-insensitive. Implementations must be safe to call from any thread.
/// </summary>
public interface IVariableResolver
{
    bool IsDefined(VariableKind kind, string name);

    /// <summary>The value of the variable or secret.</summary>
    /// <exception cref="ApplicationException">It is not defined, or (a secret) it cannot be decrypted.</exception>
    string Resolve(VariableKind kind, string name);

    /// <summary>
    /// Replaces every known secret value found in the text with <see cref="VariableSet.RedactedText"/>.
    /// Used on everything written to the execution logs or sent to the editors.
    /// </summary>
    string Redact(string text);
}
