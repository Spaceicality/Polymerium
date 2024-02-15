﻿using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Instances;
using Trident.Abstractions;
using Trident.Abstractions.Resources;

namespace Polymerium.App.ViewModels;

public class InstanceViewModel : ViewModelBase
{
    private readonly TridentContext _context;
    private readonly InstanceManager _instanceManager;
    private readonly InstanceStatusService _instanceStatusService;
    private readonly NavigationService _navigation;
    private readonly ProfileManager _profileManager;
    private readonly TaskService _taskService;
    private readonly ThumbnailSaver _thumbnailSaver;

    private ProfileModel model = ProfileModel.DUMMY;

    public InstanceViewModel(ProfileManager profileManager, NavigationService navigation, TridentContext context,
        TaskService taskService, ThumbnailSaver thumbnailSaver, InstanceManager instanceManager,
        InstanceStatusService instanceStatusService)
    {
        _profileManager = profileManager;
        _navigation = navigation;
        _context = context;
        _taskService = taskService;
        _thumbnailSaver = thumbnailSaver;
        _instanceManager = instanceManager;
        _instanceStatusService = instanceStatusService;

        GotoMetadataViewCommand = new RelayCommand<string>(GotoMetadataView);
        GotoConfigurationViewCommand = new RelayCommand<string>(GotoConfigurationView);
        OpenHomeFolderCommand = new RelayCommand(OpenHomeFolder, CanOpenHomeFolder);
        OpenAssetFolderCommand = new RelayCommand<AssetKind>(OpenAssetFolder, CanOpenAssetFolder);
        DeleteTodoCommand = new RelayCommand<TodoModel>(DeleteTodo, CanDeleteTodo);
        StopCommand = new RelayCommand(Stop);
        PlayCommand = new RelayCommand(Play);
    }

    public ProfileModel Model
    {
        get => model;
        set => SetProperty(ref model, value);
    }

    public ICommand GotoMetadataViewCommand { get; }
    public ICommand GotoConfigurationViewCommand { get; }
    public ICommand OpenAssetFolderCommand { get; }
    public ICommand OpenHomeFolderCommand { get; }
    public ICommand DeleteTodoCommand { get; }
    public ICommand PlayCommand { get; }
    public ICommand StopCommand { get; }

    public override bool OnAttached(object? maybeKey)
    {
        if (maybeKey is string key)
        {
            var profile = _profileManager.GetProfile(key);
            if (profile != null)
                Model = new ProfileModel(key, profile, _thumbnailSaver.Get(key), _instanceStatusService.MustHave(key));
            return profile != null;
        }

        return false;
    }

    public override void OnDetached()
    {
        if (Model.Key != ProfileManager.DUMMY_KEY) _profileManager.Flush(Model.Key);
    }

    private void GotoMetadataView(string? key)
    {
        if (key != null && key != ProfileManager.DUMMY_KEY)
            _navigation.Navigate(typeof(MetadataView), key, new SlideNavigationTransitionInfo
            {
                Effect = SlideNavigationTransitionEffect.FromRight
            });
    }

    private void GotoConfigurationView(string? key)
    {
        if (key != null && key != ProfileManager.DUMMY_KEY)
            _navigation.Navigate(typeof(ConfigurationView), key, new SlideNavigationTransitionInfo
            {
                Effect = SlideNavigationTransitionEffect.FromRight
            });
    }

    private string GetHomeFolderPath()
    {
        return Path.Combine(_context.InstanceDir, Model.Key);
    }

    private string GetAssetFolderPath(AssetKind kind)
    {
        return Path.Combine(GetHomeFolderPath(), kind switch
        {
            AssetKind.Mod => "mods",
            AssetKind.Save => "saves",
            AssetKind.Screenshot => "screenshots",
            AssetKind.ShaderPack => "shaders",
            AssetKind.ResourcePack => "resourcepacks",
            AssetKind.DataPack => "datapacks",
            _ => throw new NotImplementedException()
        });
    }

    private bool CanOpenHomeFolder()
    {
        return Directory.Exists(GetHomeFolderPath());
    }

    private void OpenHomeFolder()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = GetHomeFolderPath(),
            UseShellExecute = true
        });
    }

    private bool CanOpenAssetFolder(AssetKind kind)
    {
        var path = GetAssetFolderPath(kind);
        return Directory.Exists(path);
    }

    private void OpenAssetFolder(AssetKind kind)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = GetAssetFolderPath(kind),
            UseShellExecute = true
        });
    }

    public void AddTodo(string text)
    {
        Model.Todos.Add(new TodoModel(new Profile.RecordData.Todo(false, text)));
    }

    private bool CanDeleteTodo(TodoModel? item)
    {
        return item != null;
    }

    private void DeleteTodo(TodoModel? item)
    {
        if (item != null) Model.Todos.Remove(item);
    }

    private void Play()
    {
        // var task = _taskService.Create<DeployInstanceTask>(Model.Key, Model.Inner);
        // _taskService.Enqueue(task);
        _instanceManager.Deploy(Model.Key, Model.Inner.Metadata, null, App.Current.Token);
    }

    private void Stop()
    {
        if (_instanceManager.IsTracking(Model.Key, out var tracker))
            switch (tracker)
            {
                case DeployTracker deployer:
                    deployer.Abort();
                    break;
                case LaunchTracker launcher:
                    break;
            }
    }
}