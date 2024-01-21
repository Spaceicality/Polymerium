﻿using System;
using System.IO;
using System.Reflection;
using Windows.Graphics;
using Windows.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Polly;
using Polymerium.App.Extensions;
using Polymerium.App.Services;
using Polymerium.App.ViewModels;
using Polymerium.Trident.Extractors;
using Polymerium.Trident.Repositories;
using Polymerium.Trident.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App;

public partial class App
{
    public App()
    {
        InitializeComponent();

        var services = new ServiceCollection();
        ConfigureServices(services);
        Provider = services.BuildServiceProvider();
    }

    public new static App Current => (App)Application.Current;

    public IServiceProvider Provider { get; }

    public Window Window { get; private set; } = null!;

    public static T ViewModel<T>()
        where T : ViewModelBase
    {
        return Current.Provider.GetRequiredService<T>();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // App Services
        services
            .AddSerializationOptions(options => { options.WriteIndented = true; })
            .AddLogging(builder =>
            {
                builder
                    .AddDebug()
                    .AddConsole();
            })
            .AddMemoryCache()
            .AddHttpClient()
            .ConfigureHttpClientDefaults(clientBuilder => clientBuilder.ConfigureHttpClient(client =>
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Add("User-Agent",
                        $"Polymerium/{Assembly.GetExecutingAssembly().GetName().Version}");
                })
                .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.RetryAsync()));

        services
            .AddSingleton<NavigationService>()
            .AddSingleton<TaskService>()
            .AddSingleton<NotificationService>();

        // Trident Services
        services
            .AddSingleton(new PolymeriumContext(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".polymerium")));
        services
            .AddSingleton<ProfileManager>()
            .AddSingleton<RepositoryAgent>()
            .AddSingleton<DownloadManager>()
            .AddSingleton<ModpackExtractor>()
            .AddSingleton<StorageManager>();

        // ViewModels
        services
            .AddViewModel<HomeViewModel>()
            .AddViewModel<DesktopViewModel>()
            .AddViewModel<InstanceViewModel>()
            .AddViewModel<AccountViewModel>()
            .AddViewModel<MarketViewModel>()
            .AddViewModel<SettingViewModel>()
            .AddViewModel<ToolboxViewModel>()
            .AddViewModel<ModpackViewModel>()
            .AddViewModel<WorkbenchViewModel>();

        // Repositories
        services
            .AddRepository<CurseForgeRepository>()
            .AddRepository<ModrinthRepository>();

        // Extractors
        services
            .AddExtractor<CurseForgeExtractor>();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Spawn(Provider.GetRequiredService<NavigationService>(), Provider.GetRequiredService<NotificationService>());
    }

    private void Spawn(NavigationService navigation, NotificationService notification)
    {
        const string KEY_HEIGHT = "Window.Height";
        const string KEY_WIDTH = "Window.Width";
        var layout = new Layout();
        var window = new Window
        {
            Title = "Polymerium: Powered by Trident",
            ExtendsContentIntoTitleBar = true,
            Content = layout,
            SystemBackdrop = new MicaBackdrop()
        };
        var settings = ApplicationData.Current.LocalSettings;
        window.SetTitleBar(layout.Titlebar);
        window.Activated += (_, args) =>
            layout.OnActivate(args.WindowActivationState != WindowActivationState.Deactivated);
        window.Closed += (_, args) =>
        {
            var size = window.AppWindow.Size;
            settings.Values[KEY_HEIGHT] = size.Height;
            settings.Values[KEY_WIDTH] = size.Width;
            ((IDisposable)Provider).Dispose();
        };
        navigation.SetHandler(layout.OnNavigate);
        notification.SetHandler(layout.OnEnqueueNotification);
        layout.SetMainMenu(navigation.MainNavMenu);
        layout.SetSideMenu(navigation.SideNavMenu);
        layout.SetHandler((view, parameter, info) => navigation.Navigate(view, parameter, info, true));
        if (settings.Values.TryGetValue(KEY_HEIGHT, out var h) && h is int height
                                                               && settings.Values.TryGetValue(KEY_WIDTH, out var w) &&
                                                               w is int width)
            window.AppWindow.Resize(new SizeInt32(width, height));
        Window = window;
        window.Activate();
    }
}