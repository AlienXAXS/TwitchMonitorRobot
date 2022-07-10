﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;
using TwitchLib.PubSub;

namespace TMRAgent.Twitch
{
    internal class TwitchLiveMonitor : IDisposable
    {
        public static TwitchLiveMonitor Instance = _instance ??= new TwitchLiveMonitor();
        private static readonly TwitchLiveMonitor? _instance;

        public TwitchAPI twitchApi;
        private TwitchPubSub pubSubClient;
        public LiveStreamMonitorService liveStreamMonitorService;

        public int CurrentLiveStreamId = -1;
        public DateTime LastUpdateTime;

        private readonly ManualResetEvent _quitAppEvent = new ManualResetEvent(false);

        public void Start()
        {
            Task.Run(() => StartAsyncMonitor());
            Task.Run(() => StartPubSub());
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

#if RELEASE
            liveStreamMonitorService.Start();
#endif

            _quitAppEvent.WaitOne();
        }

        private void LiveStreamMonitorService_OnStreamUpdate(object sender, OnStreamUpdateArgs e)
        {
            TwitchHandler.Instance.CheckForStreamUpdate();
        }

        public async void StartPubSub()
        {
            pubSubClient = new TwitchPubSub();
            pubSubClient.ListenToBitsEventsV2(ConfigurationHandler.Instance.Configuration.PubSubChannelId);
            pubSubClient.ListenToChannelPoints(ConfigurationHandler.Instance.Configuration.PubSubChannelId);

            pubSubClient.OnPubSubServiceConnected += PubSubClient_OnPubSubServiceConnected;
            pubSubClient.OnBitsReceivedV2 += PubSubClient_OnBitsReceivedV2;
            pubSubClient.OnChannelPointsRewardRedeemed += PubSubClient_OnChannelPointsRewardRedeemed;

            pubSubClient.OnStreamDown += PubSubClient_OnStreamDown;
            pubSubClient.OnStreamUp += PubSubClient_OnStreamUp;

            pubSubClient.OnListenResponse += PubSubClient_OnListenResponse;

#if RELEASE
            pubSubClient.Connect();
#endif

            _quitAppEvent.WaitOne();
        }

        private void PubSubClient_OnStreamUp(object sender, TwitchLib.PubSub.Events.OnStreamUpArgs e)
        {
            ConsoleUtil.WriteToConsole($"Stream {e.ChannelId} PubSub Event: StreamUp", ConsoleUtil.LogLevel.INFO, ConsoleColor.Green);
        }

        private void PubSubClient_OnStreamDown(object sender, TwitchLib.PubSub.Events.OnStreamDownArgs e)
        {
            ConsoleUtil.WriteToConsole($"Stream {e.ChannelId} PubSub Event: StreamDown", ConsoleUtil.LogLevel.INFO, ConsoleColor.Green);
        }

        private void PubSubClient_OnListenResponse(object sender, TwitchLib.PubSub.Events.OnListenResponseArgs e)
        {
            if (!e.Successful)
                ConsoleUtil.WriteToConsole($"Failed to listen! Response: {e.Response.Error}", ConsoleUtil.LogLevel.ERROR);
            else
                ConsoleUtil.WriteToConsole($"Successfully hooked {e.Topic}!", ConsoleUtil.LogLevel.INFO);
        }

        private void PubSubClient_OnChannelPointsRewardRedeemed(object sender, TwitchLib.PubSub.Events.OnChannelPointsRewardRedeemedArgs e)
        {
            try
            {
                MySQL.MySQLHandler.Instance.Bits.ProcessBitsRedeem(
                    e.RewardRedeemed.Redemption.User.DisplayName,
                    int.Parse(e.RewardRedeemed.Redemption.User.Id),
                    e.RewardRedeemed.Redemption.Reward.Title,
                    e.RewardRedeemed.Redemption.Reward.Cost);
            } catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"[Error] PubSubClient_OnChannelPointsRewardRedeemed -> {ex.Message}", ConsoleUtil.LogLevel.ERROR, ConsoleColor.Red);
            }
        }

        private void PubSubClient_OnBitsReceivedV2(object sender, TwitchLib.PubSub.Events.OnBitsReceivedV2Args e)
        {
            //TODO: Finish this part lulz.
        }

        private void PubSubClient_OnPubSubServiceConnected(object sender, EventArgs e)
        {
            ConsoleUtil.WriteToConsole($"[Twitch-PubSub] Successfully Connected to Public Subscriptions", ConsoleUtil.LogLevel.INFO);
            pubSubClient.SendTopics(ConfigurationHandler.Instance.Configuration.PubSubToken);
        }

        private void LiveStreamMonitorService_OnStreamOffline(object sender, OnStreamOfflineArgs e)
        {
            ConsoleUtil.WriteToConsole("[StreamEvent] Stream is now marked as Offline, uploading stats to Database.", ConsoleUtil.LogLevel.INFO, ConsoleColor.Yellow);
            MySQL.MySQLHandler.Instance.Streams.ProcessStreamOffline(DateTime.Now.ToUniversalTime(), e.Stream.ViewerCount);
            CurrentLiveStreamId = -1;
            TwitchHandler.Instance.ProcessStreamOffline();
        }

        private void LiveStreamMonitorService_OnStreamOnline(object sender, OnStreamOnlineArgs e)
        {
            ConsoleUtil.WriteToConsole("[StreamEvent] Stream is now marked as Online, creating new Database entry.", ConsoleUtil.LogLevel.INFO, ConsoleColor.Yellow);
            MySQL.MySQLHandler.Instance.Streams.ProcessStreamOnline(e.Stream.StartedAt);
        }
        public void Dispose()
        {
            liveStreamMonitorService.Stop();
            _quitAppEvent.Set();
        }
    }
}
