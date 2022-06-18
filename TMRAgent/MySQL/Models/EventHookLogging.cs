using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMRAgent.MySQL.Models
{
    [Table(Name = "eventhooklogging")]
    public class EventHookLogging
    {

        [PrimaryKey, Identity, Column(Name = "id")]
        public int Id { get; set; }

        [Column(Name = "name")]
        public string Name { get; set; }

        [Column(Name = "additionalinfo")]
        public string AdditionalInformation { get; set; }

        [Column(Name = "date")]
        public DateTime Date { get; set; }

    }
}
