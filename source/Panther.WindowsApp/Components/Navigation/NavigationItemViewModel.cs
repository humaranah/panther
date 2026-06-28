using Microsoft.Windows.ApplicationModel.Resources;
using System.Collections.ObjectModel;

namespace Panther.WindowsApp.Components.Navigation;

public class NavigationItemViewModel
{
    private readonly ResourceLoader _resourceLoader = new();

    public const string NavItemPrefix = "NavItem_";

    protected NavigationItemViewModel() { }

    public string Tag { get; set; } = string.Empty;
    public object? Icon { get; set; }
    public bool IsSeparator { get; set; }

    public ObservableCollection<NavigationItemViewModel> SubItems { get; } = [];

    public string LocalizedText => _resourceLoader.GetString($"{NavItemPrefix}{Tag}");

    public NavigationItemViewModel WithSubItems(params NavigationItemViewModel[] items)
    {
        foreach (var item in items)
        {
            SubItems.Add(item);
        }
        return this;
    }

    public static NavigationItemViewModel CreateItem(string tag, object? icon) =>
        new()
        {
            Tag = tag,
            Icon = icon
        };

    public static NavigationItemViewModel CreateSeparator() => new() { IsSeparator = true };
}
