using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SpiderNs
{
    public class Page
    {
        public int ID { get; set; }
        public string Uri { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public double Score { get; set; } = -1;
    }
}
