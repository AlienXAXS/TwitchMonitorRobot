using System;
using LinqToDB.Mapping;

namespace TMRAgent.MySQL.Models
{
    [Table(Name = "commands")]
    public class Commands
    {
        [PrimaryKey, Identity, Column(Name = "id")]
        public int Id { get; set; }

        [Column(Name = "userid"), NotNull]
        public int UserId { get; set; }

        [Column(Name = "command"), NotNull]
        public string Command { get; set; }

        [Column(Name = "parameters"), Nullable]
        public string Parameters { get; set; }

        [Column(Name = "date"), NotNull]
        public DateTime Date { get; set; }
    }
}
