using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;
using Discord.Commands;
using TerminusDotNetCore.Services;
using Microsoft.Extensions.Configuration;

namespace TerminusDotNetCore.Modules
{
    public class PingModule : ServiceControlModule
    {
        public PingModule(IConfiguration config) : base(config) { }

        [Command("ping", RunMode = RunMode.Async)]
        [Summary("Pings a server address that must be specified as an argument")]
        public async Task PingAsync([Remainder]string message = null)
        {
            IPAddress IP;

            //Validates whether argument exists
            if (string.IsNullOrEmpty(message))
            {
                await ReplyAsync("Must supply an IP address.");
            }
            else
            {
                string ipAddr = "";

                //Custom ip for minecraft
                if (message == "minecraft")
                {
                    ipAddr = Config["MinecraftIP"];
                }
                else
                {
                    ipAddr = message;
                }

                //Validates given ip address
                if (!(IPAddress.TryParse(ipAddr.Split(':')[0], out IP)))
                {
                    await ReplyAsync(ipAddr + " is not a valid IP address.");
                }
                else
                {
                    //Ping the specified server
                    PingReply reply;
                    reply = PingService.PingRequest(ipAddr);

                    if (reply.Status == IPStatus.Success)
                    {
                        await ReplyAsync("PING SUCCESSFUL");
                        await ReplyAsync("Address: " + reply.Address.ToString());
                        await ReplyAsync("RoundTrip time: " + reply.RoundtripTime + "s");
                        await ReplyAsync("Time to live: " + reply.Options.Ttl);
                        await ReplyAsync("Don't fragment: " + reply.Options.DontFragment);
                        await ReplyAsync("Buffer size: " + reply.Buffer.Length);
                    }
                    else
                    {
                        await ReplyAsync("Address " + ipAddr + " is currently unreachable.");
                    }
                }
            }
        }
    }
}