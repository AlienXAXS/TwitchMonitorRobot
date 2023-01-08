using LinqToDB.Mapping;
using System;

namespace TMRAgent.MySQL.Models
{
    [Table(Name = "scripts")]
    public class Scripts
    {
        [PrimaryKey, Identity, Column(Name = "id")]
        public int Id { get; set; }

        [Column(Name = "contents"), NotNull]
        public string Contents { get; set; }

        [Column(Name = "description"), NotNull]
        public string Description { get; set; }

        [Column(Name = "state"), NotNull]
        public int State { get; set; }

        [Column(Name = "date"), NotNull]
        public DateTime Date { get; set; }
    }
}