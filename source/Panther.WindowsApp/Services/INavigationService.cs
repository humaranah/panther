using Microsoft.UI.Xaml.Controls;
using System;

namespace Panther.WindowsApp.Services;

public interface INavigationService
{
    Frame RootFrame { get; }

    void NavigateTo(Type pageType, object? parameter = null);
}
