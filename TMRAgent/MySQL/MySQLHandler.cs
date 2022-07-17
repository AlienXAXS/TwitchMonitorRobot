using System;

namespace TMRAgent.MySQL
{
    internal class MySqlHandler
    {
        public static MySqlHandler Instance = _instance ??= new MySqlHandler();
        // ReSharper disable once InconsistentNaming
        private static readonly MySqlHandler? _instance;

        public Function.Bits Bits = new Function.Bits();
        public Function.Messages Messages = new Function.Messages();
        public Function.Streams Streams = new Function.Streams();
        public Function.Users Users = new Function.Users();
        public Function.Commands Commands = new Function.Commands();
        public Function.Subscriptions Subscriptions = new Function.Subscriptions();

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
