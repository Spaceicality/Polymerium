// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.Controls;
using Polymerium.App.Models;
using Polymerium.App.ViewModels;

namespace Polymerium.App.Views;

public sealed partial class CreateInstanceWizardDialog : CustomDialog
{
    // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty VersionsProperty =
        DependencyProperty.Register(nameof(Versions), typeof(IEnumerable<GameVersionModel>),
            typeof(CreateInstanceWizardDialog), new PropertyMetadata(null));

    public static readonly DependencyProperty IsOperableProperty =
        DependencyProperty.Register(nameof(IsOperable), typeof(bool), typeof(CreateInstanceWizardDialog),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ErrorMessageProperty =
        DependencyProperty.Register(nameof(ErrorMessage), typeof(string), typeof(CreateInstanceWizardDialog),
            new PropertyMetadata(string.Empty));

    private readonly DispatcherQueue _dispatcher;

    private ContentControl _root;

    public CreateInstanceWizardDialog()
    {
        InitializeComponent();
        ViewModel = App.Current.Provider.GetRequiredService<CreateInstanceWizardViewModel>();
        _dispatcher = DispatcherQueue.GetForCurrentThread();
    }

    public IEnumerable<GameVersionModel> Versions
    {
        get => (IEnumerable<GameVersionModel>)GetValue(VersionsProperty);
        set => SetValue(VersionsProperty, value);
    }

    public bool IsOperable
    {
        get => (bool)GetValue(IsOperableProperty);
        set => SetValue(IsOperableProperty, value);
    }

    public string ErrorMessage
    {
        get => (string)GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    public CreateInstanceWizardViewModel ViewModel { get; }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _root = FindName("Root") as ContentControl;
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Dismiss();
    }

    private void CreateInstanceWizardDialog_OnLoaded(object sender, RoutedEventArgs e)
    {
        VisualStateManager.GoToState(_root, "Loading", false);
        IsOperable = false;
        Task.Run(() => ViewModel.FillDataAsync(ViewModel_FillDataCompletedAsync), CancellationToken.None);
    }

    private Task ViewModel_FillDataCompletedAsync(IEnumerable<GameVersionModel> data)
    {
        _dispatcher.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        {
            IsOperable = true;
            Versions = data;
            CoreVersion.SelectedIndex = 0;
            VisualStateManager.GoToState(_root, "Default", false);
        });
        return Task.CompletedTask;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var errors = ViewModel.GetErrors();
        if (errors != null && errors.Any())
        {
            ErrorMessage = string.Join('\n', errors.Select(x => x.ErrorMessage));
        }
        else
        {
            ErrorMessage = string.Empty;
            VisualStateManager.GoToState(_root, "Working", false);
            IsOperable = false;
            Task.Run(() => ViewModel.Commit(ViewModel_CommitCompletedAsync), CancellationToken.None);
        }
    }

    private Task ViewModel_CommitCompletedAsync()
    {
        _dispatcher.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        {
            IsOperable = true;
            VisualStateManager.GoToState(_root, "Default", false);
            Dismiss();
        });
        return Task.CompletedTask;
    }

    private void CoreVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(ViewModel.InstanceName) ||
            ViewModel.InstanceName == ((GameVersionModel)e.RemovedItems.FirstOrDefault())?.Id)
            ViewModel.InstanceName = ((GameVersionModel)e.AddedItems.FirstOrDefault())?.Id;
    }
}