using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.NetCode;
using Unity.Collections;

namespace NetCode
{
    /*
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct ClientRequestGameEntrySystem : ISystem
    {
        private EntityQuery _pendingNetworkIdQuery;

        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<NetworkId>().WithNone<NetworkStreamInGame>();
            _pendingNetworkIdQuery = state.GetEntityQuery(builder);
            state.RequireForUpdate(_pendingNetworkIdQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var pendingNetworkIds = _pendingNetworkIdQuery.ToEntityArray(Allocator.Temp);

            foreach (var pendingNetworkId in pendingNetworkIds)
            {
                ecb.AddComponent<NetworkStreamInGame>(pendingNetworkId);
                //in the tutorial, he builds the moba team request. we will need something similar, but should just check for, "is there room in game?"

            }
        }
    }
    */
}
