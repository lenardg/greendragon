namespace GreenDragon.Web.Configuration;

/// <summary>Names host configuration sources shared by setup components.</summary>
public static class HostingSettings
{
    /// <summary>The optional untracked local settings file.</summary>
    public const string LocalSettingsFile = "appsettings.local.json";
    /// <summary>The optional Azure Key Vault connection string name.</summary>
    public const string KeyVaultConnectionName = "GreenDragon-keyvault";
    /// <summary>The user secrets identifier for this host.</summary>
    public const string UserSecretsId = "GreenDragon-web";
}
