using LinqToDB.Mapping;

namespace TMRAgent.MySQL.Models
{
    [Table(Name = "mod_commands")]
    public class ModCommands
    {
        [PrimaryKey, Identity, Column(Name = "id")]
        public int Id { get; set; }

        [Column(Name = "command")]
        public string Command { get; set; }
    }
}
