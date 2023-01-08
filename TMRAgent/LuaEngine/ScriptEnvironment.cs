using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLua;
using TMRAgent.LuaEngine.Models;

namespace TMRAgent.LuaEngine
{
    internal class ScriptEnvironment : IDisposable, ITwitchHandler
    {
        public string Name { get; private set; }

        public EventManager EventManager { get; private set; }
        public Lua LuaRunner { get; private set; }

        private readonly string _filePath;
        private bool _alreadyLoaded;

        public ScriptEnvironment(string filePath)
        {
            Name = Path.GetFileNameWithoutExtension(filePath);
            _filePath = filePath;

            EventManager = new EventManager();
            LuaRunner = new Lua();
        }

        public void Load()
        {
            if (_alreadyLoaded) return;

            string scriptFile;
            try
            {
                scriptFile = File.ReadAllText(_filePath);
            }
            catch (Exception ex)
            {
                Util.Log($"Unable to load file {_filePath}: {ex.Message}", Util.LogLevel.Error);
                return;
            }

            //LuaRunner.LoadCLRPackage();
            LuaRunner["event"] = EventManager;
            LuaRunner["twitch"] = new Models.Twitch();
            LuaRunner["logger"] = new Logger();

            try
            {
                LuaRunner.DoString(scriptFile);

                // If we have no event registers from Lua itself, we can safely dispose of this script in memory.
                if ( !EventManager.HasRegisteredFunctions() ) Dispose();
            }
            catch (Exception ex)
            {
                Util.Log(ex.Message, Util.LogLevel.Error);
            }

            _alreadyLoaded = true;
        }

        public void Dispose()
        {
            LuaRunner?.Dispose();
        }

        public void OnTwitchChatMessage(string message)
        {

        }

        public void Bits()
        {
            
        }
    }
}
