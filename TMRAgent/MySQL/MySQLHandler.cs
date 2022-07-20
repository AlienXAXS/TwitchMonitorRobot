using System;

namespace TMRAgent.MySQL
{
    internal class MySqlHandler
    {
        public static MySqlHandler Instance = _instance ??= new MySqlHandler();
        // ReSharper disable once InconsistentNaming
        private static readonly MySqlHandler? _instance;

        public Function.Bits Bits = new();
        public Function.Messages Messages = new();
        public Function.Streams Streams = new();
        public Function.Users Users = new();
        public Function.Commands Commands = new();
        public Function.Subscriptions Subscriptions = new();

        public void Connect()
        {
            try
            {
                using ( var db = new DBConnection.Database() )
                {
                    ConsoleUtil.WriteToConsole($"DB Version: {db.Connection.ServerVersion}", ConsoleUtil.LogLevel.Info);  
                }
            }
            catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"Fatal Error: {ex.Message}\r\n\r\n{ex.StackTrace}", ConsoleUtil.LogLevel.Fatal, ConsoleColor.Red);
            }
        }

    }    
}
