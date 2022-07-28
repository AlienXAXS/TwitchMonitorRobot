using System;
using System.Runtime.CompilerServices;

namespace TMRAgent
{
    public static class ConsoleUtil
    {
        public static void WriteToConsole(string message, LogLevel logLevel, ConsoleColor foregroundColor = ConsoleColor.White, ConsoleColor backgroundColor = ConsoleColor.Black, [CallerMemberName] string callerName = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
        {
            Console.ForegroundColor = foregroundColor;
            Console.BackgroundColor = backgroundColor;

            callerFilePath = callerFilePath.Substring(callerFilePath.LastIndexOf('\\')+1);

            var dtNow = DateTime.Now;

            Console.WriteLine($"[{dtNow.Year:####}-{dtNow.Month:0#}-{dtNow.Day:0#} {dtNow.Hour:0#}:{dtNow.Minute:0#}:{dtNow.Second:0#}-{logLevel}]-[{System.Threading.Thread.CurrentThread.ManagedThreadId}:{callerName}] {message}");

            // Don't throw debug messages at Discord.
            if (logLevel == LogLevel.Debug) return;

            Discord.Handler.SendWebhookMessage($"[{callerFilePath}@{callerLineNumber}/{callerName}/T{System.Threading.Thread.CurrentThread.ManagedThreadId}] -> [{Enum.GetName(logLevel)}] {message}");
        }

        public enum LogLevel
        {
            Info,
            Debug,
            Warn,
            Error,
            Fatal
        }
    }
}
