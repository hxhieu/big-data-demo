using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HDInsight.Models.Account
{
    public class ManageAccountOpenIdAppModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }

    public class ManageAccountModel
    {
        public List<ManageAccountOpenIdAppModel> OpenIdApps { get; set; }
        [Required]
        public string NewAppName { get; set; }

        public ManageAccountModel()
        {
            OpenIdApps = new List<ManageAccountOpenIdAppModel>();
        }
    }
}
