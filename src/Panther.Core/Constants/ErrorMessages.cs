namespace Panther.Core.Constants
{
    public static class ErrorMessages
    {
        public const string CouldNotInitBass = "Could not init BASS!";
        public const string QueueNotInitialized = "Queue not initialized!";
        public const string ValueNotNullOrEmpty = "Value cannot be null or empty.";

        public static string CouldNotFind(string fileName) => $"Could not find \"{Path.GetFullPath(fileName)}\"!";
    }
}
