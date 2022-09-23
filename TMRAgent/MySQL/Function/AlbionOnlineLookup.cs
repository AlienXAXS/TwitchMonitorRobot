using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMRAgent.MySQL.Function
{
    internal class AlbionOnlineLookup
    {
        public string? GetAlbionUserByTwitchId(int TwitchId)
        {
            using (var db = new DBConnection.Database())
            {
                var result = db.AlbionOnlineLookups.DefaultIfEmpty(null).FirstOrDefault(x => x.TwitchId.Equals(TwitchId));
                return result?.AlbionName;
            }
        }
    }
}
