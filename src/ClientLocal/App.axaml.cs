using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ClientLocal.Views;
using ClientLocal.Views.Decorator;

namespace ClientLocal;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var args = desktop.Args ?? Array.Empty<string>();
            desktop.MainWindow = args.Contains("--decorator-demo")
                ? new DecoratorHostWindow()
                : new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
