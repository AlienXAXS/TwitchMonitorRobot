using LinqToDB.Mapping;
using System;

namespace TMRAgent.MySQL.Models
{
    [Table(Name = "logging")]
    public class Logging
    {
        [PrimaryKey, Identity, Column(Name = "id")]
        public int Id { get; set; }

        [Column(Name = "type"), NotNull]
        public int type { get; set; }

        [Column(Name = "message"), NotNull]
        public string Message { get; set; }

        [Column(Name = "stacktrace"), NotNull]
        public string StackTrace { get; set; }

        [Column(Name = "date"), NotNull]
        public DateTime Date { get; set; }
    }
}
