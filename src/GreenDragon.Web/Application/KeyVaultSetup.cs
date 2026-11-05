using Azure.Identity;
using GreenDragon.Web.Configuration;

namespace GreenDragon.Web.Application;

/// <summary>Adds optional managed-identity-backed Azure configuration.</summary>
public static class KeyVaultSetup
{
    /// <summary>Loads vault secrets last, only if a vault URI has been configured.</summary>
    /// <param name="builder">The web host being composed.</param>
    public static void AddKeyVault(this WebApplicationBuilder builder)
    {
        var vaultUri = builder.Configuration.GetConnectionString(HostingSettings.KeyVaultConnectionName);
        if (!string.IsNullOrWhiteSpace(vaultUri))
        {
            builder.Configuration.AddAzureKeyVault(new Uri(vaultUri), new DefaultAzureCredential());
        }
    }
}
