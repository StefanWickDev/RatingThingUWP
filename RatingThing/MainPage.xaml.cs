using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.Azure.Devices.Client;
using Windows.Storage;
using Windows.System;
using Windows.ApplicationModel.Core;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RatingThing
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        DeviceClient iotHubClient = null;
        string connectionString = "HostName=swickFreeHub.azure-devices.net;DeviceId=RatingThingPi;SharedAccessKey=lucX+ap+24loV+nGGZi+++/xw9eFHiKHczK6y/513d8="; // optionally hard-code your device connection string here

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (iotHubClient == null)
                ConnectToAzure();
        }

        private void ConnectToAzure()
        {
            if (connectionString != "")
            {                
                iotHubClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt);
                iotHubClient.SetConnectionStatusChangesHandler(new ConnectionStatusChangesHandler(this.StatusChangesHandler));
                iotHubClient.OpenAsync();
            }
        }

        private async void StatusChangesHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (status == ConnectionStatus.Connected)
                {                    
                    ratingControl.IsEnabled = true;
                    btnSubmit.IsEnabled = true;
                }
                else
                {
                    ratingControl.IsEnabled = false;
                    btnSubmit.IsEnabled = false;
                }
            });
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (connectionString == "")
            {               
                if (ApplicationData.Current.LocalSettings.Values.ContainsKey("device-connection-string"))
                {
                    connectionString = ApplicationData.Current.LocalSettings.Values["device-connection-string"] as string;
                }
                else
                {
                    // if we haven't stored a connection string let's prompt the user
                    ConnectionStringDialog dlg = new ConnectionStringDialog();
                    ContentDialogResult result = await dlg.ShowAsync();
                    if (result.ToString() == "Primary")
                    {
                        connectionString = dlg.ConnectionString.Text;
                        ApplicationData.Current.LocalSettings.Values["device-connection-string"] = connectionString;
                        ConnectToAzure();
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }

        private async void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            ulong kiloBytes = MemoryManager.AppMemoryUsage / 1024;
            int rating = (int)ratingControl.Value;

            Message message = new Message();
            message.Properties.Add("rating", rating.ToString());
            message.Properties.Add("app-memory-usage", kiloBytes.ToString());

            await iotHubClient.SendEventAsync(message);            
            ratingControl.ClearValue(RatingControl.ValueProperty);
        }
    }
}
