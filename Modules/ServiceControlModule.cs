using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerminusDotNetCore.Services;

namespace TerminusDotNetCore.Modules
{
    public class ServiceControlModule
    {
        //allow services to reply on a text channel
        //Task ServiceReplyAsync(string s, EmbedBuilder embedBuilder = null);

	public CustomService Service { get; set; }
	    
	public ServiceControlModule(CustomService service)
	{
	    Service = service;
	}
    }
}
