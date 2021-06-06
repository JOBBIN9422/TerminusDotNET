using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerminusDotNetCore.Modules;

namespace TerminusDotNetCore.Services
{
    public class RedditService : ICustomService
    {
        public IConfiguration Config { get; set; }
        public ServiceControlModule ParentModule { get; set; }
    }
}
