using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrineZip
{
    class SettingsManager
    {

        public static Uri reviewUri = new Uri("ms-windows-store://pdp/?productid=9NKKGGS3VX8G");
        public static Uri helpUri = new Uri("https://winappcenter.com/products/ecms/lively-metro/");


        public static Uri PrivacyUri = new Uri("https://winappcenter.com/PrivacyPolicy.html");

        public static void ReviewProductAsync()
        {
            Windows.System.Launcher.LaunchUriAsync(SettingsManager.reviewUri).AsTask().Wait();

        }
        public static void OpenPrivacyAsync()
        {
            Windows.System.Launcher.LaunchUriAsync(SettingsManager.PrivacyUri).AsTask().Wait();

        }
        public static void OpenHelpAsync()
        {
            Windows.System.Launcher.LaunchUriAsync(SettingsManager.helpUri).AsTask().Wait();

        }
    }
}
