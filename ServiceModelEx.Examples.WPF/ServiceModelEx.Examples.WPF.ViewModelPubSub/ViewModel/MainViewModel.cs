using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using ServiceModelEx.Examples.PubSub.Contracts.ServiceContracts;
using ServiceModelEx.Examples.WPF.ViewModelPubSub.Model;
using ServiceModelEx.Examples.WPF.ViewModelPubSub.Publisher.Views;
using System;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Discovery;
using System.Windows.Input;

namespace ServiceModelEx.Examples.WPF.ViewModelPubSub.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class MainViewModel : ViewModelBase, IFooBarServiceContract, IDisposable
    {
        private PublisherView publisherInstance;
        private ServiceHost<MainViewModel> serviceHost;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            if (IsInDesignMode)
            {
                // Code runs in Blend --> create design time data.
            }
            else
            {
                Messenger.Default.Register<NotificationMessage>(this, (m) =>
                {
                    if (m.Notification == "OpenPublisher")
                    {
                        if (publisherInstance != null && publisherInstance.IsVisible) return;

                        Messenger.Default.Send(new NotificationMessage("Publisher"));
                        publisherInstance = publisherInstance ?? new PublisherView();
                        publisherInstance.Closed += (s, e) => publisherInstance = null;
                        publisherInstance.Show();
                    }
                });

                serviceHost = new ServiceHost<MainViewModel>(this, new Uri($"net.tcp://localhost/{ typeof(MainViewModel).Name }"));
                serviceHost.Open();

                Disposed = false;
            }
        }

        protected bool Disposed { get; set; }

        public ObservableCollection<SubscriptionResult> SubscriptionFeed { get; set; } = new ObservableCollection<SubscriptionResult>();

        public ICommand ShowPublisherWindow => new RelayCommand(OnShowPublisherWindow);//() => Messenger.Default.Send(new NotificationMessage("OpenPublisher")));

        private void OnShowPublisherWindow()
        {
            Messenger.Default.Send(new NotificationMessage("OpenPublisher"));
        }

        public void Foo(string payload)
        {
            SubscriptionFeed.Add(new SubscriptionResult
            {
                Id = SubscriptionFeed.Count,
                Payload = payload,
                Timestamp = DateTime.Now.ToString()
            });
        }

        public static void Configure(ServiceConfiguration config)
        {
            var binding = new NetTcpBinding(SecurityMode.Transport, true);
            config.EnableProtocol(binding);
            config.AddServiceEndpoint(new UdpDiscoveryEndpoint());
            ServiceDiscoveryBehavior serviceDiscoveryBehavior = new ServiceDiscoveryBehavior();
            serviceDiscoveryBehavior.AnnouncementEndpoints.Add(new UdpAnnouncementEndpoint());
            config.Description.Behaviors.Add(serviceDiscoveryBehavior);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {                serviceHost.Close();
            }

            Disposed = true;
        }
    }
}