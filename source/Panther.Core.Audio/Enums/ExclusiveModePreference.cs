namespace Panther.Core.Audio.Enums;

public enum ExclusiveModePreference
{
    Never,            // Never use exclusive mode, always use shared mode
    Always,           // Always prefer exclusive mode if available, and fail if exclusive mode is not available
    PreferExclusive,  // Prefer exclusive mode if available, but allow shared mode if exclusive is not available
}
