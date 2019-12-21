using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;
using Discord.Commands;
using TerminusDotNetCore.Services;

namespace TerminusDotNetCore.Modules
{
    public class PingModule : ModuleBase<SocketCommandContext>
    {
        [Command("Ping")]
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
                string ip_addr = "";

                //Custom ip for minecraft
                if (message == "minecraft")
                {
                    ip_addr = "98.200.245.252";
                }
                else
                {
                    ip_addr = message;
                }

                //Validates given ip address
                if (!(IPAddress.TryParse(ip_addr.Split(':')[0], out IP)))
                {
                    await ReplyAsync(ip_addr + " is not a valid IP address.");
                }
                else
                {
                    //Ping the specified server
                    PingReply reply;
                    reply = PingServer.PingRequest(ip_addr);

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
                        await ReplyAsync("Address " + ip_addr + " is currently unreachable.");
                    }
                }
            }
        }
    }
}