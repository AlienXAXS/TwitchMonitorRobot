using System;
using LinqToDB;

namespace TMRAgent.MySQL.Function
{
    internal class Bits
    {
        internal void ProcessBitsRedeem(string Username, int TwitchUserId, string Name, int Cost)
        {
            try
            {
                var dbUserId = MySQL.MySqlHandler.Instance.Users.GetUserId(Username, TwitchUserId);

                using (var db = new DBConnection.Database())
                {
                    db.BitRedeems
                        .Value(p => p.Name, Name)
                        .Value(p => p.UserId, dbUserId)
                        .Value(p => p.Cost, Cost)
                        .Value(p => p.Date, DateTime.Now.ToUniversalTime())
                        .Insert();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to get User ID in Database for user {Username}\r\n\r\nError: {ex.Message}");
            }
        }
    }
}
