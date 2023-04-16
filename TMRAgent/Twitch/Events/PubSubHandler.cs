using System;
using TMRAgent.Twitch.Utility;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace TMRAgent.Twitch.Events
{
    public class PubSubHandler : IDisposable
    {
        private TwitchPubSub? _pubSubClient;

        public void Start()
        {
            // Validate OAuth Token
            try
            {
                TwitchHandler.Instance.Auth.Validate(Auth.AuthType.PubSub, true);
            }
            catch (Exception ex)
            {
                Util.Log($"[LivestreamMonitorService] Unable to connect to Twitch Livestream Monitoring.  OAuth Validation failed. Error: {ex.Message}", Util.LogLevel.Error, ConsoleColor.Red);
                return;
            }

            _pubSubClient = new TwitchPubSub();

            _pubSubClient.ListenToBitsEventsV2(ConfigurationHandler.Instance.Configuration.PubSub.ChannelId!);
            _pubSubClient.ListenToChannelPoints(ConfigurationHandler.Instance.Configuration.PubSub.ChannelId!);
            _pubSubClient.ListenToVideoPlayback(ConfigurationHandler.Instance.Configuration.PubSub.ChannelId!);

            _pubSubClient.OnPubSubServiceConnected += (sender, args) =>
            {
                _pubSubClient?.SendTopics(ConfigurationHandler.Instance.Configuration.PubSub.AuthToken);
            };

            _pubSubClient.OnBitsReceivedV2 += PubSubClient_OnBitsReceivedV2!;
            _pubSubClient.OnChannelPointsRewardRedeemed += PubSubClient_OnChannelPointsRewardRedeemed!;

            _pubSubClient.OnPubSubServiceError += OnOnPubSubServiceError;
            _pubSubClient.OnPubSubServiceClosed += OnOnPubSubServiceClosed;
            _pubSubClient.OnPubSubServiceConnected += OnOnPubSubServiceConnected;

            _pubSubClient.OnStreamDown += PubSubClient_OnStreamDown!;
            _pubSubClient.OnStreamUp += PubSubClient_OnStreamUp!;

            _pubSubClient.OnListenResponse += PubSubClient_OnListenResponse!;

            _pubSubClient.OnLog += (sender, args) => MySQL.MySqlHandler.Instance.Streams.CheckForStreamUpdate();

            _pubSubClient.Connect();
        }

        private void OnOnPubSubServiceConnected(object? sender, EventArgs e)
        {
            Util.Log($"[OnPubSubServiceConnected] State: PubSub Service Websocket Connected", Util.LogLevel.Info);
        }

        private void OnOnPubSubServiceClosed(object? sender, EventArgs e)
        {
            Util.Log($"[OnPubSubServiceClosed] State: Stopped", Util.LogLevel.Info);
            if (!Program.ExitRequested)
            {
                try
                {
                    TwitchHandler.Instance.Auth.Validate(Auth.AuthType.PubSub, true);
                }
                catch (Exception ex)
                {
                    Util.Log(
                        $"[LivestreamMonitorService] Unable to connect to Twitch Livestream Monitoring.  OAuth Validation failed. Error: {ex.Message}",
                        Util.LogLevel.Error, ConsoleColor.Red);
                    return;
                }

                _pubSubClient?.Connect();
            }
            else
            {
                _pubSubClient?.SendTopics(ConfigurationHandler.Instance.Configuration.PubSub.AuthToken!, true);
            }
        }

        private void OnOnPubSubServiceError(object? sender, OnPubSubServiceErrorArgs e)
        {
            if (e.Exception.Message.Equals("The operation was canceled.")) return;
            Util.Log($"[OnPubSubServiceError] Error: {e.Exception}", Util.LogLevel.Error, ConsoleColor.Red);
        }

        private void PubSubClient_OnStreamUp(object sender, OnStreamUpArgs e)
        {
            Util.Log($"Stream {e.ChannelId} PubSub Event: StreamUp on ChannelID: {e.ChannelId} @ {e.ServerTime} (Local Time: {DateTime.Now.ToUniversalTime()})", Util.LogLevel.Info, ConsoleColor.Green);
            MySQL.MySqlHandler.Instance.Streams.ProcessStreamOnline(DateTime.Now.ToUniversalTime());
        }

        private void PubSubClient_OnStreamDown(object sender, OnStreamDownArgs e)
        {
            Util.Log($"Stream {e.ChannelId} PubSub Event: StreamDown", Util.LogLevel.Info, ConsoleColor.Green);
            MySQL.MySqlHandler.Instance.Streams.ProcessStreamOffline(DateTime.Now.ToUniversalTime(), null);
        }

        private void PubSubClient_OnListenResponse(object sender, OnListenResponseArgs e)
        {
            if (!e.Successful)
                Util.Log($"Failed to listen! Response: {e.Response.Error}", Util.LogLevel.Error);
            else
                Util.Log($"Successfully hooked {e.Topic}!", Util.LogLevel.Info);
        }

        private void PubSubClient_OnChannelPointsRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs e)
        {
            try
            {
                MySQL.MySqlHandler.Instance.Bits.ProcessBitsRedeem(
                    e.RewardRedeemed.Redemption.User.DisplayName,
                    int.Parse(e.RewardRedeemed.Redemption.User.Id),
                    e.RewardRedeemed.Redemption.Reward.Title,
                    e.RewardRedeemed.Redemption.Reward.Cost);
            }
            catch (Exception ex)
            {
                Util.Log($"[Error] PubSubClient_OnChannelPointsRewardRedeemed -> {ex.Message}", Util.LogLevel.Error, ConsoleColor.Red);
            }
        }

        private void PubSubClient_OnBitsReceivedV2(object? sender, OnBitsReceivedV2Args e)
        {
            //TODO: Finish this part lulz.
        }

        public void Dispose()
        {
            _pubSubClient?.Disconnect();
        }
    }
}
