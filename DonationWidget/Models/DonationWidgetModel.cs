using Alkami.Client.Framework.Mvc;
using DonationWidget.Models;
using System;
using System.Collections.Generic;
using Account = Alkami.MicroServices.Accounts.Data.Account;

namespace DonationWidget.Models
{
    public class DonationWidgetModel : BaseModel
    {
        /// <summary>
        /// Display method
        /// </summary>
        public string DisplayMethod { get; set; }
        public List<Charity> Charities { get; set; }
        public List<Account> Accounts { get; set; }
    }
}
