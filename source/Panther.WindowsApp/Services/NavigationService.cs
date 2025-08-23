using Microsoft.UI.Xaml.Controls;
using System;

namespace Panther.WindowsApp.Services;

public class NavigationService() : INavigationService
{
    public Frame RootFrame { get; } = new Frame();

    public void NavigateTo(Type pageType, object? parameter = null)
    {
        if (parameter == null)
        {
            RootFrame.Navigate(pageType);
        }
        else
        {
            RootFrame.Navigate(pageType, parameter);
        }
    }
}
