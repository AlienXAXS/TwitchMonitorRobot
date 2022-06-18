using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;
using TMRAgent.MySQL.Models;

namespace TMRAgent.MySQL
{
    internal class MySQLHandler : IDisposable
    {
        public static MySQLHandler Instance = _instance ??= new MySQLHandler();
        private static readonly MySQLHandler? _instance;

        private Dictionary<string, int> UsernameMemory = new Dictionary<string, int>();

        public Database dbConnection = new Database();

        public string DatabaseVersion;

        public void Connect()
        {
            try
            {
                DatabaseVersion = dbConnection.Connection.ServerVersion;
                ConsoleUtil.WriteToConsole($"DB Version: {DatabaseVersion}", ConsoleUtil.LogLevel.INFO);
            }
            catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"Fatal Error: {ex.Message}\r\n\r\n{ex.StackTrace}", ConsoleUtil.LogLevel.FATAL, ConsoleColor.Red);
            }
        }

        public int GetUserId(string Username, bool isModerator = false)
        {
            if (UsernameMemory.ContainsKey(Username))
            {
                return UsernameMemory[Username];
            }

            var userId = 0;

            try
            {
                using (var db = new Database())
                {
                    var user = db.Users.Where(u => u.Username == Username);
                    if (user.Any())
                    {
                        userId = user.First().Id;
                        UsernameMemory.Add(Username, userId);
                    }
                    else
                    {
                        userId = AddNewUser(Username, isModerator);
                        UsernameMemory.Add(Username, userId);
                    }
                }
            } catch (Exception ex)
            {
                throw new Exception($"Unable to get User ID in Database for user {Username}\r\n\r\nError: {ex.Message}");
            }

            return userId;
        }

        internal void ProcessStreamOnline(DateTime dateTime)
        {
            try
            {
                using ( var db = new Database() )
                {
                    Twitch.TwitchLiveMonitor.Instance.CurrentLiveStreamId = (int)db.Streams
                        .Value(p => p.Start, dateTime)
                        .InsertWithInt32Identity();
                }
            } catch (Exception ex)
            {
                throw new Exception($"Unable to add current stream as being Online\r\n\r\n{ex.Message}");
            }
        }

        internal void ProcessStreamOffline(DateTime dateTime, int Viewers)
        {
            try
            {
                if ( Twitch.TwitchLiveMonitor.Instance.CurrentLiveStreamId == -1 )
                {
                    throw new Exception($"Current stream does not have a known ID, unable to set it offline!");
                }

                using ( var db = new Database() )
                {
                    db.Streams
                        .Where(p => p.Id == Twitch.TwitchLiveMonitor.Instance.CurrentLiveStreamId)
                        .Set(p => p.End, dateTime)
                        .Set(p => p.Viewers, Viewers)
                        .Update();
                }
            } catch (Exception ex)
            {
                throw new Exception($"Unable to add current stream as being Offline\r\n\r\n{ex.Message}");
            }
        }

        public Int32 AddNewUser(string Username, bool isModerator = false)
        {
            try
            {
                using (var db = new Database())
                {
                    var queryId = db.Users
                        .Value(p => p.Username, Username)
                        .Value(p => p.IsModerator, isModerator)
                        .Value(p => p.LastSeen, DateTime.Now)
                        .InsertWithInt32Identity();

                    return (int)queryId;
                }
            } catch (Exception ex)
            {
                throw new Exception($"Unable to add new user to database for user {Username}\r\n\r\nError: {ex.Message}");
            }
        }

        public void ProcessChatMessage(string Username, bool IsModerator, string Message)
        {
            int userId;
            try
            {
                userId = MySQL.MySQLHandler.Instance.GetUserId(Username, IsModerator);
            } catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"Exception:\r\n{ex.Message}", ConsoleUtil.LogLevel.ERROR);
                return;
            }

            try
            {
                using (var db = new MySQL.Database())
                {
                    db.Messages
                        .Value(p => p.UserId, userId)
                        .Value(p => p.Date, DateTime.Now)
                        .Value(p => p.Message, Message)
                        .Insert();
                }
            } catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"Exception:\r\n{ex.Message}", ConsoleUtil.LogLevel.ERROR);
            }
        }

        public void ProcessCommandMessage(string Username, bool IsModerator, string Message)
        {
            int userId;
            try
            {
                userId = MySQL.MySQLHandler.Instance.GetUserId(Username, IsModerator);
            } catch(Exception ex)
            {
                ConsoleUtil.WriteToConsole($"Exception:\r\n{ex.Message}", ConsoleUtil.LogLevel.ERROR);
                return;
            }

            try
            {
                var cmdSplit = Message.Split(" ", 2);
                using (var db = new MySQL.Database())
                {
                    db.Commands
                        .Value(p => p.UserId, userId)
                        .Value(p => p.Command, cmdSplit[0])
                        .Value(p => p.Parameters, cmdSplit.Length == 2 ? cmdSplit[1] : null)
                        .Value(p => p.Date, DateTime.Now)
                        .Insert();
                }
            } catch(Exception ex)
            {
                ConsoleUtil.WriteToConsole($"Exception:\r\n{ex.Message}", ConsoleUtil.LogLevel.ERROR);
            }
        }

        public void Dispose()
        {
            dbConnection?.Dispose();
        }
    }


    public class Database : LinqToDB.Data.DataConnection
    {
        public Database() : base("TMRAgent") { }

        public ITable<Models.Commands> Commands => this.GetTable<Models.Commands>();
        public ITable<Models.Messages> Messages => this.GetTable<Models.Messages>();
        public ITable<Models.Users> Users => this.GetTable<Models.Users>();
        public ITable<Models.EventHookLogging> EventHookLogging => this.GetTable<Models.EventHookLogging>();
        public ITable<Models.Streams> Streams => this.GetTable<Models.Streams>();
    }

    public class ConnectionStringSettings : IConnectionStringSettings
    {
        public string ConnectionString { get; set; }
        public string Name { get; set; }
        public string ProviderName { get; set; }
        public bool IsGlobal => false;
    }

    public class MySettings : ILinqToDBSettings
    {
        public IEnumerable<IDataProviderSettings> DataProviders
            => Enumerable.Empty<IDataProviderSettings>();

        public string DefaultConfiguration => "MySQL";
        public string DefaultDataProvider => "MySQL";

        public IEnumerable<IConnectionStringSettings> ConnectionStrings
        {
            get
            {
                yield return
                    new ConnectionStringSettings
                    {
                        Name = "TMRAgent",
                        ProviderName = ProviderName.MySql,
                        ConnectionString = ConfigurationHandler.Instance.Configuration.ConnectionString
                    };
            }
        }
    }

    
}
