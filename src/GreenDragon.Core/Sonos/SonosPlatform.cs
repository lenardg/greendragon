using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using OpenPhonos.UPnP;

namespace GreenDragon.Core.Sonos;

/// <summary>Provides server behavior without a UI dispatcher.</summary>
/// <param name="logger">Observes upstream callback failures.</param>
internal sealed class SonosPlatform(ILogger logger) : IPlatform
{
    /// <summary>Identifies the portal to the upstream listener.</summary>
    public const string ApplicationName = "GreenDragon";
    /// <summary>Identifies portal discovery requests to devices.</summary>
    public const string UserAgent = "GreenDragon/1.0 UPnP/1.1";
    /// <inheritdoc />
    public bool IsAlwaysWiFi()
    {
        return false;
    }

    /// <inheritdoc />
    public string DeviceName()
    {
        return Environment.MachineName;
    }

    /// <inheritdoc />
    public string Name()
    {
        return ApplicationName;
    }

    /// <inheritdoc />
    public string FullPlatformName()
    {
        return RuntimeInformation.OSDescription;
    }

    /// <inheritdoc />
    public string FindNetworkName(NetworkInterface network)
    {
        return network.Description;
    }

    /// <inheritdoc />
    public bool IsUsableNetwork(NetworkInterface network, out string error)
    {
        error = string.Empty;
        return network.OperationalStatus == OperationalStatus.Up
            && network.NetworkInterfaceType != NetworkInterfaceType.Loopback
            && network.SupportsMulticast;
    }
    /// <inheritdoc />
    public void OnUIThread(Func<Task> uiAction)
    {
        _ = ObserveCallbackAsync(uiAction);
    }
    private async Task ObserveCallbackAsync(Func<Task> callback)
    {
        try
        {
            await callback();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "OpenPhonos callback failed");
        }
    }
}
