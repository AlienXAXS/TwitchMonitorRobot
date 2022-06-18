using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;

namespace TMRAgent.Twitch
{
    internal class TwitchLiveMonitor : IDisposable
    {
        public static TwitchLiveMonitor Instance = _instance ??= new TwitchLiveMonitor();
        private static readonly TwitchLiveMonitor? _instance;

        public TwitchAPI twitchApi;
        public LiveStreamMonitorService liveStreamMonitorService;

        public int CurrentLiveStreamId;

        private readonly ManualResetEvent _quitAppEvent = new ManualResetEvent(false);

        public void Start()
        {
            Task.Run(() => StartAsyncMonitor());
        }

        public async void StartAsyncMonitor()
        {
            twitchApi = new TwitchAPI();
            twitchApi.Settings.AccessToken = ConfigurationHandler.Instance.Configuration.AuthToken;
            twitchApi.Settings.ClientId = ConfigurationHandler.Instance.Configuration.AppClientId;

            liveStreamMonitorService = new LiveStreamMonitorService(twitchApi, 10);
            liveStreamMonitorService.SetChannelsByName(new List<string>() { ConfigurationHandler.Instance.Configuration.ChannelName });

            liveStreamMonitorService.OnStreamOnline += LiveStreamMonitorService_OnStreamOnline;
            liveStreamMonitorService.OnStreamOffline += LiveStreamMonitorService_OnStreamOffline;

            liveStreamMonitorService.OnStreamUpdate += LiveStreamMonitorService_OnStreamUpdate;

            liveStreamMonitorService.Start();

            _quitAppEvent.WaitOne();
        }

        private void LiveStreamMonitorService_OnStreamUpdate(object sender, OnStreamUpdateArgs e)
        {
            
        }

        private void LiveStreamMonitorService_OnStreamOffline(object sender, OnStreamOfflineArgs e)
        {
            MySQL.MySQLHandler.Instance.ProcessStreamOffline(DateTime.Now.ToUniversalTime(), e.Stream.ViewerCount);
            CurrentLiveStreamId = -1;
        }

        private void LiveStreamMonitorService_OnStreamOnline(object sender, OnStreamOnlineArgs e)
        {
            MySQL.MySQLHandler.Instance.ProcessStreamOnline(e.Stream.StartedAt);
        }

        public void Dispose()
        {
            liveStreamMonitorService.Stop();
            _quitAppEvent.Set();
        }
    }
}
