using LinqToDB;
using LinqToDB.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMRAgent.MySQL.Function
{
    internal class Users
    {
        private Dictionary<string, int> UsernameMemory = new Dictionary<string, int>();

        public int GetUserId(string Username, int TwitchUserId, bool isModerator = false)
        {
            if (UsernameMemory.ContainsKey(Username))
            {
                return UsernameMemory[Username];
            }

            var userId = 0;

            try
            {
                using (var db = new DBConnection.Database())
                {
                    // First find a user with that name, but only if they dont have a twitch ID
                    var usrTwitchIdFix = db.Users.DefaultIfEmpty(null).FirstOrDefault(u => u.Username.Equals(Username));

                    if (usrTwitchIdFix.TwitchId == 0)
                    {
                        // Fix the user
                        ConsoleUtil.WriteToConsole($"Fixing TwitchID for user {Username} = {TwitchUserId}", ConsoleUtil.LogLevel.INFO, ConsoleColor.Yellow);
                        UpdateExistingUser(usrTwitchIdFix.Id, TwitchId: TwitchUserId);
                    }

                    var user = db.Users.Where(u => u.TwitchId == TwitchUserId);
                    if (user.Any())
                    {
                        userId = user.First().Id;
                        UsernameMemory.Add(Username, userId);
                    }
                    else
                    {
                        userId = AddNewUser(Username, TwitchUserId, isModerator);
                        UsernameMemory.Add(Username, userId);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to get User ID in Database for user {Username}\r\n\r\nError: {ex.Message}");
            }

            return userId;
        }

        public int AddNewUser(string Username, int TwitchUserId, bool isModerator = false)
        {
            try
            {
                using (var db = new DBConnection.Database())
                {
                    var queryId = db.Users
                        .Value(p => p.Username, Username)
                        .Value(p => p.IsModerator, isModerator)
                        .Value(p => p.LastSeen, DateTime.Now.ToUniversalTime())
                        .Value(p => p.TwitchId, TwitchUserId)
                        .InsertWithInt32Identity();

                    return (int)queryId;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to add new user to database for user {Username}\r\n\r\nError: {ex.Message}");
            }
        }

        public void UpdateExistingUser(int UserId, string Username = "", int TwitchId = -1)
        {
            try
            {
                using (var db = new DBConnection.Database())
                {
                    var user = db.Users.Where(x => x.Id == UserId);
                    LinqToDB.Linq.IUpdatable<Models.Users> tmp = null;
                    if (Username != "")
                        tmp = user.Set(x => x.Username, Username);
                    if (TwitchId > -1)
                        tmp = user.Set(x => x.TwitchId, TwitchId);

                    if (tmp != null)
                        tmp.Update();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to update an existing user {Username}\r\n\r\nError: {ex.Message}");
            }
        }
    }
}
