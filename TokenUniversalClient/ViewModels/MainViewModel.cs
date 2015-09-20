using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TokenUniversalClient.Services;

namespace TokenUniversalClient.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        const string tenant = "iotdevices.onmicrosoft.com";
        const string clientId = "1825ade3-f5a9-482f-a9be-5ac77a4a41d8";
        const string aadInstance = "https://login.microsoftonline.com/{0}";
        static string authority = String.Format(CultureInfo.InvariantCulture, aadInstance, tenant);

        private static string sasTokenResourceId = "https://iotdevices.onmicrosoft.com/sastokenapi";
        private static string sasTokenBaseAddress = "https://mtcsastokenapi.azurewebsites.net/api/v1/";
        //private static string sasTokenBaseAddress = "https://localhost:44300/api/v1/";

        private HttpClient httpClient = new HttpClient();
        private static AuthenticationContext authContext;
        private static AuthenticationResult authResult;
        private Uri redirectURI;

        private IDialogService _dialogService;

        public ObservableCollection<string> ServiceNamespaces { get; set; }
        public ObservableCollection<string> EventHubs { get; set; }
        public ObservableCollection<string> KeyNames { get; set; }


        string _currentServiceNamespace;
        public string CurrentServiceNamespace
        {
            get { return _currentServiceNamespace; }
            set
            {
                if (_currentServiceNamespace == value)
                    return;
                _currentServiceNamespace = value;
                RaisePropertyChanged("CurrentServiceNamespace");
                RaisePropertyChanged("IsNewServiceNamespace");
            }
        }

        string _currentEventHub;
        public string CurrentEventHub
        {
            get { return _currentEventHub; }
            set
            {
                if (_currentEventHub == value)
                    return;
                _currentEventHub = value;
                RaisePropertyChanged("CurrentEventHub");
                RaisePropertyChanged("IsNewEventHub");
            }
        }


        string _currentKeyName;
        public string CurrentKeyName
        {
            get { return _currentKeyName; }
            set
            {
                if (_currentKeyName == value)
                    return;
                _currentKeyName = value;
                RaisePropertyChanged("CurrentKeyName");
                RaisePropertyChanged("IsNewKeyName");
            }
        }



        string _currentKeyValue;
        public string CurrentKeyValue
        {
            get { return _currentKeyValue; }
            set
            {
                if (_currentKeyValue == value)
                    return;
                _currentKeyValue = value;
                RaisePropertyChanged("CurrentKeyValue");
            }
        }


        bool _isReading;
        public bool IsReading
        {
            get { return _isReading; }
            set
            {
                if (_isReading == value)
                    return;
                _isReading = value;
                RaisePropertyChanged("IsReading");
            }
        }


        public bool IsNewServiceNamespace
        {
            get { return !ServiceNamespaces.Contains(CurrentServiceNamespace); }
          
        }

        public bool IsNewEventHub
        {
            get { return !EventHubs.Contains(CurrentEventHub); }

        }

        public bool IsNewKeyName
        {
            get { return !KeyNames.Contains(CurrentKeyName); }

        }

        public RelayCommand<object> FetchEventHubs { get; set; }
        public RelayCommand<object> FetchKeyNames { get; set; }
        public RelayCommand<object> SelectKeyName { get; set; }

        public RelayCommand SaveKey { get; set; }

        public RelayCommand Refresh { get; set; }

        public RelayCommand Clear { get; set; }

        public MainViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;

            this.ServiceNamespaces = new ObservableCollection<string>();
            this.EventHubs = new ObservableCollection<string>();
            this.KeyNames = new ObservableCollection<string>();

            redirectURI = Windows.Security.Authentication.Web.WebAuthenticationBroker.GetCurrentApplicationCallbackUri();
            authContext = new AuthenticationContext(authority);
            SetupCommands();
        }


        public async void Initialize()
        {
            authResult = await authContext.AcquireTokenAsync(sasTokenResourceId, clientId, redirectURI);

            if (authResult.Status != AuthenticationStatus.Success)
            {
                if (authResult.Error == "authentication_canceled")
                {
                }
                else
                {
                    var msg = string.Format("If the error continues, please contact your administrator.\n\nError: {0}\n\nError Description:\n\n{1}", authResult.Error, authResult.ErrorDescription);
                    await _dialogService.ShowErrorAsync(msg, "Authentication Error");

                }
                return;
            }

            await GetServiceNamespacesAsync();
        }

     

        private async Task PostAPICall<T>(T data)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var json = JsonConvert.SerializeObject(data);

            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(sasTokenBaseAddress, httpContent);
            if (response.IsSuccessStatusCode)
                await GetServiceNamespacesAsync();
            else
                await _dialogService.ShowErrorAsync("Unable to update SAS Service", "Update Error");
        }
        private async Task<T> GetAPICall<T>(string command)
        {
            IsReading = true;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);

            //    var action = string.Format("/api/sastoken/{0}/{1}/{2}?publisherId={3}", serviceNamespace.Text, eventHub.Text, keyName.Text, publisherId.Text);
            HttpResponseMessage response = await httpClient.GetAsync(sasTokenBaseAddress + command);

            if (response.IsSuccessStatusCode)
            {
                // Read the response as a Json Array and databind to the GridView to display todo items
                var json = await response.Content.ReadAsStringAsync();
                IsReading = false;
                return  JsonConvert.DeserializeObject<T>(json);
               
            }
            else
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // If the To Do list service returns access denied, clear the token cache and have the user sign-in again.
                    await _dialogService.ShowErrorAsync("Sorry, you don't have access to the SAS Service.  Please sign-in again.", "Not Authorized");
                    authContext.TokenCache.Clear();
                }
                else
                {
                    await _dialogService.ShowErrorAsync("Sorry, an error occurred accessing the SAS Service.  Please try again.", "Unexpected Error");
                }
                IsReading = false;
                return default(T);
            }
            
        }

        private async Task GetServiceNamespacesAsync()
        {
            ClearLists();

            var serviceNamespaces = await GetAPICall<IEnumerable<string>>("sasKeys/servicenamespaces");
            if (serviceNamespaces == null)
                return;

            foreach (var s in serviceNamespaces)
                this.ServiceNamespaces.Add(s);
            CurrentServiceNamespace = string.Empty;


        }

        private void ClearLists()
        {
            this.ServiceNamespaces.Clear();
            this.EventHubs.Clear();
            this.KeyNames.Clear();
        }

        private async Task GetEventHubsAsync(string serviceNamespace)
        {
            this.EventHubs.Clear();
            var hubs = await GetAPICall<IEnumerable<string>>("saskeys/" + serviceNamespace + "/eventhubs");

            if (hubs == null)
                return;

            foreach (var h in hubs)
            {
                EventHubs.Add(h);
            }

            CurrentServiceNamespace = serviceNamespace;
            CurrentEventHub = string.Empty;
        }

        private async Task GetKeyNames(string eventhub)
        {
            this.KeyNames.Clear();
            var keys = await GetAPICall<IEnumerable<string>>("saskeys/" + CurrentServiceNamespace + "/" + eventhub + "/" + "keynames");
            if (keys == null)
                return;

            foreach (var k in keys)
            {
                this.KeyNames.Add(k);
            }
            CurrentEventHub = eventhub;
        }

    

        private void SetupCommands()
        {
            this.FetchEventHubs = new RelayCommand<object>(async (servicenamespace) => {
                if (servicenamespace  is string)
                    await GetEventHubsAsync(servicenamespace as string);
            });
            this.FetchKeyNames = new RelayCommand<object>(async (eventhub) => {
                if (eventhub is string)
                    await GetKeyNames(eventhub as string);
            });
            this.SelectKeyName = new RelayCommand<object>((keyname) =>
            {
                if (keyname is string)
                    this.CurrentKeyName = keyname as string;

            });
            this.SaveKey = new RelayCommand(async() =>
            {
                var keyRegistration = new {
                    ServiceNamespace = this.CurrentServiceNamespace,
                    EventHub = this.CurrentEventHub,
                    KeyName = this.CurrentKeyName,
                    KeyValue = this.CurrentKeyValue
                };

                await PostAPICall<object>(keyRegistration);
            },
            () =>{


                return true;
            });
            this.Refresh = new RelayCommand(async() =>
            {
                await GetServiceNamespacesAsync();
            });
            this.Clear = new RelayCommand(() =>
            {
                this.CurrentServiceNamespace = string.Empty;
                this.CurrentEventHub= string.Empty;
                this.CurrentKeyName = string.Empty;
                this.CurrentKeyValue = string.Empty;

            });
        }
    }
}

