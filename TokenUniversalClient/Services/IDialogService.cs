using System.Threading.Tasks;

namespace TokenUniversalClient.Services
{
    public interface IDialogService
    {
        Task ShowErrorAsync(string message, string title);
    }
}