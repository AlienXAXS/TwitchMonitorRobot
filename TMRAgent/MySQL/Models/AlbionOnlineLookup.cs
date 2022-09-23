using LinqToDB.Mapping;

namespace TMRAgent.MySQL.Models
{
    [Table(Name = "albion_online_lookup")]
    public class AlbionOnlineLookup
    {
        [PrimaryKey, Identity, Column(Name = "id")]
        public int Id { get; set; }

        [Column(Name = "albionname")]
        public string AlbionName { get; set; }

        [Column(Name = "twitchid")]
        public int TwitchId { get; set; }
    }
}
