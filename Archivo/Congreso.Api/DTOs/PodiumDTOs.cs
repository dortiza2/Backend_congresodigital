using System.ComponentModel.DataAnnotations;
using System;

namespace Congreso.Api.DTOs
{
    public class PodiumResponse
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public int Place { get; set; }
        public Guid? ActivityId { get; set; }
        public string ActivityTitle { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public string? WinnerName { get; set; }
        public DateTime? AwardDate { get; set; }
        public int? TeamId { get; set; }
        public string? PrizeDescription { get; set; }
    }

    public class CreatePodiumRequest
    {
        [Required(ErrorMessage = "El año es requerido")]
        [Range(2020, 2030, ErrorMessage = "El año debe estar entre 2020 y 2030")]
        public int Year { get; set; }

        [Required(ErrorMessage = "El lugar es requerido")]
        [Range(1, 3, ErrorMessage = "El lugar debe estar entre 1 y 3")]
        public int Place { get; set; }

        [Required(ErrorMessage = "El ID de actividad es requerido")]
        public int ActivityId { get; set; }

        [Required(ErrorMessage = "El título de actividad es requerido")]
        [StringLength(200, ErrorMessage = "El título no puede exceder 200 caracteres")]
        public string ActivityTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "El ID de usuario es requerido")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "El nombre del ganador es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string WinnerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de premiación es requerida")]
        public DateTime AwardDate { get; set; }

        public int? TeamId { get; set; }

        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string? PrizeDescription { get; set; }
    }

    public class UpdatePodiumRequest
    {
        [Required(ErrorMessage = "El año es requerido")]
        [Range(2020, 2030, ErrorMessage = "El año debe estar entre 2020 y 2030")]
        public int Year { get; set; }

        [Required(ErrorMessage = "El lugar es requerido")]
        [Range(1, 3, ErrorMessage = "El lugar debe estar entre 1 y 3")]
        public int Place { get; set; }

        [Required(ErrorMessage = "El ID de actividad es requerido")]
        public int ActivityId { get; set; }

        [Required(ErrorMessage = "El título de actividad es requerido")]
        [StringLength(200, ErrorMessage = "El título no puede exceder 200 caracteres")]
        public string ActivityTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "El ID de usuario es requerido")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "El nombre del ganador es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string WinnerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de premiación es requerida")]
        public DateTime AwardDate { get; set; }

        public int? TeamId { get; set; }

        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        public string? PrizeDescription { get; set; }
    }

    public class PodiumQueryParameters
    {
        [Required(ErrorMessage = "El año es requerido")]
        [Range(2020, 2030, ErrorMessage = "El año debe estar entre 2020 y 2030")]
        public int Year { get; set; }
    }
}