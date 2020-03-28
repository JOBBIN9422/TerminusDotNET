using Discord;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerminusDotNetCore.Modules;

namespace TerminusDotNetCore.Services
{
    public interface ICustomService
    {
        //shared config object - set in controlling module constructor (passed via DI)
        public IConfiguration Config { get; set; }
        ServiceControlModule ParentModule { get; set; }
    }
}
