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
        //private static string sasTokenBaseAddress = "https://mtcsastokenapi.azurewebsites.net/api/sastoken/";
        private static string sasTokenBaseAddress = "https://localhost:44300/api/sastoken/";

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

        public RelayCommand<string> FetchEventHubs { get; set; }
        public RelayCommand<string> FetchKeyNames { get; set; }
        public RelayCommand<string> SelectKeyName { get; set; }

        public RelayCommand SaveKey { get; set; }

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

        private async Task GetServiceNamespacesAsync()
        {
            this.ServiceNamespaces.Clear();

            var serviceNamespaces = await GetAPICall<IEnumerable<string>>("servicenamespaces");
            foreach (var s in serviceNamespaces)
                this.ServiceNamespaces.Add(s);
            CurrentServiceNamespace = string.Empty;

            if (this.ServiceNamespaces.Count == 1)
            {
                await GetEventHubsAsync(this.ServiceNamespaces[0]);
                CurrentServiceNamespace = this.ServiceNamespaces[0];
            }

        }

        private async Task PostAPICall(string content)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
            var httpContent = new StringContent(content);

            var response = await httpClient.PostAsync("https://localhost:44300/api/sastoken?serviceName=test&eventHub=test&keyName=test&keyValue=test",null);
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
                    await _dialogService.ShowErrorAsync("Sorry, you don't have access to the To Do Service.  Please sign-in again.", "Not Authorized");
                    authContext.TokenCache.Clear();
                }
                else
                {
                    await _dialogService.ShowErrorAsync("Sorry, an error occurred accessing your To Do list.  Please try again.", "Unexpected Error");
                }
                IsReading = false;
                return default(T);
            }
            
        }

        private async Task GetEventHubsAsync(string serviceNamespace)
        {
            this.EventHubs.Clear();
            var hubs = await GetAPICall<IEnumerable<string>>(serviceNamespace + "/eventhubs");
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
            var keys = await GetAPICall<IEnumerable<string>>(CurrentServiceNamespace + "/" + eventhub + "/" + "keynames");
            foreach (var k in keys)
            {
                this.KeyNames.Add(k);
            }
            CurrentEventHub = eventhub;
        }

    

        private void SetupCommands()
        {
            this.FetchEventHubs = new RelayCommand<string>(async (servicenamespace) => {
                await GetEventHubsAsync(servicenamespace);
            });
            this.FetchKeyNames = new RelayCommand<string>(async (eventhub) => {
                await GetKeyNames(eventhub);
            });
            this.SelectKeyName = new RelayCommand<string>((keyname) =>
            {
                this.CurrentKeyName = keyname;

            });
            this.SaveKey = new RelayCommand(async() =>
            {
                var formContent = new FormUrlEncodedContent(new[]
                  {
                        new KeyValuePair<string,string>("serviceNamespace", "test")
                  });

                await PostAPICall(await formContent.ReadAsStringAsync());
            },
            () =>{


                return true;
            });
        }
    }
}

