﻿using Microsoft.Diagnostics.Tracing;

namespace R4nd0mApps.TddStud10.Hosts.Console.Diagnostics
{
    [EventSource(Name = Constants.EtwProviderNameHostsConsole)]
    internal sealed class Logger : EventSource
    {
        public static Logger I = new Logger();

        [Event(1, Level = EventLevel.Informational)]
        public void Log(string message)
        {
            WriteEvent(1, message);
        }

        [Event(2, Level = EventLevel.Error)]
        internal void LogError(string message)
        {
            base.WriteEvent(2, message);
        }

        [NonEvent]
        public void Log(string format, params object[] args)
        {
            if (IsEnabled())
            {
                Log(string.Format(format, args));
            }
        }
    }
}
