#nullable enable
using System;
using System.Collections.Generic;
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
        // ReSharper disable once InconsistentNaming
        private static readonly TwitchLiveMonitor? _instance;

        public TwitchAPI? TwitchApi;
        private TwitchPubSub? _pubSubClient;
        public LiveStreamMonitorService? LiveStreamMonitorService;

        public int CurrentLiveStreamId = -1;
        public DateTime LastUpdateTime;

        private readonly ManualResetEvent _quitAppEvent = new ManualResetEvent(false);

        public void Start()
        {
            Task.Run(StartAsyncMonitor);
            Task.Run(StartPubSub);
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

            LiveStreamMonitorService.Start();

            _quitAppEvent.WaitOne();
        }

        private void LiveStreamMonitorServiceOnOnServiceStopped(object? sender, OnServiceStoppedArgs e)
        {
            
        }

        private void LiveStreamMonitorService_OnStreamUpdate(object? sender, OnStreamUpdateArgs e)
        {
            TwitchHandler.Instance.CheckForStreamUpdate();
        }

        public void StartPubSub()
        {
            _pubSubClient = new TwitchPubSub();
            _pubSubClient.ListenToBitsEventsV2(ConfigurationHandler.Instance.Configuration.PubSub.ChannelId);
            _pubSubClient.ListenToChannelPoints(ConfigurationHandler.Instance.Configuration.PubSub.ChannelId);

            _pubSubClient.OnPubSubServiceConnected += PubSubClient_OnPubSubServiceConnected!;
            _pubSubClient.OnBitsReceivedV2 += PubSubClient_OnBitsReceivedV2!;
            _pubSubClient.OnChannelPointsRewardRedeemed += PubSubClient_OnChannelPointsRewardRedeemed!;

            _pubSubClient.OnStreamDown += PubSubClient_OnStreamDown!;
            _pubSubClient.OnStreamUp += PubSubClient_OnStreamUp!;

            _pubSubClient.OnListenResponse += PubSubClient_OnListenResponse!;

            _pubSubClient.Connect();

            _quitAppEvent.WaitOne();
        }

        private void PubSubClient_OnStreamUp(object sender, TwitchLib.PubSub.Events.OnStreamUpArgs e)
        {
            ConsoleUtil.WriteToConsole($"Stream {e.ChannelId} PubSub Event: StreamUp", ConsoleUtil.LogLevel.Info, ConsoleColor.Green);
        }

        private void PubSubClient_OnStreamDown(object sender, TwitchLib.PubSub.Events.OnStreamDownArgs e)
        {
            ConsoleUtil.WriteToConsole($"Stream {e.ChannelId} PubSub Event: StreamDown", ConsoleUtil.LogLevel.Info, ConsoleColor.Green);
        }

        private void PubSubClient_OnListenResponse(object sender, TwitchLib.PubSub.Events.OnListenResponseArgs e)
        {
            if (!e.Successful)
                ConsoleUtil.WriteToConsole($"Failed to listen! Response: {e.Response.Error}", ConsoleUtil.LogLevel.Error);
            else
                ConsoleUtil.WriteToConsole($"Successfully hooked {e.Topic}!", ConsoleUtil.LogLevel.Info);
        }

        private void PubSubClient_OnChannelPointsRewardRedeemed(object sender, TwitchLib.PubSub.Events.OnChannelPointsRewardRedeemedArgs e)
        {
            try
            {
                MySQL.MySqlHandler.Instance.Bits.ProcessBitsRedeem(
                    e.RewardRedeemed.Redemption.User.DisplayName,
                    int.Parse(e.RewardRedeemed.Redemption.User.Id),
                    e.RewardRedeemed.Redemption.Reward.Title,
                    e.RewardRedeemed.Redemption.Reward.Cost);
            } catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"[Error] PubSubClient_OnChannelPointsRewardRedeemed -> {ex.Message}", ConsoleUtil.LogLevel.Error, ConsoleColor.Red);
            }
        }

        private void PubSubClient_OnBitsReceivedV2(object? sender, TwitchLib.PubSub.Events.OnBitsReceivedV2Args e)
        {
            //TODO: Finish this part lulz.
        }

        private void PubSubClient_OnPubSubServiceConnected(object? sender, EventArgs e)
        {
            ConsoleUtil.WriteToConsole($"[Twitch-PubSub] Successfully Connected to Public Subscriptions", ConsoleUtil.LogLevel.Info);
            _pubSubClient?.SendTopics(ConfigurationHandler.Instance.Configuration.PubSub.AuthToken);
        }

        private void LiveStreamMonitorService_OnStreamOffline(object? sender, OnStreamOfflineArgs e)
        {
            ConsoleUtil.WriteToConsole("[StreamEvent] Stream is now marked as Offline, uploading stats to Database.", ConsoleUtil.LogLevel.Info, ConsoleColor.Yellow);
            MySQL.MySqlHandler.Instance.Streams.ProcessStreamOffline(DateTime.Now.ToUniversalTime(), e.Stream.ViewerCount);
            CurrentLiveStreamId = -1;
            TwitchHandler.Instance.ProcessStreamOffline();
        }

        private void LiveStreamMonitorService_OnStreamOnline(object? sender, OnStreamOnlineArgs e)
        {
            ConsoleUtil.WriteToConsole("[StreamEvent] Stream is now marked as Online, creating new Database entry.", ConsoleUtil.LogLevel.Info, ConsoleColor.Yellow);
            MySQL.MySqlHandler.Instance.Streams.ProcessStreamOnline(e.Stream.StartedAt);
        }
        public void Dispose()
        {
            LiveStreamMonitorService?.Stop();
            _quitAppEvent.Set();
        }
    }
}
