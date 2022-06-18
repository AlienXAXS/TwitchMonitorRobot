using System;
using LinqToDB.Mapping;

namespace TMRAgent.MySQL.Models
{
    [Table(Name = "messages")]
    public class Messages
    {
        [PrimaryKey, Identity, Column(Name = "id")]
        public int Id { get; set; }

        [Column(Name = "userid"), NotNull]
        public int UserId { get; set; }

        [Column(Name="date"), NotNull]
        public DateTime Date { get; set; }

        [Column(Name = "message"), NotNull]
        public string Message { get; set; }
    }
}
