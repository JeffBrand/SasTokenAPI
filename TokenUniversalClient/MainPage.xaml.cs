using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TokenUniversalClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const string tenant = "iotdevices.onmicrosoft.com"; 
        const string clientId = "1825ade3-f5a9-482f-a9be-5ac77a4a41d8"; 
        const string aadInstance = "https://login.microsoftonline.com/{0}";
        static string authority = String.Format(CultureInfo.InvariantCulture, aadInstance, tenant);

        private static string sasTokenResourceId = "https://iotdevices.onmicrosoft.com/sastokenapi";
        private static string sasTokenBaseAddress = "https://mtcsastokenapi.azurewebsites.net";

        private HttpClient httpClient = new HttpClient();
        private static AuthenticationContext authContext;
        private Uri redirectURI;


        public MainPage()
        {
            this.InitializeComponent();

            //
            // Every Windows Store application has a unique URI.
            // Windows ensures that only this application will receive messages sent to this URI.
            // ADAL uses this URI as the application's redirect URI to receive OAuth responses.
            // 
            // To determine this application's redirect URI, which is necessary when registering the app
            //      in AAD, set a breakpoint on the next line, run the app, and copy the string value of the URI.
            //      This is the only purposes of this line of code, it has no functional purpose in the application.
            //

            redirectURI = Windows.Security.Authentication.Web.WebAuthenticationBroker.GetCurrentApplicationCallbackUri();
            authContext = new AuthenticationContext(authority);

            //
            // Out of the box, this sample is *not* configured to work with Windows Integrated Authentication (WIA)
            // when used with a federated Azure Active Directory domain.  To work with WIA the application manifest
            // must enable additional capabilities.  These are not configured by default for this sample because 
            // applications requesting the Enterprise Authentication or Shared User Certificates capabilities require 
            // a higher level of verification to be accepted into the Windows Store, and not all developers may wish
            // to perform the higher level of verification.
            //
            // To enable Windows Integrated Authentication, in Package.appxmanifest, in the Capabilities tab, enable:
            // * Enterprise Authentication
            // * Private Networks (Client & Server)
            // * Shared User Certificates
            //
            // Plus uncomment the following line of code:
            // 
            // authContext.UseCorporateNetwork = true;

        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            var result = await authContext.AcquireTokenAsync(sasTokenResourceId, clientId, redirectURI);

            if (result.Status != AuthenticationStatus.Success)
            {
                if (result.Error == "authentication_canceled")
                {
                    // The user cancelled the sign-in, no need to display a message.
                }
                else
                {
                    MessageDialog dialog = new MessageDialog(string.Format("If the error continues, please contact your administrator.\n\nError: {0}\n\nError Description:\n\n{1}", result.Error, result.ErrorDescription), "Sorry, an error occurred while signing you in.");
                    await dialog.ShowAsync();
                }
                return;
            }

            var queryString = string.Format("serviceNamespace={0}&keyName={1}&eventHub={2}&publisherId={3}", serviceNamespace.Text, keyName.Text, eventHub.Text, publisherId.Text);

            //
            // Add the access token to the Authorization Header of the call to the To Do list service, and call the service.
            //
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            HttpResponseMessage response = await httpClient.GetAsync(sasTokenBaseAddress + "/api/sasToken?" + queryString);

            if (response.IsSuccessStatusCode)
            {
                // Read the response as a Json Array and databind to the GridView to display todo items
                var sasToken = await response.Content.ReadAsStringAsync();

                this.sasToken.Text = sasToken;
            }
            else
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // If the To Do list service returns access denied, clear the token cache and have the user sign-in again.
                    MessageDialog dialog = new MessageDialog("Sorry, you don't have access to the To Do Service.  Please sign-in again.");
                    await dialog.ShowAsync();
                    authContext.TokenCache.Clear();
                }
                else
                {
                    MessageDialog dialog = new MessageDialog("Sorry, an error occurred accessing your To Do list.  Please try again.");
                    await dialog.ShowAsync();
                }
            }
        }

    }
}

