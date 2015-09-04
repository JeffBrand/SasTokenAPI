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

namespace SASTokenAPI.Controllers
{
    [Authorize]
    public class SASTokenController : ApiController
    {

        const int DEFAULT_TTL = 120;

        TimeSpan _ttl;
        Dictionary<string, string> keys;

        public SASTokenController()
        {
            LoadKeys();
            LoadTTL();
           
        }

        /// <summary>
        ///  Return list of registered service namespaces
        /// </summary>
        /// <returns></returns>
        public async Task<IHttpActionResult> GetList()
        {
            var results = (from m in keys.Keys select m.Split(new char[] { ':' })[0]);

            if (results != null)
            {

                return Ok(results.ToArray());
            }

            return NotFound(); ;
        }

        /// <summary>
        /// Return list of registered event hubs for a given namespace
        /// </summary>
        /// <param name="serviceNamespace"></param>
        /// <returns></returns>
        [Route("api/sastoken/{serviceNamespace}")]
        public async Task<IHttpActionResult> GetListByNamespace(string serviceNamespace)
        {
            var results = (from m in keys.Keys where m.StartsWith(serviceNamespace) select m.Split(new char[] { ':' })[1]);

            if (results != null)
            {

                return Ok(results.ToArray());
            }

            return NotFound(); ;
        }

        [Route("api/sastoken/{serviceNamespace}/{eventHub}")]
        public async Task<IHttpActionResult> GetListByEventHubs(string serviceNamespace, string eventHub)
        {
            var results = (from m in keys.Keys where m.StartsWith(serviceNamespace + ":" + eventHub) select m.Split(new char[] { ':' })[2]);

            if (results != null)
            {

                return Ok(results.ToArray());
            }

            return NotFound(); ;
        }

        [Route("api/sastoken/{serviceNamespace}/{eventHub}/{keyName}")]
        public async Task<IHttpActionResult> GetToken(string serviceNamespace, string eventHub, string keyName, string publisherId, string transport = "http")
        {
            var lookup = string.Format("{0}:{1}:{2}", serviceNamespace,eventHub,keyName);

            if (keys.ContainsKey(lookup))
            {
                string serviceUri;
                string sasToken;
                
                switch (transport.ToUpper())
                {
                    case "HTTP":
                    case "HTTPS":
                        serviceUri = ServiceBusEnvironment.CreateServiceUri("https", serviceNamespace, String.Format("{0}/publishers/{1}/messages", eventHub, publisherId))
                                        .ToString()
                                        .Trim('/');

                        sasToken = SharedAccessSignatureTokenProvider.GetSharedAccessSignature(keyName, keys[lookup], serviceUri, _ttl);
                        return Ok(new SASTokenRespone() { Token = sasToken, TTL = _ttl });
                    case "AMQP":
                        serviceUri = ServiceBusEnvironment.CreateServiceUri("sb", serviceNamespace, String.Format("{0}/publishers/{1}", eventHub, publisherId))
                                       .ToString()
                                       .Trim('/');
                        sasToken = SharedAccessSignatureTokenProvider.GetSharedAccessSignature(keyName, keys[lookup], serviceUri, _ttl);
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

        private async void LoadKeys()
        {
            if (HttpContext.Current.Application["SasKeys"] != null)
                keys = HttpContext.Current.Application["SasKeys"] as Dictionary<string, string>;

            var filePath = HttpContext.Current.Server.MapPath("~/sas.dat");
            if (File.Exists(filePath))
            {
                var file = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var reader = new StreamReader(file);
                var json = await reader.ReadToEndAsync();

                try
                {
                    var keys = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    HttpContext.Current.Application["SasKeys"] = keys;
                    
                }
                catch (Exception)
                {
                    keys = null;
                }
            }

            keys =  new Dictionary<string, string>() { { "testServiceNamespace:testEventHub:testSenderKey","test" } };
           
        }

        private async Task SaveKeys(Dictionary<string,string> keys)
        {
            var json = JsonConvert.SerializeObject(keys);
            var file = new FileStream(HttpContext.Current.Server.MapPath("~/sas.dat"), FileMode.CreateNew, FileAccess.Write);
            var writer = new StreamWriter(file);
            await writer.WriteLineAsync(json);
            await writer.FlushAsync();
            file.Close();
        }

        public void Post(string serviceName, string eventHub, string key)
        {

        }
    }
}

