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
    public class Stonks : ServiceControlModule
    {
        public Stonks(IConfiguration config) : base(config)
        {

        }

        public object PingSever { get; private set; }

        [Command("stonks")]
        public async Task StonksAsync([Summary("Stock acronym for desired company")]string stock_name = null)
        {
            if (string.IsNullOrEmpty(stock_name))
            {
                await ServiceReplyAsync("Please add a stock name.");
                return;
            }
            await ServiceReplyAsync("Downloading stock data for " + stock_name + ".");
            
            string graphImgPath = await StonksHelper.DownloadImage(stock_name);

            await ServiceReplyAsync("Download finished.");

            await Context.Channel.SendFileAsync(graphImgPath);
        }

    }
}
