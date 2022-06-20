﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB;

namespace TMRAgent.MySQL.Function
{
    internal class Messages 
    {
        public void ProcessChatMessage(string Username, int TwitchUserId, bool IsModerator, string Message)
        {
            int userId;
            try
            {
                userId = MySQLHandler.Instance.Users.GetUserId(Username, TwitchUserId, IsModerator);
            }
            catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"Exception:\r\n{ex.Message}", ConsoleUtil.LogLevel.ERROR);
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
                        .Insert();
                }
            }
            catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"Exception:\r\n{ex.Message}", ConsoleUtil.LogLevel.ERROR);
            }
        }
    }
}
