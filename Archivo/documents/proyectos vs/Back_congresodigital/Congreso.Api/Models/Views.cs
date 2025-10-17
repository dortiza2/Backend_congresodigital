using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Congreso.Api.Models
{
    [Table("vw_public_activities")]
    public class PublicActivityView
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }
        [Column("title")]
        public string Title { get; set; } = string.Empty;
        [Column("activity_type")]
        public string ActivityType { get; set; } = string.Empty;
        [Column("location")]
        public string Location { get; set; } = string.Empty;
        [Column("start_time")]
        public DateTime StartTime { get; set; }
        [Column("end_time")]
        public DateTime EndTime { get; set; }
        [Column("capacity")]
        public int Capacity { get; set; }
        [Column("published")]
        public bool Published { get; set; }
        [Column("enrolled_count")]
        public int EnrolledCount { get; set; }
        [Column("available_spots")]
        public int AvailableSpots { get; set; }
    }

    [Table("vw_user_enrollments")]
    public class UserEnrollmentView
    {
        [Key]
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public Guid ActivityId { get; set; }
        public string ActivityTitle { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public DateTime EnrollmentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsUmg { get; set; }
        public string OrgName { get; set; } = string.Empty;
    }

    [Table("vw_podium_by_year")]
    public class PodiumByYearView
    {
        [Key]
        public int Id { get; set; }
        public int Year { get; set; }
        public Guid ActivityId { get; set; }
        public string ActivityTitle { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int Position { get; set; }
        public string Prize { get; set; } = string.Empty;
        public DateTime AwardDate { get; set; }
        public bool IsUmg { get; set; }
        public string OrgName { get; set; } = string.Empty;
    }
}