using Alkami.Client.Framework.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Account = Alkami.MicroServices.Accounts.Data.Account;


namespace DonationWidget.Models
{
    public class AccountsModel : BaseModel
    {
        public List<Account> Accounts { get; set; }
    }
}
