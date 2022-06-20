using System;
using LinqToDB.Mapping;

namespace TMRAgent.MySQL.Models
{
    [Table(Name = "bit_redeems")]
    public class BitRedeems
    {
        [PrimaryKey, Identity, Column(Name = "id")]
        public int Id { get; set; }

        [Column(Name = "userid")]
        public int UserId { get; set; }

        [Column(Name = "name")]
        public string Name { get; set; }

        [Column(Name = "cost")]
        public int Cost { get; set; }

        [Column(Name = "date")]
        public DateTime Date { get; set; }
    }
}