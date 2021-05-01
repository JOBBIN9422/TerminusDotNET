﻿using Quartz;
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
        public string Name { get; set; }

        private readonly AudioService _audioService;
        public AudioEventJob(AudioService service)
        {
            _audioService = service;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            string cronString = (string)dataMap.Get("CronString");
            Name = (string)dataMap.Get("SongName");
            ulong channelId = ulong.Parse((string)dataMap.Get("ChannelId"));

            await _audioService.SaveAudioEvent(Name, cronString);

            LocalAudioItem song = new LocalAudioItem()
            {
                Path = _audioService.GetAliasedSongPath(Name),
                PlayChannelId = channelId,
                AudioSource = FileAudioType.Local,
                OwnerName = "Terminus.NET",
                DisplayName = Name,
                StartTime = DateTime.Now
            };
            await _audioService.PlayAudioEvent(song);
        }
    }
}
