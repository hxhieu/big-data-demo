using System.ComponentModel.DataAnnotations;

namespace HDInsight.Models.Account
{
    public class CreateAccountModel
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
