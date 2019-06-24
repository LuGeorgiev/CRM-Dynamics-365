using System.ComponentModel.DataAnnotations;

using static StudentManagment.Models.DataModelsConstants;

namespace StudentManagment.Models.Student
{
    public class StudentDetailsViewModel
    {
        [RegularExpression(NAME_REGEX_PATTERN, ErrorMessage =NAME_ERROR_MESSAGE)]
        [Display(Name = DISPLAY_FIRST_NAME)]
        [Required]
        public string FirstName { get; set; }

        
        [RegularExpression(NAME_REGEX_PATTERN, ErrorMessage =NAME_ERROR_MESSAGE)]
        [Display(Name = DISPLAY_LAST_NAME)]
        [Required]
        public string LastName { get; set; }

        [Required]
        [Display(Name = "StudentId")]
        public string StudentId { get; set; }

        public int StudentStatusValue { get; set; }

        [Display(Name = "Program")]
        public string Program { get; set; }

        [Display(Name = "Program advisor")]
        public string ProgramAdvisor { get; set; }

        [Display(Name ="Student status")]
        public string StudentStaus
        {
            get
            {
                if (this.StudentStatusValue == 1)
                {
                    return "Enrolled";
                }
                else if (this.StudentStatusValue == 2)
                {
                    return "Graduated";
                }
                else if (this.StudentStatusValue == 3)
                {
                    return "Never Marticulated";
                }
                else if (this.StudentStatusValue == 4)
                {
                    return "On Hold";
                }
                else if (this.StudentStatusValue == 5)
                {
                    return "Unenrolled";
                }
                else
                {
                    return "Not Correct status!";
                }
            }
        }


    }
}
