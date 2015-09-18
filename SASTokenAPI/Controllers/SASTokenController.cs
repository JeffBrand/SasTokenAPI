using Microsoft.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using SASTokenAPI.Models;
using System.Configuration;
using System.Web;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using System.Web.Routing;
using System.Security.Claims;
using SASTokenAPI.Services;

namespace SASTokenAPI.Controllers
{
    
    public class SASTokenController : ApiController
    {

        const int DEFAULT_TTL = 120;

        TimeSpan _ttl;
        ISASKeyRepository _keyRepo;

        public SASTokenController(ISASKeyRepository keyRepo)
        {
            _keyRepo = keyRepo;
            LoadTTL();
           
        }

        [Route("api/sastoken/test")]
        public IHttpActionResult GetTest()
        {
            return Ok();
        }

        /// <summary>
        ///  Return list of registered service namespaces
        /// </summary>
        /// <returns></returns>
        /// 
        [Authorize]
        [Route("api/sastoken/servicenamespaces")]
        public async Task<IHttpActionResult> GetRegisteredNamespaces()
        {
            var keys = await _keyRepo.GetRegistrationsAsync();
            var namespaces = (from k in keys select k.ServiceNamespace).Distinct();

            if (namespaces != null && namespaces.Count() > 0)
                return Ok(namespaces);
            else
                return NotFound();
        }

        /// <summary>
        /// Return list of registered event hubs for a given namespace
        /// </summary>
        /// <param name="serviceNamespace"></param>
        /// <returns></returns>
        [Authorize]
        [Route("api/sastoken/{serviceNamespace}/eventhubs")]
        public async Task<IHttpActionResult> GetEventHubsByNamespace(string serviceNamespace)
        {
            
            var keys = await _keyRepo.GetRegistrationsAsync();
            var eventhubs = (from k in keys where k.ServiceNamespace == serviceNamespace select k.EventHub).Distinct();
            if (eventhubs != null && eventhubs.Count() > 0)
                return Ok(eventhubs);
            else
                return NotFound();
        }

        [Authorize]
        [Route("api/sastoken/{serviceNamespace}/{eventHub}/keynames")]
        public async Task<IHttpActionResult> GetKeyNames(string serviceNamespace, string eventHub)
        {
            var keys = await _keyRepo.GetRegistrationsAsync();
            var names = from k in keys where k.ServiceNamespace == serviceNamespace && k.EventHub == eventHub select k.KeyName;

            if (names != null && names.Count() > 0)
                return Ok(names);
            else
                return NotFound();
        }

        [Authorize]
        [Route("api/sastoken/{serviceNamespace}/{eventHub}/{keyName}")]
        public async Task<IHttpActionResult> GetToken(string serviceNamespace, string eventHub, string keyName, string publisherId, string transport = "http")
        {

            if (await _keyRepo.ContainsKeyAsync(serviceNamespace, eventHub, keyName))
            {
                string serviceUri;
                string sasToken;

                var key = await _keyRepo.GetKeyAsync(serviceNamespace, eventHub, keyName);
                switch (transport.ToUpper())
                {
                    case "HTTP":
                    case "HTTPS":
                        serviceUri = ServiceBusEnvironment.CreateServiceUri("https", serviceNamespace, String.Format("{0}/publishers/{1}/messages", eventHub, publisherId))
                                        .ToString()
                                        .Trim('/');

                        sasToken = SharedAccessSignatureTokenProvider.GetSharedAccessSignature(keyName,key, serviceUri, _ttl);
                        return Ok(new SASTokenRespone() { Token = sasToken, TTL = _ttl });
                    case "AMQP":
                        serviceUri = ServiceBusEnvironment.CreateServiceUri("sb", serviceNamespace, String.Format("{0}/publishers/{1}", eventHub, publisherId))
                                       .ToString()
                                       .Trim('/');
                        sasToken = SharedAccessSignatureTokenProvider.GetSharedAccessSignature(keyName, key, serviceUri, _ttl);
                        return Ok(new SASTokenRespone { Token = sasToken, TTL = _ttl });
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
                _ttl = TimeSpan.FromMinutes(output);

            _ttl = TimeSpan.FromMinutes(DEFAULT_TTL);
        }

        [Authorize]
        [Route("api/sastoken")]
        [HttpPost]
        public async Task<IHttpActionResult> Post([FromBody] KeyRegistration keyRegistration)
        {
            if (string.IsNullOrWhiteSpace(keyRegistration.ServiceNamespace) ||
                string.IsNullOrWhiteSpace(keyRegistration.EventHub) ||
                string.IsNullOrWhiteSpace(keyRegistration.KeyName) ||
                string.IsNullOrWhiteSpace(keyRegistration.KeyValue))
                return BadRequest("Cannot submit null or empty properties");

            await _keyRepo.SaveKeyAsync(keyRegistration.ServiceNamespace, keyRegistration.EventHub, keyRegistration.KeyName, keyRegistration.KeyValue);
            return Ok();

        }
    }
}

