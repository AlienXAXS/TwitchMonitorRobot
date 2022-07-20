using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace TMRAgent.Twitch.Events
{
    public class PubSubHandler : IDisposable
    {
        private TwitchPubSub? _pubSubClient;

        public void StartPubSub()
        {
            _pubSubClient = new TwitchPubSub();
            _pubSubClient.ListenToBitsEventsV2(ConfigurationHandler.Instance.Configuration.PubSub.ChannelId);
            _pubSubClient.ListenToChannelPoints(ConfigurationHandler.Instance.Configuration.PubSub.ChannelId);

            _pubSubClient.OnPubSubServiceConnected += PubSubClient_OnPubSubServiceConnected!;
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
        }

        private void PubSubClientOnOnPubSubServiceError(object? sender, OnPubSubServiceErrorArgs e)
        {
            ConsoleUtil.WriteToConsole($"[PubSubClientOnOnPubSubServiceError] Error: {e.Exception}", ConsoleUtil.LogLevel.Error, ConsoleColor.Red);
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
            }
            catch (Exception ex)
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

        public void Dispose()
        {
            _pubSubClient?.Disconnect();
        }
    }
}
