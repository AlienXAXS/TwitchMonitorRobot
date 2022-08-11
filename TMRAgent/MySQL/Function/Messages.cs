using System;
using LinqToDB;

namespace TMRAgent.MySQL.Function
{
    internal class Messages 
    {
        public void ProcessChatMessage(string Username, int TwitchUserId, bool IsModerator, string Message, int Bits)
        {
            int userId;
            try
            {
                userId = MySqlHandler.Instance.Users.GetUserId(Username, TwitchUserId, IsModerator);
            }
            catch (Exception ex)
            {
                Util.Log($"OAuth Exception:\r\n{ex.Message}\r\n{ex.StackTrace}", Util.LogLevel.Error);
                return;
            }

            try
            {
                using (var db = new MySQL.DBConnection.Database())
                {
                    db.Messages
                        .Value(p => p.UserId, userId)
                        .Value(p => p.Date, DateTime.Now.ToUniversalTime())
                        .Value(p => p.Message, Message)
                        .Value(p => p.Bits, Bits)
                        .Insert();
                }
            }
            catch (Exception ex)
            {
                Util.Log($"Database Exception:\r\n{ex.Message}\r\n{ex.StackTrace}", Util.LogLevel.Error);
            }
        }
    }
}
