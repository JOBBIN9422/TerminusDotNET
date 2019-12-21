using System;
using System.Net.NetworkInformation;
using System.Text;

namespace TerminusDotNetCore.Services
{
    class PingService
    {
        public static PingReply PingRequest(string ip_addr)
        {
            try
            {
                Ping pingSender = new Ping();
                PingOptions options = new PingOptions();

                // Use the default Ttl value which is 128,
                // but change the fragmentation behavior.
                options.DontFragment = true;

                // Create a buffer of 32 bytes of data to be transmitted.
                string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                int timeout = 120;

                PingReply reply = pingSender.Send(ip_addr, timeout, buffer, options);

                return reply;
            }
            catch (InvalidOperationException ex)
            {
                Console.Write("Exception " + ex + ", IP could not be reached.");

                throw ex;
            }
        }
    }
}