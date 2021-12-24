using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using TerminusDotNetCore.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TerminusDotNetCore.Helpers;
using System.Xml.Linq;
using System.Linq;
using Quartz.Impl;
using Quartz;
using Quartz.Spi;
using Discord.Interactions;

namespace TerminusDotNetCore
{
    public class Bot
    {
        public ulong BeanId { get; private set; } = 647265224205926410;
        public DateTime StartTime { get; private set; }

        public DiscordSocketClient Client { get; private set; }

        public bool IsRegexActive { get; set; } = true;

        public Dictionary<string, string> InstalledLibraries { get; private set; } = new Dictionary<string, string>();

        //command services
        private IServiceProvider _serviceProvider;

        private InteractionService _interactionService;

        //ignored channels
        private List<ulong> _blacklistChannels = new List<ulong>();

        private IConfiguration _config = new ConfigurationBuilder()
                                        .AddJsonFile("appsettings.json", true, true)
                                        .AddJsonFile("secrets.json", true, true)
                                        .Build();

        public async Task Initialize()
        {
            StartTime = DateTime.Now;

            //init custom services
            _serviceProvider = await InstallServices();
            InitScheduler();

            //load libraries into version dict
            PopulateInstalledLibrariesList();

            //instantiate client and register log event handler
            DiscordSocketConfig config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.All
            };
            Client = new DiscordSocketClient(config);
            Client.Log += Logger.Log;
            Client.Ready += InitInteractionService;

            //verify that each required client secret is in the secrets file
            Dictionary<string, string> requiredSecrets = new Dictionary<string, string>()
            {
                {"DiscordToken", "Token to connect to your discord server"}
            };

            //verify that each required config entry is in the appsettings file
            Dictionary<string, string> requiredConfigs = new Dictionary<string, string>()
            {
                {"ServerId", "ID of the Discord server"},
                {"AudioChannelId", "ID of main audio channel to play audio in"},
                {"WeedChannelId", "ID of weed sesh audio channel"}
            };

            //alert in console for each missing config field
            foreach (var configEntry in requiredConfigs)
            {
                if (_config[configEntry.Key] == null)
                {
                    await Logger.Log(new LogMessage(LogSeverity.Warning, "appsettings.json", $"WARN: Missing item in appsettings config file :: {configEntry.Key} --- Description :: {configEntry.Value}"));
                }
            }

            //alert in console for each missing client secret field
            foreach (var secretEntry in requiredSecrets)
            {
                if (_config[secretEntry.Key] == null)
                {
                    await Logger.Log(new LogMessage(LogSeverity.Warning, "secrets.json", $"WARN: Missing item in secrets file :: {secretEntry.Key} --- Description :: {secretEntry.Value}"));
                }
            }

            //load regex state from config
            IsRegexActive = bool.Parse(_config["RegexEnabled"]);

            //log in & start the client
            string token = _config["DiscordToken"];
            await Client.LoginAsync(TokenType.Bot, token);
            await Client.StartAsync();

            //load blacklisted channels
            var blacklistSection = _config.GetSection("BlacklistChannels");
            foreach (var section in blacklistSection.GetChildren())
            {
                ulong id = ulong.Parse(section.Value);
                _blacklistChannels.Add(id);
            }

            Client.InteractionCreated += HandleInteractionAsync;

            //hang out for now
            await Task.Delay(-1);
        }

        private async Task HandleInteractionAsync(SocketInteraction arg)
        {
            try
            {
                var ctx = new SocketInteractionContext(Client, arg);
                await _interactionService.ExecuteCommandAsync(ctx, _serviceProvider);
            }
            catch (Exception e)
            {
                await Logger.Log(new LogMessage(LogSeverity.Warning, "InteractionHandler", $"Error: {e}"));
            }
        }

        //slash command handler
        private async Task OnSlashCommandExecutedAsync(SlashCommandInfo arg1, IInteractionContext arg2, Discord.Interactions.IResult arg3)
        {
            if (!arg3.IsSuccess)
            {
                await Logger.Log(new LogMessage(LogSeverity.Warning, "SlashCmdHandler", $"Error in {arg1.Name}: {arg3.ErrorReason}"));
            }
        }

        //set client and guild for audio service BEFORE any playback commands are executed
        private async Task InitInteractionService()
        {
            //init interaction service
            InteractionServiceConfig intSvcConfig = new InteractionServiceConfig()
            {
                DefaultRunMode = Discord.Interactions.RunMode.Async
            };
            _interactionService = new InteractionService(Client.Rest, intSvcConfig);
            _interactionService.Log += Logger.Log;

            await _interactionService.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: _serviceProvider);
            var registeredCommands = await _interactionService.RegisterCommandsToGuildAsync(ulong.Parse(_config["ServerId"]));
            _interactionService.SlashCommandExecuted += OnSlashCommandExecutedAsync;

            foreach (var command in registeredCommands)
            {
                await Logger.Log(new LogMessage(LogSeverity.Info, "Client", $"Registered command: {command.Name}"));
            }

            AudioService audioService = _serviceProvider.GetService(typeof(AudioService)) as AudioService;
            if (audioService != null)
            {
                audioService.Client = Client;
                audioService.Guild = Client.GetGuild(ulong.Parse(_config["ServerId"]));
            }
        }

        private async Task<IServiceProvider> InstallServices()
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            IScheduler scheduler = await schedulerFactory.GetScheduler();

            var serviceCollection = new ServiceCollection();

            //new custom services (and objects passed via DI) get added here
            serviceCollection.AddSingleton<TextEditService>()
                             .AddSingleton(_config)
                             //.AddSingleton<ImageService>()
                             //.AddSingleton<TextEditService>()
                             //.AddSingleton<TwitterService>()
                             //.AddSingleton<AudioService>()
                             //.AddSingleton<MarkovService>()
                             //.AddSingleton<TicTacToeService>()
                             //.AddSingleton<IronPythonService>()
                             //.AddSingleton<ServerManagementService>()
                             //.AddSingleton(new Random())
                             //.AddSingleton(this)
                             .AddSingleton(scheduler)
                             .AddTransient<AudioEventJob>(); 

            return serviceCollection.BuildServiceProvider();
        }

        private void InitScheduler()
        {
            IScheduler scheduler = _serviceProvider.GetService(typeof(IScheduler)) as IScheduler;
            IJobFactory jobFactory = new AudioEventJobFactory(_serviceProvider);
            scheduler.JobFactory = jobFactory;
        }

        private void PopulateInstalledLibrariesList()
        {
            //open the .csproj
            XNamespace msBuild = "http://schemas.microsoft.com/developer/msbuild/2003";
            XDocument projectDoc = XDocument.Load("TerminusDotNetCore.csproj");

            //get nuget package names
            IEnumerable<string> references = projectDoc.Element("Project")
                                                       .Element("ItemGroup")
                                                       .Elements("PackageReference")
                                                       .Attributes("Include")
                                                       .Select(e => e.Value);
            //get nuget package versions
            IEnumerable<string> versions = projectDoc.Element("Project")
                                                       .Element("ItemGroup")
                                                       .Elements("PackageReference")
                                                       .Attributes("Version")
                                                       .Select(e => e.Value);

            //zip into dict of package-version pairs
            InstalledLibraries = references.Zip(versions, (pkg, version) => new { pkg, version })
                .ToDictionary(x => x.pkg, x => x.version);
        }
    }
}
