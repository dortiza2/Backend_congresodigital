using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Congreso.Api.DTOs
{
    public class UpdateImageDto
    {
        [Required]
        public IFormFile File { get; set; }
    }
}