using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using TerminusDotNetCore.Modules;
using TerminusDotNetCore.Helpers;
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
	private MarkovHelper _clickbaitMarkov = new MarkovHelper(Path.Combine("assets", "clickbait.txt"));

	public string GenerateClickbaitSentence()
	{
            return _clickbaitMarkov.GenerateSentence();
	}
    }
}
