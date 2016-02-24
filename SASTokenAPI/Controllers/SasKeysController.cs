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
using SASTokenAPI.Filters;

namespace SASTokenAPI.Controllers
{
    [RoutePrefix("api/v1/saskeys")]
    public class SasKeysController : ApiController
    {


        ISASKeyRepository _keyRepo;

        public SasKeysController(ISASKeyRepository keyRepo)
        {
            _keyRepo = keyRepo;

        }


        /// <summary>
        ///  Return list of registered service namespaces
        /// </summary>
        /// <returns></returns>
        /// 

        [Route("servicenamespaces")]
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
        [Route("{serviceNamespace}/eventhubs")]
        public async Task<IHttpActionResult> GetEventHubsByNamespace(string serviceNamespace)
        {

            var keys = await _keyRepo.GetRegistrationsAsync();
            var eventhubs = (from k in keys where k.ServiceNamespace == serviceNamespace select k.EventHub).Distinct();
            if (eventhubs != null && eventhubs.Count() > 0)
                return Ok(eventhubs);
            else
                return NotFound();
        }

        [Route("{serviceNamespace}/{eventHub}/keynames")]
        public async Task<IHttpActionResult> GetKeyNames(string serviceNamespace, string eventHub)
        {
            var keys = await _keyRepo.GetRegistrationsAsync();
            var names = from k in keys where k.ServiceNamespace == serviceNamespace && k.EventHub == eventHub select k.KeyName;

            if (names != null && names.Count() > 0)
                return Ok(names);
            else
                return NotFound();
        }




        [Route("")]
        [HttpPost]
        public async Task<IHttpActionResult> Post([FromBody] SASKeyRegistration keyRegistration)
        {
            if (string.IsNullOrWhiteSpace(keyRegistration.ServiceNamespace) ||
                string.IsNullOrWhiteSpace(keyRegistration.EventHub) ||
                string.IsNullOrWhiteSpace(keyRegistration.KeyName) ||
                string.IsNullOrWhiteSpace(keyRegistration.KeyValue))
                return BadRequest("Cannot submit null or empty properties");

            await _keyRepo.SaveKeyAsync(keyRegistration.ServiceNamespace, keyRegistration.EventHub, keyRegistration.KeyName, keyRegistration.KeyValue);
            return Created<SASKeyRegistration>("", keyRegistration);

        }

        [Route("{serviceNamespace}/{eventHub}/{keyname}")]
        public async Task<IHttpActionResult> Delete(string serviceNamespace, string eventHub, string keyname)
        {
            await _keyRepo.DeleteKeyAsync(serviceNamespace, eventHub, keyname);
            return Ok();
        }
    }
}

