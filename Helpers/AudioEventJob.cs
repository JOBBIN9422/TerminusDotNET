using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerminusDotNetCore.Services;

namespace TerminusDotNetCore.Helpers
{
    public class AudioEventJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            AudioService audioService = (AudioService)dataMap.Get("AudioService");
            LocalAudioItem audioEvent = (LocalAudioItem)dataMap.Get("AudioEvent");

            await audioService.PlayAudioEvent(audioEvent);
        }
    }
}
