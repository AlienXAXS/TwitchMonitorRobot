#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TwitchLib.Api;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace TMRAgent.Twitch.Events
{
    public class LivestreamMonitorService : IDisposable
    {
        public static LivestreamMonitorService Instance = _instance ??= new LivestreamMonitorService();
        // ReSharper disable once InconsistentNaming
        private static readonly LivestreamMonitorService? _instance;

        public TwitchAPI? TwitchApi;
        
        public LiveStreamMonitorService? LiveStreamMonitorService;

        public int CurrentLiveStreamId = -1;
        public DateTime LastUpdateTime;

        public void Start()
        {
            Task.Run(StartAsyncMonitor);
        }

        public void StartAsyncMonitor()
        {
            TwitchApi = new TwitchAPI
            {
                Settings =
                {
                    AccessToken = ConfigurationHandler.Instance.Configuration.TwitchChat.AuthToken,
                    ClientId = ConfigurationHandler.Instance.Configuration.AppClientId
                }
            };

            if (ConfigurationHandler.Instance.Configuration.TwitchChat.ChannelName == null) return;

            LiveStreamMonitorService = new LiveStreamMonitorService(TwitchApi, 10);
            LiveStreamMonitorService.SetChannelsByName(new List<string>() { ConfigurationHandler.Instance.Configuration.TwitchChat.ChannelName });

            LiveStreamMonitorService.OnStreamOnline += LiveStreamMonitorService_OnStreamOnline;
            LiveStreamMonitorService.OnStreamOffline += LiveStreamMonitorService_OnStreamOffline;
            LiveStreamMonitorService.OnStreamUpdate += LiveStreamMonitorService_OnStreamUpdate;
            LiveStreamMonitorService.OnServiceStopped += LiveStreamMonitorServiceOnOnServiceStopped;
            LiveStreamMonitorService.OnServiceStarted += LiveStreamMonitorServiceOnOnServiceStarted;

            LiveStreamMonitorService.Start();
        }

        private void LiveStreamMonitorServiceOnOnServiceStarted(object? sender, OnServiceStartedArgs e)
        {
            ConsoleUtil.WriteToConsole("[LiveStreamMonitorServiceOnOnServiceStarted] State: Started", ConsoleUtil.LogLevel.Info);
        }

        private void LiveStreamMonitorServiceOnOnServiceStopped(object? sender, OnServiceStoppedArgs e)
        {
            ConsoleUtil.WriteToConsole("[LiveStreamMonitorServiceOnOnServiceStopped] State: Stopped", ConsoleUtil.LogLevel.Info);
        }

        private void LiveStreamMonitorService_OnStreamUpdate(object? sender, OnStreamUpdateArgs e)
        {
            MySQL.MySqlHandler.Instance.Streams.CheckForStreamUpdate();
        }

        private void LiveStreamMonitorService_OnStreamOffline(object? sender, OnStreamOfflineArgs e)
        {
            ConsoleUtil.WriteToConsole("[StreamEvent] Stream is now marked as Offline, uploading stats to Database.", ConsoleUtil.LogLevel.Info, ConsoleColor.Yellow);
            MySQL.MySqlHandler.Instance.Streams.ProcessStreamOffline(DateTime.Now.ToUniversalTime(), e.Stream.ViewerCount);
            CurrentLiveStreamId = -1;
            TwitchHandler.Instance.ChatHandler.ProcessStreamOffline();
        }

        private void LiveStreamMonitorService_OnStreamOnline(object? sender, OnStreamOnlineArgs e)
        {
            ConsoleUtil.WriteToConsole("[StreamEvent] Stream is now marked as Online, creating new Database entry.", ConsoleUtil.LogLevel.Info, ConsoleColor.Yellow);
            MySQL.MySqlHandler.Instance.Streams.ProcessStreamOnline(e.Stream.StartedAt);
        }


        public void Dispose()
        {
            LiveStreamMonitorService?.Stop();
        }
    }
}
