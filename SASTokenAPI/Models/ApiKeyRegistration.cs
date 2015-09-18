using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SASTokenAPI.Models
{
    public class ApiKeyRegistration
    {
        public string AppId { get; set; }
        public string KeyName { get; set; }
        public string Key { get; set; }
    }
}