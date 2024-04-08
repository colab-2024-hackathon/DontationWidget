using Alkami.Client.Framework.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Account = Alkami.MicroServices.Accounts.Data.Account;

namespace DonationWidget.Models
{
	internal class DonationTransactionModel : BaseModel
	{
		public Account FromAccount { get; set; }
		public string ToCharity { get; set; }
		public decimal Amount { get; set; }
	}
}