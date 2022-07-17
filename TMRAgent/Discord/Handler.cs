using JNogueira.Discord.Webhook.Client;
using System;

namespace TMRAgent.Discord
{
    internal static class Handler
    {
        private static DiscordWebhookClient _discordWebhookClient;

        public static void SendWebhookMessage(string msg)
        {
            try
            {
                if (ConfigurationHandler.Instance.IsEnabled)
                {
                    if (_discordWebhookClient == null)
                        _discordWebhookClient = new DiscordWebhookClient(ConfigurationHandler.Instance.Configuration.WebHookUrl);

#if !DEBUG
                    _discordWebhookClient.SendToDiscord(new DiscordMessage(msg)).Wait(3000);
#endif
                }
            } catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"[DiscordWebHookHandler] {ex.Message}\r\n\r\n{ex.StackTrace}", ConsoleUtil.LogLevel.Error, ConsoleColor.Red);
            }
        }
    }
}
