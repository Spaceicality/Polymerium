// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Polymerium.App.Models;
using Polymerium.App.ViewModels.AddAccountWizard;

namespace Polymerium.App.Views.AddAccountWizards;

public sealed partial class AccountSelectionView : Page
{
    private AddAccountWizardStateHandler? handler;

    public AccountSelectionView()
    {
        ViewModel = App.Current.Provider.GetRequiredService<AccountSelectionViewModel>();
        InitializeComponent();
    }

    public AccountSelectionViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        (handler, _, _) = ((AddAccountWizardStateHandler, CancellationToken, object?))e.Parameter;
        base.OnNavigatedTo(e);
    }

    private void FirstPage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is AccountWizardEntryModel first)
            handler?.Invoke((first.Page, null));
    }
}
