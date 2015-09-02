using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SASTokenAPI.Models
{
    public class SASTokenRespone
    {
        public string Token { get; set; }
        public TimeSpan TTL { get; set; }
    }
}