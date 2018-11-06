using System;
using NodaTime;

namespace MrCooperPsa
{
    public struct TimeEntry
    {
        public LocalDate Date { get; set; }
        public TimeSpan Duration { get; set; }
        public string Account { get; set; }
        public string Project { get; set; }
    }
}