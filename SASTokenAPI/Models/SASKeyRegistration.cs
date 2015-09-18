using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SASTokenAPI.Models
{
    public class SASKeyRegistration
    {
        public string ServiceNamespace { get; set; }
        public string EventHub { get; set; }
        public string KeyName { get; set; }
        public string KeyValue { get; set; }
    }
}