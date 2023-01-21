using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.Abstractions;
using Polymerium.App.Services;
using Polymerium.App.Views;

namespace Polymerium.App.ViewModels
{
    public partial class InstanceViewModel : ObservableObject
    {
        private readonly InstanceManager _instanceManager;
        private readonly IOverlayService _overlayService;
        public InstanceViewModel(InstanceManager instanceManager, IOverlayService overlayService)
        {
            _instanceManager = instanceManager;
            _overlayService = overlayService;
        }

        private GameInstance instance;
        public GameInstance Instance { get => instance; set => SetProperty(ref instance, value); }

        public void GotInstance(GameInstance instance)
        {
            Instance = instance;
        }

        [RelayCommand]
        public void Start()
        {
            var dialog = new PrepareGameDialog(Instance, _overlayService);
            _overlayService.Show(dialog);
        }
    }
}