using Alkami.Client.Framework.Mvc;
using Alkami.Common;
using Alkami.MicroServices.Accounts.Data;
using Alkami.Security.Common.Claims;
using Common.Logging;
using DonationWidget.Helpers;
using DonationWidget.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web.Mvc;
using WebToolkit;
using Account = Alkami.MicroServices.Accounts.Data.Account;
using Exception = System.Exception;
using Alkami.Client.Services.Notification.Repository;
using Alkami.App.Notification.Contracts;
using Alkami.Security.Common.DataContracts;
using Alkami.Client.Messages;
using Alkami.MicroServices.Accounts.Contracts.Filters_And_Mappers;
using Alkami.MicroServices.Accounts.Contracts.Requests;
using Alkami.MicroServices.Accounts.Contracts;
using Alkami.MicroServices.Accounts.Service.Client;
using Alkami.MicroServices.Transfers.CoreProxy.Contracts;
using Alkami.MicroServices.Transfers.CoreProxy.Service;
using Alkami.MicroServices.Transfers.CoreProxy.Contracts.Requests;
using Alkami.MicroServices.Transfers.CoreProxy.Contracts.Models;
using System.IO;
using Alkami.MicroServices.Accounts.Service.Client;

//using Alkami.Client.Framework.Utility;//used when widget setting is uncommented below


namespace DonationWidget.Controllers
{
    [ClaimsAuthorizationFilter(PermissionNames.NoPermissions)]
    public class DonationWidgetController : BaseController
    {

        public static Func<IAccountServiceContract> accountsService = () => new AccountServiceClient();

        public static Func<ITransfersCoreProxyServiceContract> TransfersCoreProxyServiceFactory = () => new Alkami.MicroServices.Transfers.CoreProxy.Service.Client.ServiceClient();


        /// <summary>
        /// Gets the logger
        /// </summary>
        private static readonly ILog Logger = LogManager.GetLogger<DonationWidgetController>();

        public INotificationRepository NotificationRepository { get; set; }

        //please add this logPrefix to the beginging of all your log statements
        string logPrefix = "DonationWidget";


        private AlkamiHelpers alkamiHelpers = new AlkamiHelpers();


        public ActionResult Index()
        {
            var model = new DonationWidgetModel();
            
            try
            {
                List<Charity> charityList = new List<Charity>() {
                    new Charity()
                    {
                        AccountIdentifier=Guid.NewGuid(),
                        Name="United Way",
                        CharityInfo="United Way is a non-profit organization that works with almost 1,200 local United Way offices throughout the country in a coalition of charitable organizations to pool efforts in fundraising and support.",
                        TotalDonationAmount=1000,
                        GoalAmount=5000,
                        WebsiteUrl="https://www.unitedway.org/",
                        LogoUrl="https://www.unitedway.org/assets/img/united-way-lock-up-rgb-cropped.jpg"
                    },
                    new Charity()
                    {
                        AccountIdentifier = Guid.NewGuid(),
                        Name = "American Red Cross",
                        CharityInfo = "The American Red Cross is a humanitarian organization that provides emergency assistance, disaster relief, and education inside the United States.",
                        TotalDonationAmount = 2000,
                        GoalAmount = 10000,
                        WebsiteUrl = "https://www.redcross.org/",
                        LogoUrl = "https://www.redcross.org/content/dam/redcross/imported-images/redcross-logo.png.img.png"
                    },
                    new Charity()
                    {
                        AccountIdentifier = Guid.NewGuid(),
                        Name = "Salvation Army",
                        CharityInfo = "The Salvation Army is a Protestant Christian church and an international charitable organization. The organization reports a worldwide membership of over 1.7 million, consisting of soldiers, officers and adherents collectively known as Salvationists.",
                        TotalDonationAmount = 3000,
                        GoalAmount = 15000,
                        WebsiteUrl = "https://www.salvationarmyusa.org/",
                        LogoUrl = "https://static.salvationarmy.org/us-east-1/templates/symphony/static_resources/images/global/shield.svg"
                    },
                };
                model.Charities = charityList;
                var userId = this.CurrentUser.Id;
                var Accounts = GetAccounts(userId);

                model.Accounts = Accounts;

                Logger.DebugFormat($" {logPrefix} [GET] Controller/Index");
                return View(model);
            }
            catch (Exception e)
            {
                Logger.Error("Error [GET] Controller/Index", e);
                return View("Error");
            }

        }


        public ActionResult Accounts()
        {
            var userId = this.CurrentUser.Id;
            var Accounts = GetAccounts(userId);

            var model = new AccountsModel();
            model.Accounts = Accounts;
            //model.Accounts.FirstOrDefault().Id
            // model.Accounts.FirstOrDefault().AvailableBalance
            //model.Accounts.FirstOrDefault().Number
            // model.Accounts.FirstOrDefault().AccountType
            return View("Accounts", model);
        }

        private List<Account> GetAccounts(long userId)
        {
            Logger.DebugFormat($" {logPrefix} GetAccounts Controller method called userId {userId}");
            //Get User Request to call security service
            var getUserRequest = alkamiHelpers.GetUserRequest(userId);

            Logger.DebugFormat($" {logPrefix} GetUserRequest returned for user {userId}");

            //augement request will add bank Identifier an other important information
            this.AugmentRequest(getUserRequest);

            Logger.DebugFormat($" {logPrefix} userRequest Augmented");

            //Use the User request to pull back the useraccounts
            var userAccounts = alkamiHelpers.GetUserAccounts(getUserRequest);

            //extract active account Ids from the user accounts
            var accountIds = userAccounts?.Where(y => !y.Deleted)?.Select(x => x.AccountId) ?? new List<long>();
            Logger.DebugFormat($" {logPrefix} GetUserAccounts returned {String.Join(",", accountIds)}");

            //further information on accounts can be obtained using the account microservice
            var accountFormat = BankSettings.GetSettingOrDefault<string>(BankSettingName.JoinAccountHolderNumberFormatString, "{0}{1}");
            Logger.DebugFormat($" {logPrefix} accountFormat returned {accountFormat}");

            var accountRequest = alkamiHelpers.GenerateAccountRequest(accountIds, accountFormat);
            Logger.DebugFormat($" {logPrefix} GenerateAccountRequest returned for user {userId}");

            //augement request will add bank Identifier an other important information
            this.AugmentRequest(accountRequest);
            Logger.DebugFormat($" {logPrefix} accountRequest Augmented");

            //The alkami Account Identifier Guid can be obtained from the accounts response
            var accountResponse = alkamiHelpers.CallAccountService(accountRequest);
            Logger.DebugFormat($" {logPrefix} CallAccountService returned for user {userId} accounts {String.Join(",", accountIds)}");

            //examples of what is on the account object, alot more information is available on this obect
            // accountResponse.Accounts.FirstOrDefault().AccountIdentifier
            //  accountResponse.Accounts.FirstOrDefault().AccountType
            //  accountResponse.Accounts.FirstOrDefault().AvailableBalance

            return accountResponse.Accounts;

        }

        public bool SendEmail()
        {
            bool isSent = false;

            UserContactEmail emailContact =
                CurrentUser.GetUserContact<UserContactEmail>(x => x.IsPrimary).FirstOrDefault()
                ?? CurrentUser.GetUserContact<UserContactEmail>().FirstOrDefault();

            string subject = "The Boys CU Donation Confirmation";
            string body= "You succesfully donated some money to a charity. You're a really nice person <3.";
            var v1 = this.CurrentBankIdentifier;
            var v2 = CurrentBankName;
            var v3 = CurrentUserIdentifier;
            var v4 = TransportAgentMedium.Email;
            
            try
            {
                var result = NotificationRepository.SendToSpecificAddess(
                        this.CurrentBankIdentifier,
                        CurrentBankName,
                        CurrentUserIdentifier,
                        TransportAgentMedium.Email,
                        emailContact.Email,
                        subject,
                        body,
                        true,
                        true);
                if (result == null)
                {
                    throw new InvalidOperationException("SendToUser response returns null.");
                }

                if (result.Status != Status.Success)
                {
                    string errorMsg = string.Format(
                        "SendEmail failed to send email message. User Identifier [{0}] Email Address: [{1}] Status [{2}] Error [{3}] ",
                        new object[]
                        {
                    CurrentUserIdentifier.ToString(), emailContact.ToString(), result.Status.ToString(),
                    result.Exception == null ? "" : result.Exception.Message
                        });
                    throw new InvalidOperationException(errorMsg);
                }
                else
                {
                    isSent = true;
                }
            }
            catch (Exception e)
            {
                Logger.Error("SendEmail failed.", e);
                isSent = false;
            }

            return isSent;
        }





        /// <summary>
        /// this redirects to an external URL and passes the account number by default
        /// This is designed to work with Account.cshtmlpage
        /// </summary>
        /// <param name="accountNumber"></param>
        /// <returns></returns>
        public ActionResult RedirectToAccount(string accountNumber)
        {
            //Simple redirect Code start
            //urls should normally be done with a widget setting like below, widget settings are added in the admin screen
            //also note the 
            //var url = GetWidgetSetting("SsoUrl"); //if you want redirect best to use URL
            var url = @"https://www.alkami.com";
            //using System.Collections.Specialized;
            NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
            var userBankIdentifier = this.CurrentUser.BankIdentifier;//Identifier of the customers bank
            var userCustomerId = this.CurrentUser.CustomerId;//customerId is usually a core identifier
            var userIdentifier = this.CurrentUser.UserIdentifier; //alkami value

            //more examples of cureUser object Information
            //var userInfoToPassToSSO = this.CurrentUser.FirstName;
            // var userInfoToPassToSSO = this.CurrentUser.LastName;

            //queryString.Add("useridentifier", userIdentifier.ToString());
            queryString.Add("accountNumber", accountNumber);
            queryString.Add("useridentifier", userIdentifier.ToString());
            queryString.Add("customerId", userCustomerId);
            queryString.Add("userBankIdentifier", userBankIdentifier.ToString());

            var redirectUrl = $"{url}?{queryString.ToString()}";

            //Response.Redirect(redirectUrl, true);
            return Redirect(redirectUrl);
            //end Simple redirect Code 

        }

        private List<DonationTransactionModel> ReadLog()
        {
            string filepath = "C:\\Database\\Transactions.txt";
            var transactions = new List<DonationTransactionModel>();
            using (var reader = new StreamReader(filepath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var parts = line.Split(',');
                    if (parts.Length == 3)
                    {
                        string accountID = parts[0];

                        var transaction = new DonationTransactionModel
                        {
                            FromAccount = GetAnAccount((long)Convert.ToDouble(accountID)),
                            ToCharity = parts[1],
                            Amount = decimal.Parse(parts[2])
                        };
                        transactions.Add(transaction);
                    }
                }
            }
            return transactions;
        }
        private void LogTransfer(DonationTransactionModel transaction)
        {
            string filepath = "C:\\Database\\Transactions.txt";

            {
                using (var writer = new StreamWriter(filepath))
                {
                    var line = $"{transaction.FromAccount.AccountIdentifier},{transaction.ToCharity},{transaction.Amount}";
                    writer.WriteLine(line);
                }
            }
        }
        private async void TransferFunds(DonationTransactionModel transaction)
        {
            var transferRequest = new AddTransferRequest
            {
                ItemList = new List<TransferRequest>
    {
        new TransferRequest
        {
            Amount = transaction.Amount,
            Description = "Donation to " + transaction.ToCharity,
            DestinationAccount = new TransferAccount
            {
                AccountIdentifier = Guid.Parse("189DD151-CCA9-419F-B9C8-C6FDE379B6A4"),
                TransactionCode = "9999",
            },
            SourceAccount = new TransferAccount
            {
                AccountIdentifier = transaction.FromAccount.AccountIdentifier,
                TransactionCode = "1111",
            },
            EffectiveDate = DateTime.UtcNow,
            ShouldTriggerAccountSync = true,
            TransferType = TransferType.Standard,
        },
    },
            };
            this.AugmentRequest(transferRequest);
            var response = AsyncHelper.RunSync(() => TransfersCoreProxyServiceFactory().AddTransferAsync(transferRequest));
            LogTransfer(transaction);
            return;
        }

        Account GetAnAccount(long accountId)
        {
            Account result = new Account();
            var accountRequest = new GetAccountRequest()
            {
                Filter = new AccountFilter() { Ids = new List<long> { accountId }, IncludeExternal = true },
                Mapping = new AccountMapper()
                {
                    IncludeAccountType = true,
                    IncludeAccountTypeFields = null,
                    IncludeRoutingInfo = true,
                    AccountMaskSettings = new AccountMaskSettings()
                    {
                        FormatOnlyAccountHolder = false,
                        JoinAccountHolderNumberFormatString = BankSettings.GetSettingOrDefault(BankSettingName.JoinAccountHolderNumberFormatString, "{0}{1}"),
                        PadOutput = false
                    }
                }
            };

            this.AugmentRequest(accountRequest);

            // Get the account from the Accounts microservice
            var accountsResponse = AsyncHelper.RunSync(() => accountsService().GetAccountAsync(accountRequest));

            // Because we are filtering by accountId, there should always only be one account under this criteria
            if (accountsResponse.Accounts != null && accountsResponse.Accounts.Count == 1)
                result = accountsResponse.Accounts[0];
            return result;
        }


    }
}
