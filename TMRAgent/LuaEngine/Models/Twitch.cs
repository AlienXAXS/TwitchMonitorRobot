namespace TMRAgent.LuaEngine.Models
{
    public class Twitch
    {
        public void SendChatMessage(string channel, string message)
        {
            TMRAgent.Twitch.TwitchHandler.Instance.ChatService.SendMessage(channel, message);
        }
    }
}
