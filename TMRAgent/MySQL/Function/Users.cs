using LinqToDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TMRAgent.MySQL.Function
{
    internal class Users
    {
        private readonly Dictionary<string, int> _usernameMemory = new();

        public int GetUserId(string Username, int TwitchUserId, bool isModerator = false)
        {
            if (_usernameMemory.ContainsKey(Username))
            {
                return _usernameMemory[Username];
            }

            var userId = -1;

            try
            {
                using (var db = new DBConnection.Database())
                {
                    // First find a user with that name, but only if they dont have a twitch ID
                    var usrTwitchIdFix = db.Users.DefaultIfEmpty(null).FirstOrDefault(u => u.Username.Equals(Username));

                    if (usrTwitchIdFix != null && usrTwitchIdFix.TwitchId == 0)
                    {
                        // Fix the user
                        Util.Log($"Fixing TwitchID for user {Username} = {TwitchUserId}", Util.LogLevel.Info, ConsoleColor.Yellow);
                        UpdateExistingUser(usrTwitchIdFix.Id, TwitchId: TwitchUserId);
                    }

                    var user = db.Users.Where(u => u.TwitchId == TwitchUserId);
                    if (user.Any())
                    {
                        userId = user.First().Id;
                        _usernameMemory.Add(Username, userId);
                    }
                    else
                    {
                        userId = AddNewUser(Username, TwitchUserId, isModerator);
                        _usernameMemory.Add(Username, userId);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to get User ID in Database for user {Username}\r\n\r\nError: {ex.Message}");
            }

            return userId;
        }

        public int? GetUserByUsername(string username)
        {
            try
            {
                using (var db = new DBConnection.Database())
                {
                    var user = db.Users.DefaultIfEmpty(null)
                        .FirstOrDefault(x => x.Username.ToLower().Equals(username.ToLower()));
                    return user?.Id;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to get User ID in Database for user {username}\r\n\r\nError: {ex.Message}");
            }
        }

        public int? GetUserId(int TwitchUserId)
        {
            int? userId = null;

            try
            {
                using (var db = new DBConnection.Database())
                {
                    var user = db.Users.Where(u => u.TwitchId == TwitchUserId);
                    if (user.Any())
                    {
                        userId = user.First().Id;
                    }
                    else return null;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to get User ID in Database for user {TwitchUserId}\r\n\r\nError: {ex.Message}");
            }

            return userId;
        }

        public int? GetTwitchIdFromDbId(int? DbId)
        {
            if ( DbId == null ) return null;

            int? returnedValue = null;

            var userId = DbId;
            using (var db = new DBConnection.Database())
            {
                var user = db.Users.Where(x => x.Id.Equals(userId));
                if (user.Any())
                {
                    returnedValue = user.First().TwitchId;
                }
            }

            return returnedValue;
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
