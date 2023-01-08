using System;
using System.Collections.Generic;
using System.Linq;
using NLua;

namespace TMRAgent.LuaEngine.Models;

public class EventManager : ITwitchHandler
{
    List<LuaFunction> OnChatMessageFunctions = new List<LuaFunction>();

    public void RegisterChatMessage(LuaFunction x)
    {
        OnChatMessageFunctions.Add(x);
    }

    public bool UnregisterChatMessage(LuaFunction x)
    {
        return OnChatMessageFunctions.Remove(x);
    }

    public void OnChatMessage(string channel, TwitchLib.Client.Models.ChatMessage message)
    {
        foreach (var x in OnChatMessageFunctions.ToArray())
        {
            try
            {
                x.Call(message);
            }
            catch (Exception ex)
            {
                Util.Log(ex.Message, Util.LogLevel.Error);
            }
        }
    }

    public bool HasRegisteredFunctions()
    {
        return OnChatMessageFunctions.Any();
    }

    public void OnTwitchChatMessage(string message)
    {
        
    }

    public void Bits()
    {
        
    }
}