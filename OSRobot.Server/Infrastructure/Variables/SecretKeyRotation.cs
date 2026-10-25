// SPDX-FileCopyrightText: Gianluca Di Bucci (gianx1980) <https://www.os-robot.com>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Options;

namespace OSRobot.Server.Infrastructure.Variables;

/// <summary>
/// Data Protection never re-encrypts a key it already created: changing AppSettings:DataProtection
/// (adding a certificate on Windows, replacing a certificate) only applies to keys created afterwards,
/// and the active key keeps being used for up to 90 days. So at startup the active key is compared with
/// the configuration; when they differ, a new key is created (encrypted as configured) and every secret
/// is re-encrypted with it. From then on the secrets no longer need the old key, nor what protected it
/// (DPAPI on this machine, the previous certificate).
/// </summary>
public static class SecretKeyRotation
{
    /// <returns>True if the keys were rotated and the secrets re-encrypted.</returns>
    public static bool EnsureActiveKeyMatchesConfiguration(IServiceProvider services, DataProtectionSetupResult setup, ILogger logger)
    {
        IKeyManager keyManager = services.GetRequiredService<IKeyManager>();
        IXmlRepository repository = services.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository
                                    ?? throw new InvalidOperationException("Data Protection has no key repository configured.");

        DateTimeOffset now = DateTimeOffset.UtcNow;

        // The key new data is encrypted with: the most recently activated one still valid. None yet (a new
        // installation, or every key expired): the next one is created as configured, nothing to do.
        // A key Data Protection pre-generated for later, still with the old protection, is caught at the
        // first start after it becomes active.
        IKey? activeKey = keyManager.GetAllKeys()
                                    .Where(k => !k.IsRevoked && k.ActivationDate <= now && k.ExpirationDate > now)
                                    .OrderByDescending(k => k.ActivationDate)
                                    .FirstOrDefault();
        if (activeKey == null)
            return false;

        XElement? keyElement = repository.GetAllElements()
                                         .FirstOrDefault(e => e.Name.LocalName == "key" && (string?)e.Attribute("id") == activeKey.KeyId.ToString());
        if (keyElement == null)
            return false;

        string actual = DataProtectionSetup.DescribeKeyProtection(keyElement);
        if (string.Equals(actual, setup.KeyProtection, StringComparison.OrdinalIgnoreCase))
            return false;

        logger.LogWarning("The secrets' encryption key {KeyId} is protected by '{Actual}', but the configuration says '{Expected}': " +
                          "creating a new key and re-encrypting the secrets with it.", activeKey.KeyId, actual, setup.KeyProtection);

        // Data Protection's usual lifetime for a key; the new key is active at once.
        VariableStore store = services.GetRequiredService<VariableStore>();
        int count = store.ReencryptSecrets(() => keyManager.CreateNewKey(now, now.AddDays(90)).KeyId);

        logger.LogInformation("{Count} secret(s) re-encrypted with the new key.", count);
        return true;
    }
}
