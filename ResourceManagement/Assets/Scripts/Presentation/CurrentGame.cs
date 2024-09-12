using System;
using System.Collections.Generic;
using System.Linq;
using NetCode;
using Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Presentation
{
    // WARNING: Accessing Properties of this class is likely not super performant.
    //  Refrain from checking them every frame as much as possible
    public static class CurrentGame
    {
        static EntityQuery? ThisPlayerQuery
        {
            get
            {
                if (EntityWorlds.ClientEntityManager == null)
                    return null;
                return EntityWorlds.ClientEntityManager.Value.CreateEntityQuery(
                    typeof(GhostOwnerIsLocal), typeof(ThirdPersonCharacterComponent));
            }
        }
        
        static EntityQuery? AllPlayersQuery
        {
            get
            {
                if (EntityWorlds.ClientEntityManager == null)
                    return null;
                return EntityWorlds.ClientEntityManager.Value.CreateEntityQuery(
                    typeof(ThirdPersonCharacterComponent));
            }
        }

        public static Entity? ThisPlayer
        {
            get
            {
                var players = ThisPlayerQuery?.ToEntityArray(Allocator.Temp);
                if (players != null && players.Value.Length == 1)
                {
                    var player = players.Value[0];
                    players.Value.Dispose();
                    return player;
                }

                if (players == null)
                    return null;

                if (players.Value.Length > 1)
                {
                    Debug.LogError("Somehow found more than one local player??? That's wrong.");
                }

                players.Value.Dispose();
                return null;
            }
        }

        public static Entity[] AllPlayers
        {
            get
            {
                var queryResult = AllPlayersQuery?.ToEntityArray(Allocator.Temp);
                var players = queryResult == null 
                    ? Array.Empty<Entity>() 
                    : queryResult.Value.ToArray();
                queryResult?.Dispose();
                return players;
            }
        }
        
        public static int GetScore(Entity? player) 
        {
            if (player == null)
                return 0;

            var score = EntityWorlds.ClientEntityManager?
                .GetComponentData<CharacterScore>(player.Value);
            return score?.Value ?? 0;
        }

        public static int GetRatCount(Entity? player) 
        {
            if (player == null)
                return 0;

            var rats = EntityWorlds.ClientEntityManager?
                .GetBuffer<ThrowableFollowerElement>(player.Value);
            return rats?.Length ?? 0;
        }

        public static bool TryGetTransform(Entity? entity, out LocalTransform tf)
        {
            tf = default;
            if (entity == null || !(EntityWorlds.ClientEntityManager?.Exists(entity.Value) ?? false))
            {
                return false;
            }

            var queryResult = EntityWorlds.ClientEntityManager?.GetComponentData<LocalTransform>(entity.Value);
            if (queryResult == null)
            {
                return false;
            }

            tf = queryResult.Value;
            return true;
        }

        public static bool TryGetRatBuffer(Entity? player, out ThrowableFollowerElement[] rats)
        {
            rats = null;
            if (player == null)
                return false;
            
            var ratBuffer = EntityWorlds.ClientEntityManager?
                .GetBuffer<ThrowableFollowerElement>(player.Value);
            if (ratBuffer == null)
                return false;
            rats = new ThrowableFollowerElement[ratBuffer.Value.Length];
            for (int i = 0; i < rats.Length; ++i)
            {
                rats[i] = ratBuffer.Value[i];
            }
            return true;
        }
    }
}
