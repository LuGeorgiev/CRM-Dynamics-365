using System.ComponentModel.DataAnnotations;

using static StudentManagment.Models.DataModelsConstants;

namespace StudentManagment.Models.Student
{
    public class StudentDetailsBindingModel
    {
        [Required]
        public string StudentId { get; set; }

        [RegularExpression(NAME_REGEX_PATTERN, ErrorMessage = NAME_ERROR_MESSAGE)]
        [Display(Name = DISPLAY_FIRST_NAME)]
        [Required]
        public string FirstName { get; set; }

        [RegularExpression(NAME_REGEX_PATTERN, ErrorMessage = NAME_ERROR_MESSAGE)]
        [Display(Name = DISPLAY_LAST_NAME)]
        [Required]
        public string LastName { get; set; }        
    }
}
