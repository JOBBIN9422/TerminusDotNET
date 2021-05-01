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
        private readonly AudioService _audioService;
        public AudioEventJob(AudioService service)
        {
            _audioService = service;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            string songName = (string)dataMap.Get("SongName");
            ulong channelId = ulong.Parse((string)dataMap.Get("ChannelId"));

            LocalAudioItem song = new LocalAudioItem()
            {
                Path = _audioService.GetAliasedSongPath(songName),
                PlayChannelId = channelId,
                AudioSource = FileAudioType.Local,
                OwnerName = "Terminus.NET",
                DisplayName = songName,
                StartTime = DateTime.Now
            };
            await _audioService.PlayAudioEvent(song);
        }
    }
}
