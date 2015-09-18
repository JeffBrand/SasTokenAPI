using SASTokenAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASTokenAPI.Services
{
    public interface IAPIKeyService
    {
        void DeleteAppAsync(string appId);
        Task<IEnumerable<string>> GetAppsAsync();
        Task<IEnumerable<ApiKeyRegistration>> GetKeysAsync(string appId);
        Task<string> CreateKeyAsync(string appId, string keyName);
        void DeleteKeyAsync(string appId, string keyName);
        Task<string> ResetKeyAsync(string appId, string keyName);

    }
}
