using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace TokenUniversalClient.Services
{
    public class MessageDialogService : IDialogService
    {
      
       public async Task ShowErrorAsync(string message, string title)
        {
            MessageDialog dialog = new MessageDialog(message, title);
            await dialog.ShowAsync();
        }
    }
}
