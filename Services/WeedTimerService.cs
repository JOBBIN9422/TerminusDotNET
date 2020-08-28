using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TerminusDotNetCore.Modules;

namespace TerminusDotNetCore.Services
{
    class WeedTimerService : ICustomService
    {
        public IConfiguration Config { get; set; }
        public ServiceControlModule ParentModule { get; set; }

        private AudioService _audioService;

        private readonly Timer _timer;

        public WeedTimerService(IConfiguration config, AudioService audioService)
        {
            Console.WriteLine("INIT WEED TIMER");
            Config = config;
            _audioService = audioService;

            DateTime now = DateTime.Now;
            DateTime fourTwenty = DateTime.Today.AddHours(17);

            if (now > fourTwenty)
            {
                fourTwenty = fourTwenty.AddDays(1.0);
            }
            int fourTwentyMs = (int)(fourTwenty - now).TotalMilliseconds;
            _timer = new Timer(async _ => await _audioService.PlayWeed(), null, fourTwentyMs, (int)new TimeSpan(24, 0, 0).TotalMilliseconds);
        }
    }
}
