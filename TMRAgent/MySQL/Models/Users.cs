using System;
using System.Collections.Generic;
using System.Text;
using LinqToDB.Mapping;

namespace TMRAgent.MySQL.Models
{
    [Table(Name = "users")]
    public class Users
    {
        [PrimaryKey, Identity, Column(Name = "id")]
        public int Id { get; set; }

        [Column(Name = "username"), NotNull]
        public string Username { get; set; }

        [Column(Name = "ismod"), Nullable]
        public bool IsModerator { get; set; }

        [Column(Name = "lastseen"), Nullable]
        public DateTime LastSeen { get; set; }

        [Column(Name = "twitchid"), Nullable]
        public int TwitchId { get; set; }
    }
}
