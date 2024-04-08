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

//using Alkami.Client.Framework.Utility;//used when widget setting is uncommented below


namespace DonationWidget.Controllers
{
    [ClaimsAuthorizationFilter(PermissionNames.NoPermissions)]
    public class DonationWidgetController : BaseController
    {


        /// <summary>
        /// Gets the logger
        /// </summary>
        private static readonly ILog Logger = LogManager.GetLogger<DonationWidgetController>();

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
                    }
                };
                model.Charities = charityList;
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






    }
}
