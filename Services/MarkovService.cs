using System;
using System.Collections.Generic;
using System.Text;
using TerminusDotNetCore.Modules;
using LinqToTwitter;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;

namespace TerminusDotNetCore.Services
{
    public class MarkovService : ICustomService
    {
        public ServiceControlModule ParentModule { get; set; }
        private Random _random = new Random();

        public MarkovService()
        {
            IConfiguration config = new ConfigurationBuilder()
                                        .AddJsonFile("appsettings.json", true, true)
                                        .Build();
        }
    }
}
