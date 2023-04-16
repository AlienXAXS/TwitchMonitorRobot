using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TMRAgent.LuaEngine
{
    internal class LuaHandler : ITwitchHandler
    {
        private readonly List<ScriptEnvironment> _scriptEnvironments = new List<ScriptEnvironment>();

        public static LuaHandler Instance = _instance ?? (_instance = new LuaHandler());
        private static readonly LuaHandler _instance;

        private const string _scriptPath = @"./Scripts";

        private readonly FileSystemWatcher _fileSystemWatcher = new FileSystemWatcher(_scriptPath);

        public LuaHandler()
        {
            _fileSystemWatcher.Changed += FileSystemWatcherOnChanged;
            _fileSystemWatcher.Renamed += FileSystemWatcherOnRenamed;
            _fileSystemWatcher.Deleted += FileSystemWatcherOnDeleted;
            _fileSystemWatcher.Created += FileSystemWatcherOnChanged;

            _fileSystemWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite;

            _fileSystemWatcher.EnableRaisingEvents = true;
        }

        private void FileSystemWatcherOnDeleted(object sender, FileSystemEventArgs e)
        {
            if (e.Name == null) return;
            var script = GetScriptEnvironmentByName(e.Name);
            UnloadScript(script);
        }

        private void FileSystemWatcherOnRenamed(object sender, RenamedEventArgs e)
        {
            if (e.OldName == null) return;
            var script = GetScriptEnvironmentByName(e.OldName);
            UnloadScript(script);

            if (e.Name == null)
            {
                Util.Log($"Could not reload script with old file name of {e.OldName}. No new file name found.", Util.LogLevel.Error);
                return;
            }

            LoadScript(e.FullPath);
        }

        private void FileSystemWatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine(e.ChangeType);

            LoadScript(e.FullPath);
        }

        private ScriptEnvironment GetScriptEnvironmentByName(string name)
        {
            var scriptName = Path.GetFileNameWithoutExtension(name);
            var script = _scriptEnvironments.FirstOrDefault(x => x.Name == scriptName);

            return script;
        }

        public void DiscoverAndRunScripts()
        {
            var files = Directory.EnumerateFiles(_scriptPath, "*.lua");
            foreach (var file in files)
            {
                LoadScript(file);
            }
        }

        public void LoadScript(string filePath)
        {
            Util.Log($"Attempting to load {Path.GetFileNameWithoutExtension(filePath)}", Util.LogLevel.Lua);

            var existingScript = _scriptEnvironments
                .FirstOrDefault(x => x.Name.Equals(Path.GetFileNameWithoutExtension(filePath)));

            if (existingScript != null) return;
            //UnloadScript(existingScript);

            var scriptEnvironment = new ScriptEnvironment(filePath);
            _scriptEnvironments.Add(scriptEnvironment);
            scriptEnvironment.Load();
        }

        private void UnloadScript(ScriptEnvironment? existingScript)
        {
            if (existingScript == null) return;

            Util.Log($"Attempting to unload script {existingScript.Name}", Util.LogLevel.Lua);

            _scriptEnvironments.Remove(existingScript);
            existingScript.Dispose();
        }


        public void OnTwitchChatMessage(string message)
        {
            _scriptEnvironments.ForEach(x => x.OnTwitchChatMessage(message));
        }

        public void Bits()
        {
            
        }
    }
}



/*
EventManager.OnChatMessage("mind1", new ChatMessage("botname", "userid", "alienx", 
    "alienx", "", new Color(), new EmoteSet(new Emote[]{}, ""),"message", 
    UserType.Admin, "mind1", "id", true, 0, "12312", 
    true, true, true, false, true, false, false, 
    Noisy.False, "???", ":D", new List<KeyValuePair<string, string>>(), 
    new CheerBadge(400), 500, 500
    ));
*/