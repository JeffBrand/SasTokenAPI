using SASTokenAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SASTokenAPI.Services
{
    public interface IKeyRepository
    {
        Task<IEnumerable<KeyRegistration>> GetRegistrationsAsync();

        Task<string> GetKeyAsync(string serviceNamespace, string eventHub, string keyName);
        Task<string> GetKeyAsync(KeyRegistration registration);

        Task SaveKeyAsync(string serviceNamespace, string eventHub, string keyName, string keyValue);
        Task SaveKeyAsync(KeyRegistration keyRegistration);
        Task DeleteKeyAsync(string serviceNamespace, string eventHub, string keyName);
        Task DeleteKeyAsync(KeyRegistration keyRegistration);

        Task<bool> ContainsKeyAsync(string serviceNamespace, string eventHub, string keyName);
        Task<bool> ContainsKeyAsync(KeyRegistration keyRegistration);
     
    }
}