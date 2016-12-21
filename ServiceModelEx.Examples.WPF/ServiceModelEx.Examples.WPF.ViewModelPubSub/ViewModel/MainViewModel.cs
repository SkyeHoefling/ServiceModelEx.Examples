using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using ServiceModelEx.Examples.WPF.ViewModelPubSub.Publisher.Views;
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
    public class MainViewModel : ViewModelBase
    {
        private PublisherView publisherInstance;

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
            }
        }

        public ICommand ShowPublisherWindow => new RelayCommand(OnShowPublisherWindow);//() => Messenger.Default.Send(new NotificationMessage("OpenPublisher")));

        private void OnShowPublisherWindow()
        {
            Messenger.Default.Send(new NotificationMessage("OpenPublisher"));
        }
    }
}