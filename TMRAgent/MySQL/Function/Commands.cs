using System;
using LinqToDB;

namespace TMRAgent.MySQL.Function
{
    internal class Commands 
    {
        public void ProcessCommandMessage(string Username, int TwitchUserId, bool IsModerator, string Message)
        {
            int userId;
            try
            {
                userId = MySqlHandler.Instance.Users.GetUserId(Username, TwitchUserId, IsModerator);
            }
            catch (Exception ex)
            {
                Util.Log($"Exception:\r\n{ex.Message}", Util.LogLevel.Error);
                return;
            }

            try
            {
                var cmdSplit = Message.Split(" ", 2);
                using (var db = new MySQL.DBConnection.Database())
                {
                    db.Commands
                        .Value(p => p.UserId, userId)
                        .Value(p => p.Command, cmdSplit[0])
                        .Value(p => p.Parameters, cmdSplit.Length == 2 ? cmdSplit[1] : null)
                        .Value(p => p.Date, DateTime.Now.ToUniversalTime())
                        .Insert();
                }
            }
            catch (Exception ex)
            {
                Util.Log($"Exception:\r\n{ex.Message}", Util.LogLevel.Error);
            }
        }
    }
}
