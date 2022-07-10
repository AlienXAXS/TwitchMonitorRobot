using JNogueira.Discord.Webhook.Client;
using System.Threading;

namespace TMRAgent.Discord
{
    internal static class Handler
    {
        private static DiscordWebhookClient _discordWebhookClient;

        public static void SendWebhookMessage(string msg)
        {
            if ( ConfigurationHandler.Instance.IsEnabled )
            {
                if (_discordWebhookClient == null)
                    _discordWebhookClient = new DiscordWebhookClient(ConfigurationHandler.Instance.Configuration.WebHookURL);

                new Thread((ThreadStart)async delegate
                {
                    await _discordWebhookClient.SendToDiscord(new DiscordMessage(msg));
                }).Start();
            }
        }
    }
}
