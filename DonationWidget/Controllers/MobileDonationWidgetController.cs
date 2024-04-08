using Alkami.Client.Framework.Mvc;
using Alkami.Common;
using Alkami.Security.Common.Claims;
using Common.Logging;
using System;
using System.Web.Mvc;

namespace DonationWidget.Controllers
{
    [ClaimsAuthorizationFilter(PermissionNames.NoPermissions)]
    public class MobileDonationWidgetController : BaseController
    {
        /// <summary>
        /// Gets logger
        /// </summary>
        private static readonly ILog Logger = LogManager.GetLogger<MobileDonationWidgetController>();

        /// <summary>
        /// Standard widget entry route
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            try
            {
                Logger.DebugFormat("[GET] Controller/Index");
                return View("Index");
            }
            catch (Exception e)
            {
                Logger.Error("Error [GET] Controller/Index", e);
                return View("Error");
            }
        }
    }
}