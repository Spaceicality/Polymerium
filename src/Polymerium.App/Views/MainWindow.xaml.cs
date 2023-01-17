// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.App.Messages;
using Polymerium.App.Models;
using Polymerium.App.ViewModels;
using WinUIEx;

namespace Polymerium.App.Views;

public sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();
        ViewModel = App.Current.Provider.GetRequiredService<MainViewModel>();

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            SetTitleBar(TitleBarDragArea);
            ExtendsContentIntoTitleBar = true;
        }
        else
        {
            (ColumnRight.Width, ColumnLeft.Width) = (ColumnLeft.Width, ColumnRight.Width);
        }

        if (Environment.OSVersion.Version.Major >= 10)
        {
            if (Environment.OSVersion.Version.Build >= 22000)
                Backdrop = new AcrylicSystemBackdrop();
            else
                Backdrop = new AcrylicSystemBackdrop();
        }
        else
        {
            FakeBackground.Visibility = Visibility.Visible;
        }

        foreach (var a in ViewModel.LogonAccounts)
        {
            var item = new MenuFlyoutItem() { Text = a.Inner.DisplayName, Tag = a };
            item.Click += (sender, _) => ViewModel.SwitchAccountTo((AccountItemModel)((MenuFlyoutItem)sender).Tag);
            SwitchToSubMenu.Items.Add(item);
        }
        ViewModel.LogonAccounts.CollectionChanged += LogonAccounts_CollectionChanged;
    }

    private void LogonAccounts_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:

                foreach (AccountItemModel a in e.NewItems)
                {
                    var item = new MenuFlyoutItem() { Text = a.Inner.DisplayName, Tag = a };
                    item.Click += (sender, _) => ViewModel.SwitchAccountTo((AccountItemModel)((MenuFlyoutItem)sender).Tag);
                    SwitchToSubMenu.Items.Add(item);
                }

                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (AccountItemModel a in e.OldItems)
                {
                    var item = SwitchToSubMenu.Items.FirstOrDefault(x => ((AccountItemModel)x.Tag).Inner.Id == a.Inner.Id);
                    if (item != null)
                    {
                        SwitchToSubMenu.Items.Remove(item);
                    }
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                SwitchToSubMenu.Items.Clear();
                break;
        }
    }

    public MainViewModel ViewModel { get; }

    private void OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var item = sender.SelectedItem as NavigationItemModel;
        RootFrame.Navigate(item.SourcePage, (item.GameInstance, ViewModel.LogonAccount?.Inner), new SuppressNavigationTransitionInfo());
        ViewModel.OnNavigatedTo(item);
    }

    private void Main_Closed(object sender, WindowEventArgs args)
    {
        StrongReferenceMessenger.Default.Send(new ApplicationAliveChangedMessage(false));
    }
}
