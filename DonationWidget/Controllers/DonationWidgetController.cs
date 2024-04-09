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

        public INotificationRepository NotificationRepository { get; set; }

        //please add this logPrefix to the beginging of all your log statements
        string logPrefix = "DonationWidget";


        private AlkamiHelpers alkamiHelpers = new AlkamiHelpers();


        public ActionResult Index()
        {

            try
            {
                Logger.DebugFormat($" {logPrefix} [GET] Controller/Index");

                return View("Index");
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






    }
}
