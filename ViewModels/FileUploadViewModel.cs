namespace Proj_Frame.ViewModels
{
    using System.ComponentModel.DataAnnotations;
    using Microsoft.AspNetCore.Http;

    public class FileUploadViewModel
    {
        [Required]
        public string Subject { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        public string Keywords { get; set; }

        [Required]
        public string FileName { get; set; }

        [Required]
        public IFormFile UploadedFile { get; set; }
    }
}
