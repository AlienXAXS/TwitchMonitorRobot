using LinqToDB.Mapping;
using System;

namespace TMRAgent.MySQL.Models
{
    [Table(Name = "subscriptions")]
    public class Subscriptions
    {
        [PrimaryKey, Identity, Column(Name = "id")]
        public int Id { get; set; }

        [Column(Name = "userid")]
        public int UserId { get; set; }

        [Column(Name = "date")]
        public DateTime Date { get; set; }

        [Column(Name = "isrenew")]
        public bool IsRenew { get; set; }

        [Column(Name = "isprime")]
        public bool IsPrime { get; set; }

        [Column(Name = "isgift")]
        public bool IsGift { get; set; }    

        [Column(Name = "giftfromid")]
        public int GiftFromUserId { get; set; }
    }
}
