using Microsoft.Extensions.Logging;
using Un4seen.Bass;

namespace Panther.Infrastructure.BassWrapper;

public class BassNetService(ILogger<BassNetService> logger) : IBassNetService
{
    public void Register(string email, string key)
    {
        try
        {
            logger.LogDebug("Registering {InternalName}...", BassNet.InternalName);
            BassNet.Registration(email, key);
            logger.LogDebug("Bass.Net registered successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to register Bass.Net with provided credentials.");
            throw;
        }
    }
}
