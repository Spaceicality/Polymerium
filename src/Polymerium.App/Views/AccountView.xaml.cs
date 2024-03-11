using Microsoft.UI.Xaml.Controls;
using Polymerium.App.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Views
{
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AccountView : Page
    {
        public AccountView()
        {
            InitializeComponent();
        }

        public AccountViewModel ViewModel { get; } = App.ViewModel<AccountViewModel>();
    }
}