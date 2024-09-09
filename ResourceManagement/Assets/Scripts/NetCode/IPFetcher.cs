//using Unity.Netcode;
using System.Net;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AddressFamily = System.Net.Sockets.AddressFamily;
using UnityEngine;
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
            ResponseReceived
        }

        private string playerPrefIpHandle = "playerIp";
        private string playerPrefIpTimestampHandle = "playerIpTimestamp";

        public bool FoundGlobalAddress { get; private set; }
        public string BestAddressFetched => FoundGlobalAddress ? myAddressGlobal : myAddressLocal;
        public string locallyStoredAddress;

        private string myAddressLocal;
        private string myAddressGlobal;
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
            Debug.LogError("Saving player ip: " + ipString);
            PlayerPrefs.SetString(playerPrefIpHandle, ipString);
            DateTime now = DateTime.Now;
            locallyStoredAddress = ipString;

            PlayerPrefs.SetString(playerPrefIpTimestampHandle, now.ToString());
            PlayerPrefs.Save();

        }

        public void FetchIPAddresses()
        {
            if (playerPrefIpIsCurrent())
            {
                Debug.LogError("Loading player current ip address");
                locallyStoredAddress = PlayerPrefs.GetString(playerPrefIpHandle);
                return;
            }

            if (m_Status != Status.Uninitialized)
            {
                Debug.LogError("Already fetched addresses. Can't do it twice!");
                return;
            }

           FetchTask = Task.Run(AddressFetchTask);

            FetchTask.Wait();
            if (!HasAddresses)
            {
                Debug.LogError("Something bad happened while waiting for IP Addresses...");
            }
            setPlayerPrefIp(myAddressGlobal);
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
                    FoundGlobalAddress = true;
                }
                else
                {
                    Debug.LogWarning("Timed out? " + response.StatusDescription);
                }
            }
            catch (WebException ex)
            {
                Debug.Log($"Failed to get IP: {ex.Message} \n Will use local IP ({myAddressLocal})");
            }

            m_Status = Status.ResponseReceived;
        } 
    }
}