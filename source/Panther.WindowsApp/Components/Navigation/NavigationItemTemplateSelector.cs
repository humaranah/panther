using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Panther.WindowsApp.Components.Navigation;

public class NavigationItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? ItemTemplate { get; set; }
    public DataTemplate? SeparatorTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        if (item is NavigationItemViewModel navItem)
        {
            return navItem.IsSeparator ? SeparatorTemplate : ItemTemplate;
        }
        return base.SelectTemplateCore(item);
    }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}
