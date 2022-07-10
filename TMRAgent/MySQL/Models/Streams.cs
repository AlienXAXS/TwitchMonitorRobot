using System;
using LinqToDB.Mapping;

namespace TMRAgent.MySQL.Models
{
    [Table(Name = "streams")]
    public class Streams
    {
        [PrimaryKey, Identity, Column(Name = "id")]
        public int Id { get; set; }

        [Column(Name = "start")]
        public DateTime Start { get; set; }

        [Column(Name = "end"), Nullable]
        public DateTime End { get; set; }

        [Column(Name = "lastseen"), Nullable]
        public DateTime LastSeen { get; set; }

        [Column(Name = "viewers"), Nullable]
        public int Viewers { get; set; }
    }
}
