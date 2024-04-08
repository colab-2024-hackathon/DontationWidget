using Alkami.Broker;
using Alkami.MicroServices.Accounts.Contracts;
using Alkami.MicroServices.Accounts.Contracts.Filters_And_Mappers;
using Alkami.MicroServices.Accounts.Contracts.Requests;
using Alkami.MicroServices.Accounts.Contracts.Responses;
using Alkami.MicroServices.Accounts.Service.Client;
using Alkami.MicroServices.Security.Contracts;
using Alkami.MicroServices.Security.Contracts.Requests;
using Alkami.MicroServices.Security.Data;
using Alkami.MicroServices.Security.Service.Client;
using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace DonationWidget.Helpers
{
    /// <summary>
    /// This class is generated based on the additional code needed selected when filling out the template form
    /// For example you will see code in here if you selected Add code to get account information
    /// </summary>
    internal class AlkamiHelpers
    {

        //AccountInfoCode Start

        //install nuget package Alkami.MicroServices.Accounts.Service.Client
        public static Func<IAccountServiceContract> accountsService = () => new AccountServiceClient();

        //install nuget package Alkami.MicroServices.Security.Client
        public static Func<ISecurityServiceContract> securityService = () => new SecurityServiceClient();

        private static readonly ILog Logger = LogManager.GetLogger<AlkamiHelpers>();
        public GetAccountRequest GenerateAccountRequest(IEnumerable<long> accountIds, string accountFormat = "{0}{1}")
        {
            // Create the request to get the accounts from the Accounts microservice
            var accountRequest = new GetAccountRequest()
            {
                Filter = new AccountFilter() { Ids = accountIds.ToList(), IncludeExternal = true },
                Mapping = new AccountMapper()
                {
                    IncludeAccountType = true,
                    IncludeAccountTypeFields = null,
                    IncludeRoutingInfo = true,
                    AccountMaskSettings = new AccountMaskSettings() { FormatOnlyAccountHolder = false, JoinAccountHolderNumberFormatString = accountFormat, PadOutput = false }
                }
            };
            return accountRequest;
            //this.AugmentRequest(accountRequest);
            //var accountsResponse = AsyncHelper.RunSync(() => accountsService().GetAccountAsync(accountRequest));
        }

        public AccountResponse CallAccountService(GetAccountRequest request)
        {
            var accountsResponse = AsyncHelper.RunSync(() => accountsService().GetAccountAsync(request));
            return accountsResponse;
        }

        /// <summary>
        /// gets accounts associated with the userId
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public GetUserRequest GetUserRequest(long userId)
        {
            //var userId = this.CurrentUser.Id;
            var getUserRequest = new GetUserRequest() { UserId = userId, Mapping = new UserMapper() { ShouldIncludeExternalUserAccounts = true, ShouldIncludeUserAccounts = true, ShouldIncludePermissions = true, ShouldIncludeUserAccountPermissions = true } };
            return getUserRequest;

        }

        public List<UserAccount> GetUserAccounts(GetUserRequest getuserRequest)
        {
            // Get the user account from the Security microservice
            var userAccount = AsyncHelper.RunSync(() => securityService().GetUserAsync(getuserRequest));

            // Return an empty list if the user account doesn't exist or is marked as deleted.
            if (userAccount == null || userAccount.Users.FirstOrDefault().IsDeleted)
                return new List<UserAccount>();

            // There should only be one user in the UserAccount so we will grab the first one and it' associated accounts.
            return userAccount.Users.FirstOrDefault().UserAccounts;
        }
        //AccountInfoCode end

    }

}

