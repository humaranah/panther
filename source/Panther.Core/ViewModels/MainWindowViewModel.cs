namespace Panther.Core.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private const string AppName = "Panther Music Player";

    public string Greeting { get; } = AppName;
    public string WindowTitle { get; } = OperatingSystem.IsWindows() ? "" : AppName;
    public string ChromeTitle { get; } = OperatingSystem.IsWindows() ? AppName : "";
}
