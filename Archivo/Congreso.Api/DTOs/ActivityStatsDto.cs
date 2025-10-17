using System;

namespace Congreso.Api.DTOs
{
    public class ActivityStatsDto
    {
        public int TotalActivities { get; set; }
        public int PublishedActivities { get; set; }
        public int DraftActivities { get; set; }
        public int TotalCapacity { get; set; }
        public int TotalEnrollments { get; set; }
        public double AverageCapacity { get; set; }
        public double FillRate { get; set; }
    }
}