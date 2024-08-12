//using Unity.Netcode;
using UnityEngine;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Text;
using AddressFamily = System.Net.Sockets.AddressFamily;
using PlayerPrefs = UnityEngine.PlayerPrefs;
using DateTime = System.DateTime;

namespace NetCode
{
    public class IPFetcher
    {
        public enum Status
        {
            Uninitialized,
            RequestSent,
            ResponseReceived,
            UsingLocal,
        }

        private string playerPrefIpHandle = "playerIp";
        private string playerPrefIpTimestampHandle = "playerIpTimestamp";

        public string locallyStoredAddress;
        public string myAddressLocal;
        public string myAddressGlobal;
        public bool ShouldFetchAddresses => m_Status == Status.Uninitialized;
        public bool HasAddresses => m_Status == Status.ResponseReceived;
        public Task FetchTask { get; private set; }
        Status m_Status;

        public bool playerPrefIpIsCurrent()
        {
            if (!PlayerPrefs.HasKey(playerPrefIpHandle)) { return false; }

            DateTime now = DateTime.Now;
            DateTime lastUpdate = DateTime.Parse(PlayerPrefs.GetString(playerPrefIpTimestampHandle));

            return (lastUpdate > now.AddHours(-24) && lastUpdate < now);
        }

        public void setPlayerPrefIp(string ipString)
        {
            PlayerPrefs.SetString(playerPrefIpHandle, ipString);
            DateTime now = DateTime.Now;
            locallyStoredAddress = ipString;

            PlayerPrefs.SetString(playerPrefIpTimestampHandle, now.ToString());
            PlayerPrefs.Save();

        }

        public void FetchIPAddresses()
        {
            //check player prefs logic to fetch task or not
            if (playerPrefIpIsCurrent())
            {
                locallyStoredAddress = PlayerPrefs.GetString(playerPrefIpHandle);
                return;
            }

            if (m_Status != Status.Uninitialized)
            {
                Debug.LogError("Already fetched addresses. Can't do it twice!");
                return;
            }
            FetchTask = Task.Run(AddressFetchTask);
    
        }

        public string GetIpAddress()
        {
            if (playerPrefIpIsCurrent())
            {
                return PlayerPrefs.GetString(playerPrefIpHandle);
            }
            else
            {
                FetchTask = Task.Run(AddressFetchTask);
                FetchTask.Wait();
                if (!HasAddresses)
                {
                    Debug.LogError("Something bad happened while waiting for IP Addresses...");
                }
                setPlayerPrefIp(myAddressGlobal);
                return myAddressGlobal;
            }

        }


        void AddressFetchTask()
        {
            m_Status = Status.RequestSent;
            //Get the local IP
            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in hostEntry.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    myAddressLocal = ip.ToString();
                    break;
                }
            }


            // (7.14.24) TODO | P0 - NetCode | Store IP Address in PlayerPrefs
            //  Rather than try and pull the IP address every time, we should check if we've already got a "fresh"
            //  address for this player in their PlayerPrefs. If we do and it's not too stale, just use that. Also,
            //  give the player some way to explicitly set their IP in PlayerPrefs and use that instead if they have
            //  done so.
            // Get Global IP
            // Unity Player Prefs can store data between games. I can store my IP and not pull.
            // populate text box with web request results so player can input manually.
            // "Do you want to host with this address?"
            // Fetch every 24 hours. 


            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.ipify.org");
            request.Method = "GET";
            request.Timeout = 3000; //time in ms
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Stream stream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                    myAddressGlobal = reader.ReadToEnd();
                }
                else
                {
                    Debug.LogError("Timed out? " + response.StatusDescription);
                    myAddressGlobal = myAddressLocal; //"127.0.0.1";
                }
            }
            catch (WebException ex)
            {
                Debug.Log($"Failed to get IP: {ex.Message} \n Will use local IP ({myAddressLocal})");
                myAddressGlobal = myAddressLocal; // "127.0.0.1";
            }

            m_Status = Status.ResponseReceived;

        }
    }
}