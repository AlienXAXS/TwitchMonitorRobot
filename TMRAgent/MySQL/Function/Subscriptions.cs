using System;
using LinqToDB;

namespace TMRAgent.MySQL.Function
{
    internal class Subscriptions
    {

        public void ProcessSubscription(string Username, string UserId, bool IsRenew, bool IsPrime = false, bool IsGift = false, string GiftUserName = "", string GiftUserId = "")
        {
            try
            {
                var intUserId = int.Parse(UserId);
                var intGiftUserId = -1;
                int? GiftUserDBIndex = new int();

                if (GiftUserId != "")
                {
                    intGiftUserId = int.Parse(GiftUserId);
                    GiftUserDBIndex = MySQLHandler.Instance.Users.GetUserId(intGiftUserId);
                }

                var UserIdDBIndex = MySQLHandler.Instance.Users.GetUserId(Username, intUserId);

                using ( var db = new DBConnection.Database())
                {
                    var dbInsert = db.Subscriptions
                        .Value(p => p.UserId, UserIdDBIndex)
                        .Value(p => p.Date, DateTime.Now.ToUniversalTime())
                        .Value(p => p.IsRenew, IsRenew)
                        .Value(p => p.IsPrime, IsPrime)
                        .Value(p => p.IsGift, IsGift);

                    if ( IsGift && GiftUserDBIndex.HasValue )
                    {
                        dbInsert.Value(p => p.GiftFromUserId, GiftUserDBIndex.Value);
                    }

                    dbInsert.Insert();
                }

            } catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"Fatal Error during ProcessSubscription[{Username}, {UserId}, {IsRenew}, {IsPrime}, {IsGift}, {GiftUserId}] -> {ex.Message}\r\n\r\n{ex.StackTrace}", ConsoleUtil.LogLevel.FATAL, ConsoleColor.Red);
            }
        }

    }
}
