using LinqToDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMRAgent.MySQL.DBConnection
{
    public class Database : LinqToDB.Data.DataConnection
    {
        public Database() : base("TMRAgent") { }

        public ITable<Models.Commands> Commands => this.GetTable<Models.Commands>();
        public ITable<Models.Messages> Messages => this.GetTable<Models.Messages>();
        public ITable<Models.Users> Users => this.GetTable<Models.Users>();
        public ITable<Models.EventHookLogging> EventHookLogging => this.GetTable<Models.EventHookLogging>();
        public ITable<Models.Streams> Streams => this.GetTable<Models.Streams>();
        public ITable<Models.BitRedeems> BitRedeems => this.GetTable<Models.BitRedeems>();
        public ITable<Models.ModCommands> ModCommands => this.GetTable<Models.ModCommands>();
    }
}
