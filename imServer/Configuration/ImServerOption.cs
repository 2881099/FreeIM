using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace imServer.Configuration
{
    public class ImServerOption : IOptions<ImServerOption>
    {
        public string CSRedisClient { get; set; }
        public string Servers { get; set; }
        public string Server { get; set; }
        public ImServerOption Value => this;
    }
}
