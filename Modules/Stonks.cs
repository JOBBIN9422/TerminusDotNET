using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminus.Services;
using System.Net.NetworkInformation;
using System.Net;
using Terminus.Helpers;

namespace Terminus.Modules
{
    public class Stonks : ModuleBase<SocketCommandContext>
    {
        public object PingSever { get; private set; }

        [Command("stonks")]
        public async Task StonksAsync([Summary("Stock acronym for desired company")]string stock_name = null)
        {
            if (stock_name == null)
            {
                ServiceReplyAysnc("Please add a stock name.");
                //await ReplyAsync("Please add a stock name.");
                return;
            }
            ServiceReplyAysnc("Downloading stock data for " + stock_name);
            //await ReplyAsync("Downloading stock data for " + stock_name);
            
            await ImageHelper.DownloadImage(stock_name);

            ServiceReplyAysnc("Download Finished");
            //await ReplyAsync("Download Finished");

            await SendImage("../../assets/images/graph.png");
            //await Context.Channel.SendFileAsync("../../assets/images/graph.png");
        }

    }
}
