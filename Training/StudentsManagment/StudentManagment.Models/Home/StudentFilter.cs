using System.ComponentModel.DataAnnotations;


namespace StudentManagment.Models.Home
{
    public class StudentFilter
    {
        [Display(Name = "Student status")]
        [Range(-1,5,ErrorMessage = "Please choose from given values")]
        public int Status { get; set; }
    }
}
