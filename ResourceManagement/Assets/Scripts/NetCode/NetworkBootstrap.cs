using Unity.NetCode;
using UnityEngine;


namespace NetCode
{
    [UnityEngine.Scripting.Preserve]
    public class NetworkBootstrap : ClientServerBootstrap
    {
        public override bool Initialize(string defaultWorldName)
        {
            AutoConnectPort = 7979;
            return base.Initialize(defaultWorldName);
        }
    }
}
