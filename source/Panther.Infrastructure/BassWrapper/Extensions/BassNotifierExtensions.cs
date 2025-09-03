using Panther.Infrastructure.BassWrapper.Models;
using System.Runtime.CompilerServices;
using Un4seen.Bass;

namespace Panther.Infrastructure.BassWrapper.Extensions;

public static class BassNotifierExtensions
{
    public static void GetErrorAndRaise(
        this IBassNotifier? sender,
        string description,
        EventHandler<BassOperationError>? onError,
        [CallerMemberName] string? operation = null)
    {
        var errorCode = Bass.BASS_ErrorGetCode();
        var error = new BassOperationError(errorCode, description, operation, sender);
        onError?.Invoke(sender, error);
    }
}
