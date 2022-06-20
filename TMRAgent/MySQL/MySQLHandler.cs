using System;

namespace TMRAgent.MySQL
{
    internal class MySQLHandler
    {
        public static MySQLHandler Instance = _instance ??= new MySQLHandler();
        private static readonly MySQLHandler? _instance;

        public Function.Bits Bits = new Function.Bits();
        public Function.Messages Messages = new Function.Messages();
        public Function.Streams Streams = new Function.Streams();
        public Function.Users Users = new Function.Users();
        public Function.Commands Commands = new Function.Commands();

        public void Connect()
        {
            try
            {
                using ( var db = new DBConnection.Database() )
                {
                    ConsoleUtil.WriteToConsole($"DB Version: {db.Connection.ServerVersion}", ConsoleUtil.LogLevel.INFO);  
                }
            }
            catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"Fatal Error: {ex.Message}\r\n\r\n{ex.StackTrace}", ConsoleUtil.LogLevel.FATAL, ConsoleColor.Red);
            }
        }

    }    
}
