/*
Version checker for the mod.
Gives notifications for updates.
*/
using System;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Net.Http;

namespace NPCMapLocations
{
    class MapModVersionChecker
    {
        private static string uri = "https://api.github.com/repos/Bouhm/stardew-valley-mods/releases/latest";
        public const string VERSION = "1.44";
        public static string notification = "";

        // Notification message
        public static async void getNotification()
        {
            string latestVer = await getLatestVersion();
            string currentVer = VERSION;
            int compareVer = compareVersions(currentVer, latestVer);

            if (compareVer > 0)
            {
                notification = " - Update is Available.";
            }
            else if (compareVer == 0)
            {
                notification = " - Latest Version.";
            }
            else
            {
                notification = "";
            }
        }

        private static async Task<JObject> GetJsonAsync(string uri)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Map Mod Version Checker");
                try
                {
                    var jsonString = await client.GetStringAsync(uri).ConfigureAwait(false);
                    return JObject.Parse(jsonString);
                }
                catch
                {
                    return null;
                }
            }
        }

        private static async Task<string> getLatestVersion()
        {
            JObject json = await GetJsonAsync(uri);
            if (json == null)
            {
                return "";
            }
            return (string)json["tag_name"];
        }

        // Comparable for version since converting to double has problems with how different regions interpret double
        private static int compareVersions(string current, string latest)
        {
            if (string.IsNullOrEmpty(latest)) 
            {
                return -1;
            }
            string[] currentVer = current.Split('.');
            string[] latestVer = latest.Split('.');

            // When switching from non-semantic version to semantic
            if (currentVer.Length != latestVer.Length)
            {
                return latestVer.Length - currentVer.Length;
            }
            // Compare major version
            if (!currentVer[0].Equals(latestVer[0]))
            {
                return Int32.Parse(latestVer[0]) - Int32.Parse(currentVer[0]);
            } 
            // Compare minor version (if major versions are equal) 
            if (!currentVer[1].Equals(latestVer[1])) {
                return Int32.Parse(latestVer[1]) - Int32.Parse(currentVer[1]);
            }
            if ((currentVer.Length > 2 && latestVer.Length > 2) && !currentVer[3].Equals(latestVer[3]))
            {
                return Int32.Parse(latestVer[3]) - Int32.Parse(currentVer[3]);
            }
            return 0;
        }
    }
}
