using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerminusDotNetCore.Services;
using System.Net.NetworkInformation;
using System.Net;
using Microsoft.Extensions.Configuration;
using TerminusDotNetCore.Helpers;
using System.IO;

namespace TerminusDotNetCore.Modules
{
    public class StonksModule : ServiceControlModule
    {
        public StonksModule(IConfiguration config) : base(config)
        {

        }

        public object PingSever { get; private set; }

        [Command("stonks")]
        [Summary("Get a stock chart for the given company.")]
        public async Task StonksAsync([Summary("Stock acronym for desired company")]string stockName = null)
        {
            if (string.IsNullOrEmpty(stockName))
            {
                await ServiceReplyAsync("Please add a stock name.");
                return;
            }
            await ServiceReplyAsync("Downloading stock data for " + stockName + ".");
            
            string graphImgPath = await StonksHelper.DownloadImage(Config["StonksIP"], stockName);

            await ServiceReplyAsync("Download finished.");

            await Context.Channel.SendFileAsync(graphImgPath);
        }

    }
}
