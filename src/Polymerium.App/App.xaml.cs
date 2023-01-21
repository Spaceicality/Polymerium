// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Polymerium.Abstractions.DownloadSources;
using Polymerium.App.Services;
using Polymerium.App.ViewModels;
using Polymerium.App.Views;
using Polymerium.Core;
using Polymerium.Core.DownloadSources;
using Polymerium.Core.Engines;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        Provider = ConfigureServices();
    }

    public new static App Current => (App)Application.Current;

    public MainWindow Window { get; private set; }

    public IServiceProvider Provider { get; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Window = new MainWindow();
        Window.Closed += Window_Closed;
        Window.Activate();
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        ((IDisposable)Provider).Dispose();
    }

    private IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        // application services registration
        services.AddLogging(logging => logging.AddSimpleConsole().AddDebug()
#if DEBUG
        .SetMinimumLevel(LogLevel.Debug)
#endif
        );
        services.AddMemoryCache();
        // view models registration
        services.AddTransient<MainViewModel>();
        services.AddTransient<NewInstanceViewModel>();
        services.AddTransient<CreateInstanceWizardViewModel>();
        services.AddTransient<InstanceViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<SettingViewModel>();
        services.AddTransient<PrepareGameViewModel>();
        // local service registration
        services.AddSingleton<IOverlayService, WindowOverlayService>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<AccountManager>();
        services.AddSingleton<InstanceManager>();
        services.AddSingleton<DataStorageService>();
        // global services
        services.AddSingleton<GameManager>();
        services.AddSingleton<IFileBaseService, MainFileBaseService>().Configure<MainFileBaseOptions>(configure => configure.BaseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".polymerium/"));
        // engines
        services.AddScoped<DownloadEngine>();
        services.AddScoped<ResolveEngine>();
        services.AddScoped<RestoreEngine>();
        // download source provider registration
        services.AddTransient<DownloadSourceProviderBase, BMCLApiProvider>();
        services.AddTransient<DownloadSourceProviderBase, FallbackProvider>();
        return services.BuildServiceProvider();
    }
}
