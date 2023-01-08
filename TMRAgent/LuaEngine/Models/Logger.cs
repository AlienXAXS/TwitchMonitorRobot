using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMRAgent.LuaEngine.Models
{
    internal class Logger
    {
        public void log(string message) => Log(message);
        public void Log(string message)
        {
            Util.Log(message, Util.LogLevel.Info);
        }
    }
}
