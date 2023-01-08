namespace TMRAgent.LuaEngine;

internal interface ITwitchHandler
{
    void OnTwitchChatMessage(string message);
    void Bits();
}