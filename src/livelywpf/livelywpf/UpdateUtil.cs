using System;
using System.Threading.Tasks;
using Octokit;

namespace YetAnotherLosslessCutter
{
   static class UpdateUtil
    {
        public static async Task<bool?> IsNewVersionAvailable()
        {
            var client = new GitHubClient(new ProductHeaderValue("YALC"));

            return false;

        }
    }
}
