using UnityEngine;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Scenes;
using Simulation;
using Unity.Collections;
using Unity.NetCode;

namespace NetCode
{
    public struct StartTheGame : IComponentData
    {
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct GameStartSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            //state.RequireForUpdate<StartTheGame>();
            state.RequireForUpdate<GameSetup>();
        }

        public void OnUpdate(ref SystemState state)
        {
            GameSetup gameSetup = SystemAPI.GetSingleton<GameSetup>(); //GameSetup gameSetup = SystemAPI.GetSingletonRW<GameSetup>().ValueRW;
            EntityCommandBuffer ecb = SystemAPI.GetSingletonRW<EndSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged);
            //ecb = new EntityCommandBuffer;
            foreach (var (_, entity) in
            SystemAPI.Query<StartTheGame>().WithEntityAccess())
            {
                Debug.Log("Starting the game");

                gameSetup.IsGameplayUnderway = true;
                
                //bool setupUnderway = SystemAPI.GetComponent<bool>(gameSetup.IsGameplayUnderway); //eh? error cannot convert from bool to entity?
                //setupUnderway = true; //oops can't do that
                
                foreach (var (_, setupEntity) in
                SystemAPI.Query<GameSetup>().WithEntityAccess())
                {
                    ecb.SetComponent<GameSetup>(setupEntity, new GameSetup  //eh? cannot convert from simulation.gamesetup to entity
                    {
                        IsGameplayUnderway = true,
                        CharacterSimulation = gameSetup.CharacterSimulation,
                        Player = gameSetup.Player,
                        RatProjectileSimulation = gameSetup.RatProjectileSimulation,

                    });
                }
                
                //gameSetup.IsGameplayUnderway = true;            
                ecb.DestroyEntity(entity);

            }

            //ecb.Playback(state.EntityManager);
            //ecb.Dispose();

        }

    }
    }