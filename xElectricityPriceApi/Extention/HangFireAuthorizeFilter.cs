using Hangfire.Annotations;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication;
using Serilog;
using System.Security.Claims;

namespace xElectricityPriceApi.Extention
{
    public class HangFireAuthorizeFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize([NotNull] DashboardContext context)
        {
            /*
            var httpContext = context.GetHttpContext();
            if (httpContext.User.Identity?.IsAuthenticated == false)
            {
                Log.Logger.Debug("Request is not authenticated, checking headers");
                var authUser = httpContext.ChallengeAsync();
                authUser.Wait();
            }

            var claimList = httpContext.User.Claims;
            foreach (var claim in claimList)
            {
                if (claim.Type == ClaimTypes.Role)
                {
                    if (Enum.TryParse(claim.Value, true, out Role role))
                    {
                        if (role == Role.Admin)
                        {
                            Log.Logger.Debug("Request is authenticated, and found required role", role);
                            return true;
                        }
                    }
                }
            }
            */
            // Now we have to check the cookie.
            //[login, 8fe74217-c816-4360-8375-69c93a601992:5aada059-e8a8-4ee3-a830-0d72ebc2e198]


            return false;
        }
    }
}
