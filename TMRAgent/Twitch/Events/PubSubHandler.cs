﻿using System;
using TMRAgent.Twitch.Utility;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace TMRAgent.Twitch.Events
{
    public class PubSubHandler : IDisposable
    {
        private readonly TwitchPubSub _pubSubClient = new TwitchPubSub();

        public void Start()
        {

            // Validate OAuth Token
            try
            {
                TwitchHandler.Instance.Auth.Validate(Auth.AuthType.PubSub, true);
            }
            catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"[LivestreamMonitorService] Unable to connect to Twitch Livestream Monitoring.  OAuth Validation failed. Error: {ex.Message}", ConsoleUtil.LogLevel.Error, ConsoleColor.Red);
                return;
            }

            _pubSubClient.ListenToBitsEventsV2(ConfigurationHandler.Instance.Configuration.PubSub.ChannelId!);
            _pubSubClient.ListenToChannelPoints(ConfigurationHandler.Instance.Configuration.PubSub.ChannelId!);
            _pubSubClient.ListenToVideoPlayback(ConfigurationHandler.Instance.Configuration.PubSub.ChannelId!);

            _pubSubClient.OnPubSubServiceConnected += (sender, args) =>
            {
                ConsoleUtil.WriteToConsole($"[Twitch-PubSub] Successfully Connected to Public Subscriptions",
                    ConsoleUtil.LogLevel.Info);

                _pubSubClient?.SendTopics(ConfigurationHandler.Instance.Configuration.PubSub.AuthToken!, true);
                _pubSubClient?.SendTopics(ConfigurationHandler.Instance.Configuration.PubSub.AuthToken!);
            };

            _pubSubClient.OnBitsReceivedV2 += PubSubClient_OnBitsReceivedV2!;
            _pubSubClient.OnChannelPointsRewardRedeemed += PubSubClient_OnChannelPointsRewardRedeemed!;

            _pubSubClient.OnPubSubServiceError += PubSubClientOnOnPubSubServiceError;
            _pubSubClient.OnPubSubServiceClosed += PubSubClientOnOnPubSubServiceClosed;
            _pubSubClient.OnPubSubServiceConnected += PubSubClientOnOnPubSubServiceConnected;

            _pubSubClient.OnStreamDown += PubSubClient_OnStreamDown!;
            _pubSubClient.OnStreamUp += PubSubClient_OnStreamUp!;

            _pubSubClient.OnListenResponse += PubSubClient_OnListenResponse!;

            _pubSubClient.Connect();
        }

        private void PubSubClientOnOnPubSubServiceConnected(object? sender, EventArgs e)
        {
            ConsoleUtil.WriteToConsole($"[PubSubClientOnOnPubSubServiceConnected] State: Started", ConsoleUtil.LogLevel.Info);
        }

        private void PubSubClientOnOnPubSubServiceClosed(object? sender, EventArgs e)
        {
            ConsoleUtil.WriteToConsole($"[PubSubClientOnOnPubSubServiceClosed] State: Stopped", ConsoleUtil.LogLevel.Info);
            if (!Program.ExitRequested)
            {
                _pubSubClient.Disconnect();
                _pubSubClient.Connect();
            }
        }

        private void PubSubClientOnOnPubSubServiceError(object? sender, OnPubSubServiceErrorArgs e)
        {
            ConsoleUtil.WriteToConsole($"[PubSubClientOnOnPubSubServiceError] Error: {e.Exception}", ConsoleUtil.LogLevel.Error, ConsoleColor.Red);
        }

        private void PubSubClient_OnStreamUp(object sender, OnStreamUpArgs e)
        {
            ConsoleUtil.WriteToConsole($"Stream {e.ChannelId} PubSub Event: StreamUp", ConsoleUtil.LogLevel.Info, ConsoleColor.Green);
            MySQL.MySqlHandler.Instance.Streams.ProcessStreamOnline(DateTime.Parse(e.ServerTime));
        }

        private void PubSubClient_OnStreamDown(object sender, OnStreamDownArgs e)
        {
            ConsoleUtil.WriteToConsole($"Stream {e.ChannelId} PubSub Event: StreamDown", ConsoleUtil.LogLevel.Info, ConsoleColor.Green);
            MySQL.MySqlHandler.Instance.Streams.ProcessStreamOffline(DateTime.Now.ToUniversalTime(), null);
        }

        private void PubSubClient_OnListenResponse(object sender, OnListenResponseArgs e)
        {
            if (!e.Successful)
                ConsoleUtil.WriteToConsole($"Failed to listen! Response: {e.Response.Error}", ConsoleUtil.LogLevel.Error);
            else
                ConsoleUtil.WriteToConsole($"Successfully hooked {e.Topic}!", ConsoleUtil.LogLevel.Info);
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
                ConsoleUtil.WriteToConsole($"[Error] PubSubClient_OnChannelPointsRewardRedeemed -> {ex.Message}", ConsoleUtil.LogLevel.Error, ConsoleColor.Red);
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
