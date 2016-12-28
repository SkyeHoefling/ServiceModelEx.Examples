using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ServiceModelEx;
using ServiceModelEx.Examples.PubSub.Contracts.ServiceContracts;
using ServiceModelEx.Examples.PubSub.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ServiceModelEx.Examples.WPF.ViewModelPubSub.Publisher.ViewModels
{
    public class PublisherViewModel : ViewModelBase
    {
        private bool isServiceRunning;
        private string console;

        public PublisherViewModel()
        {
            IsServiceRunning = false;
            console = string.Empty;
        }

        public bool IsServiceRunning
        {
            get
            {
                return isServiceRunning;
            }
            set
            {
                if (isServiceRunning == value) return;
                isServiceRunning = value;
                RaisePropertyChanged(nameof(IsServiceRunning));
            }
        }

        public string Console
        {
            get
            {
                return console;
            }
            set
            {
                if (console == value) return;
                console = value + Environment.NewLine;
                RaisePropertyChanged(nameof(Console));
            }
        }

        public ICommand ToggleService => new RelayCommand(OnToggleService);
        private ServiceHost host;
        private void OnToggleService()
        {
            if(host != null)
            {
                host.Close();
                host = null;
            }

            host = DiscoveryPublishService<IFooBarServiceContract>.CreateHost<FooBarService>();
            host.Open();

            IsServiceRunning = !IsServiceRunning;
        }

        public ICommand PublishMessage => new RelayCommand<string>(OnPublishMessage);

        private void OnPublishMessage(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) return;

            Console += $"{DateTime.Now.ToString()} : payload";
        }
    }
}
