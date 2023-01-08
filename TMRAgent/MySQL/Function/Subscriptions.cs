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
                int? giftUserDbIndex = new int();

                if (GiftUserId != "")
                {
                    var intGiftUserId = int.Parse(GiftUserId);
                    giftUserDbIndex = MySqlHandler.Instance.Users.GetUserId(GiftUserName,intGiftUserId);
                }

                var userIdDbIndex = MySqlHandler.Instance.Users.GetUserId(Username, intUserId);

                using ( var db = new DBConnection.Database())
                {
                    var dbInsert = db.Subscriptions
                        .Value(p => p.UserId, userIdDbIndex)
                        .Value(p => p.Date, DateTime.Now.ToUniversalTime())
                        .Value(p => p.IsRenew, IsRenew)
                        .Value(p => p.IsPrime, IsPrime)
                        .Value(p => p.IsGift, IsGift);

                    if ( IsGift && giftUserDbIndex.Value > 0 )
                    {
                        dbInsert = dbInsert.Value(p => p.GiftFromUserId, () => giftUserDbIndex.Value);
                    }

                    dbInsert.Insert();
                }

            } catch (Exception ex)
            {
                Util.Log($"Fatal Error during ProcessSubscription[{Username}, {UserId}, {IsRenew}, {IsPrime}, {IsGift}, {GiftUserId}] -> {ex.Message}\r\n\r\n{ex.StackTrace}", Util.LogLevel.Fatal, ConsoleColor.Red);
            }
        }

    }
}
