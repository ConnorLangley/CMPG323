using System.ComponentModel.DataAnnotations;

namespace Proj_Frame.ViewModels
{
    public class FAQViewModel
    {
        [Required(ErrorMessage = "Question description is required.")]
        public string Question_Desc { get; set; }

        [Required(ErrorMessage = "Answer is required.")]
        public string Answer { get; set; }
    }
}
