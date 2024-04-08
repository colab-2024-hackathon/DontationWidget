using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DonationWidget.Models
{
    public class Charity
    {
        public Guid AccountIdentifier { get; set; }
        public string Name { get; set; }
        public string CharityInfo { get; set; }
        public decimal TotalDonationAmount { get; set; }
        public decimal GoalAmount { get; set; }
        public string WebsiteUrl { get; set; }
        public string LogoUrl { get; set; }
    }
}
