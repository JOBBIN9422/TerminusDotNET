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
            if (stock_name == null)
            {
                await ServiceReplyAsync("Please add a stock name.");
                //await ReplyAsync("Please add a stock name.");
                return;
            }
            await ServiceReplyAsync("Downloading stock data for " + stock_name);
            //await ReplyAsync("Downloading stock data for " + stock_name);
            
            await StonksHelper.DownloadImage(stock_name);

            await ServiceReplyAsync("Download Finished");
            //await ReplyAsync("Download Finished");

            await Context.Channel.SendFileAsync("../../assets/images/graph.png");
            //await Context.Channel.SendFileAsync("../../assets/images/graph.png");
        }

    }
}
