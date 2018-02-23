using System;

namespace MrCooperPsa
{
    public struct TimeEntry
    {
        public DateTimeOffset Date { get; set; }
        public TimeSpan Duration { get; set; }
        public string Account { get; set; }
        public string Project { get; set; }
    }
}