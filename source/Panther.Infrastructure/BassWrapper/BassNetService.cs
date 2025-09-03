using Microsoft.Extensions.Logging;
using Panther.Infrastructure.Constants;
using Un4seen.Bass;

namespace Panther.Infrastructure.BassWrapper;

public class BassNetService(ILogger<BassNetService> logger) : IBassNetService
{
    public void Register()
    {
        try
        {
            logger.LogDebug("Registering {InternalName}...", BassNet.InternalName);
            BassNet.Registration(BassCredentials.Email, BassCredentials.Key);
            logger.LogDebug("Bass.Net registered successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to register Bass.Net with provided credentials.");
            throw;
        }
    }
}
