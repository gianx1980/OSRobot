// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using OSRobot.Server.Core.Variables;
using OSRobot.Server.Infrastructure.DataAccess.Models;
using OSRobot.Server.Models.DTO.Variables;

namespace OSRobot.Server.Infrastructure.Variables;

/// <summary>A save refused because of what was submitted. The message is meant for the user.</summary>
public sealed class VariableValidationException(string message) : Exception(message);

public interface IVariableStore
{
    List<VariableListItem> List();

    /// <returns>True if the variable was created, false if an existing one was updated.</returns>
    /// <exception cref="VariableValidationException"/>
    bool Save(VariableSaveRequest request, string? username);

    /// <returns>False if there was no such variable.</returns>
    bool Delete(string name);
}

/// <summary>
/// The variables and secrets kept in config.db, secrets encrypted with Data Protection. It is also the
/// engine's IVariableResolver (installed as Core.Variables): it serves an in-memory snapshot, decrypted
/// once and replaced after every change, so a change applies to the next task that runs, without a
/// reload. Registered as a singleton; config.db is reached through a scope per operation.
/// </summary>
public sealed partial class VariableStore(IServiceScopeFactory scopeFactory, IDataProtectionProvider dataProtectionProvider, ILogger<VariableStore> logger)
    : IVariableStore, IVariableResolver
{
    // Changing it makes every stored secret unreadable.
    public const string ProtectorPurpose = "OSRobot.Variables.Secrets.v1";
    public const int MaxNameLength = 100;
    public const int MaxValueLength = 32768;
    public const int MaxDescriptionLength = 500;

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
    private readonly ILogger<VariableStore> _logger = logger;

    // Serializes writes and snapshot rebuilds; reads only take the current snapshot.
    private readonly object _writeLock = new();
    private volatile VariableSet? _snapshot;

    private VariableSet Snapshot => _snapshot ?? LoadOnce();

    private VariableSet LoadOnce()
    {
        // Several threads may need the first snapshot at once: only one of them reads config.db.
        lock (_writeLock)
        {
            return _snapshot ?? Reload();
        }
    }

    public bool IsDefined(VariableKind kind, string name) => Snapshot.IsDefined(kind, name);

    public string Resolve(VariableKind kind, string name) => Snapshot.Resolve(kind, name);

    public string Redact(string text)
    {
        // Called for every log line: logging must keep working even if config.db can't be read.
        try
        {
            return Snapshot.Redact(text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot load the variables to redact secrets from a log line.");
            return text;
        }
    }

    /// <summary>Rebuilds the snapshot from config.db.</summary>
    public VariableSet Reload()
    {
        lock (_writeLock)
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            RobotDBContext db = scope.ServiceProvider.GetRequiredService<RobotDBContext>();

            List<KeyValuePair<string, string>> variables = [];
            List<KeyValuePair<string, string?>> secrets = [];

            foreach (Variable variable in db.Variables.AsNoTracking())
            {
                if (variable.IsSecret)
                    secrets.Add(KeyValuePair.Create(variable.Name, TryUnprotect(variable)));
                else
                    variables.Add(KeyValuePair.Create(variable.Name, variable.Value));
            }

            VariableSet snapshot = new(variables, secrets);
            _snapshot = snapshot;
            return snapshot;
        }
    }

    public List<VariableListItem> List()
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        RobotDBContext db = scope.ServiceProvider.GetRequiredService<RobotDBContext>();
        VariableSet snapshot = Snapshot;

        return
        [
            .. db.Variables.AsNoTracking()
                           .OrderBy(v => v.Name)
                           .AsEnumerable()
                           .Select(v => new VariableListItem(v.Name, v.IsSecret, v.IsSecret ? null : v.Value, v.Description, v.UpdatedAt, v.UpdatedBy,
                                                             !v.IsSecret || IsReadable(snapshot, v.Name)))
        ];
    }

    public bool Save(VariableSaveRequest request, string? username)
    {
        string name = (request.Name ?? string.Empty).Trim();
        Validate(name, request);

        lock (_writeLock)
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            RobotDBContext db = scope.ServiceProvider.GetRequiredService<RobotDBContext>();

            // The column is COLLATE NOCASE: this finds the variable whatever the case of its name.
            Variable? variable = db.Variables.FirstOrDefault(v => v.Name == name);
            bool created = variable == null;
            bool hasNewValue = !string.IsNullOrEmpty(request.Value);

            if (variable == null)
            {
                if (request.IsSecret && !hasNewValue)
                    throw new VariableValidationException("Enter the secret's value.");

                variable = new Variable { Value = string.Empty };
                db.Variables.Add(variable);
            }

            if (request.IsSecret)
            {
                if (hasNewValue)
                    variable.Value = _protector.Protect(request.Value!);
                else if (!variable.IsSecret)
                    variable.Value = _protector.Protect(variable.Value);   // A variable becoming a secret keeps its value.
                // else: an existing secret keeps its value.
            }
            else
            {
                // Turning a secret into a variable would show its value: it must be entered again.
                if (variable.IsSecret && !hasNewValue)
                    throw new VariableValidationException("To turn a secret into a variable, enter its value again: a secret's value is never shown.");

                variable.Value = request.Value ?? string.Empty;
            }

            variable.Name = name;
            variable.IsSecret = request.IsSecret;
            variable.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            variable.UpdatedAt = DateTime.Now;
            variable.UpdatedBy = username;

            db.SaveChanges();
            Reload();

            return created;
        }
    }

    public bool Delete(string name)
    {
        lock (_writeLock)
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            RobotDBContext db = scope.ServiceProvider.GetRequiredService<RobotDBContext>();

            Variable? variable = db.Variables.FirstOrDefault(v => v.Name == name);
            if (variable == null)
                return false;

            db.Variables.Remove(variable);
            db.SaveChanges();
            Reload();

            return true;
        }
    }

    /// <summary>
    /// Re-encrypts every secret with a key created by <paramref name="createNewKey"/> (which returns its id):
    /// all secrets are decrypted first (with whatever key they were saved with), then the key is created,
    /// then they are encrypted again, now with the new key. A secret that can't be decrypted keeps its ciphertext.
    /// </summary>
    /// <returns>How many secrets were re-encrypted.</returns>
    public int ReencryptSecrets(Func<Guid> createNewKey)
    {
        lock (_writeLock)
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            RobotDBContext db = scope.ServiceProvider.GetRequiredService<RobotDBContext>();

            List<(Variable Secret, string Value)> readable = [];
            foreach (Variable secret in db.Variables.Where(v => v.IsSecret))
            {
                string? value = TryUnprotect(secret);
                if (value != null)
                    readable.Add((secret, value));
            }

            WaitUntilEncryptingWith(createNewKey());

            foreach ((Variable secret, string value) in readable)
                secret.Value = _protector.Protect(value);

            db.SaveChanges();
            Reload();

            return readable.Count;
        }
    }

    // Data Protection caches its key ring: right after a key is created, Protect may still use the previous
    // one for a moment. A protected payload starts with a 4-byte magic header followed by the id of the key
    // that encrypted it, so probe until that is the new key.
    private void WaitUntilEncryptingWith(Guid keyId)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(30);
        while (true)
        {
            byte[] probe = _protector.Protect([0]);
            if (new Guid(probe.AsSpan(4, 16)) == keyId)
                return;

            if (DateTime.UtcNow > deadline)
                throw new InvalidOperationException($"Data Protection did not start using the new key {keyId}: the secrets were not re-encrypted.");

            Thread.Sleep(100);
        }
    }

    private static void Validate(string name, VariableSaveRequest request)
    {
        if (name.Length == 0 || name.Length > MaxNameLength || !NameRegex().IsMatch(name))
            throw new VariableValidationException($"The name must start with a letter or '_' and contain only letters, digits, '_', '.' and '-' (at most {MaxNameLength} characters).");

        if (request.Value != null && request.Value.Length > MaxValueLength)
            throw new VariableValidationException($"The value can be at most {MaxValueLength} characters long.");

        if (request.Description != null && request.Description.Length > MaxDescriptionLength)
            throw new VariableValidationException($"The description can be at most {MaxDescriptionLength} characters long.");
    }

    private static bool IsReadable(VariableSet snapshot, string name)
    {
        try
        {
            snapshot.Resolve(VariableKind.Secret, name);
            return true;
        }
        catch (ApplicationException)
        {
            return false;
        }
    }

    private string? TryUnprotect(Variable variable)
    {
        try
        {
            return _protector.Unprotect(variable.Value);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Secret '{Name}' cannot be decrypted: the Data Protection keys it was encrypted with are missing or unreadable.", variable.Name);
            return null;
        }
    }

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_.\-]*$")]
    private static partial Regex NameRegex();
}
