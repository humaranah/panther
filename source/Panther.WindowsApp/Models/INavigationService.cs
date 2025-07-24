using Microsoft.UI.Xaml.Controls;
using System;

namespace Panther.WindowsApp.Models;

public interface INavigationService
{
    Frame RootFrame { get; }

    void NavigateTo(Type pageType, object? parameters = null);
}
