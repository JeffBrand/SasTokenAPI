using Microsoft.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using SASTokenAPI.Models;
using System.Configuration;

namespace SASTokenAPI.Controllers
{
    [Authorize]
    public class SASTokenController : ApiController
    {
        // Hoyo4498
        TimeSpan _ttl;
        Dictionary<string, string> _keys = new Dictionary<string, string>() {
                                                              {"sensors", "SWF1Fhj2Uy/T70X6O8gCcgzqqQOInYuS5CF5tr9LWbo="},
                                                              {"logs","q03kgRM32+OVvwCaGTNy17XBTvZJTqJl0mCnkOGjTBY="},
                                                              {"jbrandhackathon:telemetry", "5TPAlLrhNCMiovr0oj15rSXRqYVjeVA4k9ooPAHHrrE="},
                                                              {"jbrandhackathon:hackhub","8D6KSpFpZIldw8CyX1QzJ67ahAo0RybZKElPN0ffuAI="}
            };
        public SASTokenController()
        {
            var ttlSetting = ConfigurationManager.AppSettings["TokenTTL"];
            int minutes = int.Parse(ttlSetting);
            _ttl = TimeSpan.FromMinutes(minutes);
        }

        public IHttpActionResult Get(string serviceNamespace, string keyName, string eventhub, string publisherId, string transport = "http")
        {
            string serviceUri;
            string sasToken;

            var lookup = string.Format("{0}:{1}", serviceNamespace, eventhub);

            if (!_keys.ContainsKey(lookup))
                return BadRequest("Invalid serviceNamespace / eventhub");

            switch (transport.ToUpper())
            {
                case "HTTP":
                case "HTTPS":
                    serviceUri = ServiceBusEnvironment.CreateServiceUri("https", serviceNamespace, String.Format("{0}/publishers/{1}/messages", eventhub, publisherId))
                                    .ToString()
                                    .Trim('/');

                     sasToken = SharedAccessSignatureTokenProvider.GetSharedAccessSignature(keyName, _keys[lookup], serviceUri, _ttl);
                     return Ok(new SASTokenRespone() { Token = sasToken, TTL = _ttl });
                case "AMQP":
                    serviceUri = ServiceBusEnvironment.CreateServiceUri("sb", serviceNamespace, String.Format("{0}/publishers/{1}", eventhub, publisherId))
                                   .ToString()
                                   .Trim('/');
                    sasToken = SharedAccessSignatureTokenProvider.GetSharedAccessSignature(keyName, _keys[lookup], serviceUri, _ttl);
                    return Ok(new SASTokenRespone { Token = sasToken, TTL = _ttl });
            default:
                    return BadRequest("Invalid transport type");
            }
           
        }

        public void Post(string serviceName, string eventHub, string key)
        {

        }
    }
}

