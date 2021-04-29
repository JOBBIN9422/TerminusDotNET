using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Spi;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerminusDotNetCore.Helpers
{
    class AudioEventJobFactory : IJobFactory
    {
        protected readonly IServiceProvider _serviceProvider;

        public AudioEventJobFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            try
            {
                return _serviceProvider.GetService(bundle.JobDetail.JobType) as IJob;
            }
            catch (Exception ex)
            {
                throw new SchedulerException($"Problem instantiating class '{bundle.JobDetail.JobType.FullName}'", ex);
            }
        }

        public void ReturnJob(IJob job)
        {
            var disposable = job as IDisposable;
            disposable?.Dispose();
        }
    }
}
