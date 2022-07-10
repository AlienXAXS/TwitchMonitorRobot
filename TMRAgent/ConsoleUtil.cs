﻿using System;
using System.Collections.Generic;
using System.Text;

namespace TMRAgent
{
    public static class ConsoleUtil
    {
        public static void WriteToConsole(string message, LogLevel logLevel, ConsoleColor foregroundColor = ConsoleColor.White, ConsoleColor backgroundColor = ConsoleColor.Black)
        {
            Console.ForegroundColor = foregroundColor;
            Console.BackgroundColor = backgroundColor;

            var dtNow = DateTime.Now;

            Console.WriteLine($"[{dtNow.Year:####}-{dtNow.Month:0#}-{dtNow.Day:0#} {dtNow.Hour:0#}:{dtNow.Minute:0#}:{dtNow.Second:0#}-{logLevel}] {message}");

            // This is awful, I know...
            if ( !message.StartsWith("Processing Chat Message") && !message.StartsWith("Processing Command Message"))
                Discord.Handler.SendWebhookMessage($"[{Enum.GetName(logLevel)}] {message}");
        }

        public enum LogLevel
        {
            INFO,
            DEBUG,
            WARN,
            ERROR,
            FATAL
        }
    }
}
