using SASTokenAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SASTokenAPI.Services
{
    public interface ISASKeyRepository
    {
        Task<IEnumerable<SASKeyRegistration>> GetRegistrationsAsync();

        Task<string> GetKeyAsync(string serviceNamespace, string eventHub, string keyName);
        Task<string> GetKeyAsync(SASKeyRegistration registration);

        Task SaveKeyAsync(string serviceNamespace, string eventHub, string keyName, string keyValue);
        Task SaveKeyAsync(SASKeyRegistration keyRegistration);
        Task DeleteKeyAsync(string serviceNamespace, string eventHub, string keyName);
        Task DeleteKeyAsync(SASKeyRegistration keyRegistration);

      
     
    }
}