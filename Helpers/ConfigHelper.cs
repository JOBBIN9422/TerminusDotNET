using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TerminusDotNetCore.Helpers
{
    public class ConfigHelper
    {
        public static JObject ReadConfig()
        {
            return JsonConvert.DeserializeObject<JObject>(File.ReadAllText("appsettings.json"));
        }

        public static void UpdateConfig(JObject newSettings)
        {
            File.WriteAllText("appsettings.json", JsonConvert.SerializeObject(newSettings));
        }
    }
}
