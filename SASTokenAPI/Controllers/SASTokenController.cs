using Microsoft.ServiceBus;
using SASTokenAPI.Filters;
using SASTokenAPI.Models;
using SASTokenAPI.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace SASTokenAPI.Controllers
{
    [RoutePrefix("api/v1/sastoken")]
    //[HMACAuthentication]
    public class SasTokenController : ApiController
    {
        const int DEFAULT_TTL_MINUTES = 120;

        TimeSpan _ttlFromConfig;
        private ISASKeyRepository _keyRepo;

        public SasTokenController(ISASKeyRepository keyRepo)
        {
            _keyRepo = keyRepo;
            LoadTTL();
        }

        
   
        [Route("{serviceNamespace}/{eventHub}/{keyName}/{publisherId}")]
        public async Task<IHttpActionResult> GetToken(string serviceNamespace, string eventHub, string keyName, string publisherId, string transport = "http", int ttlMinutes = -1)
        {
            var key = await _keyRepo.GetKeyAsync(serviceNamespace, eventHub, keyName);
            if (key != null)
            {
                string serviceUri;
                string sasToken;

                TimeSpan ttl;
                if (ttlMinutes < 0)
                    ttl = _ttlFromConfig;
                else
                    ttl = TimeSpan.FromMinutes(ttlMinutes);
                
                switch (transport.ToUpper())
                {
                    case "HTTP":
                    case "HTTPS":
                        serviceUri = ServiceBusEnvironment.CreateServiceUri("https", serviceNamespace, String.Format("{0}/publishers/{1}/messages", eventHub, publisherId)).ToString().Trim('/');

                        sasToken = SharedAccessSignatureTokenProvider.GetSharedAccessSignature(keyName, key, serviceUri, ttl);
                        return Ok(new SASTokenResponse() { Token = sasToken, TTL = ttl });
                    case "AMQP":
                        serviceUri = ServiceBusEnvironment.CreateServiceUri("sb", serviceNamespace, String.Format("{0}/publishers/{1}/messages", eventHub, publisherId)).ToString().Trim('/');
                        sasToken = SharedAccessSignatureTokenProvider.GetSharedAccessSignature(keyName, key, serviceUri, ttl);
                        return Ok(new SASTokenResponse { Token = sasToken, TTL = ttl });
                    default:
                        return BadRequest("Invalid transport type");
                }
            }

            return NotFound(); ;
        }

        private void LoadTTL()
        {
            var ttlSetting = ConfigurationManager.AppSettings["TokenTTL"];
            int output = 0;
            if (int.TryParse(ttlSetting, out output))
                _ttlFromConfig = TimeSpan.FromMinutes(output);

            _ttlFromConfig = TimeSpan.FromMinutes(DEFAULT_TTL_MINUTES);
        }

    }
}
