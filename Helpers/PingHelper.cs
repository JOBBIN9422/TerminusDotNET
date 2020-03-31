namespace TerminusDotNetCore.Helpers
{
    public class PingHelper
    {
        private string minecraft_ip;

        public string GetMinecraftIP()
        {
            return this.minecraft_ip;
        }

        //default contructor
        public PingHelper()
        {
            minecraft_ip = "98.200.245.252";
        }
    }


}