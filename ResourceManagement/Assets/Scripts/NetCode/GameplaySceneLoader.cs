using System;
using UnityEngine;
using Unity.Entities;
using Unity.Entities.Serialization;

namespace NetCode
{
    public struct GameplaySceneReferences : IComponentData
    {
        public EntitySceneReference Level;
        public EntitySceneReference GameSetup;
        public EntityPrefabReference GameState;
    }

    public class GameplaySceneLoader : MonoBehaviour
    {
        public static GameplaySceneLoader Instance;

        // For editor debugging
        [SerializeField]
        bool LoadWorldsOnPlay;
        bool ImmediateLoadFinished;

        public static EntityWorlds EntityWorldsInstance => ReferenceEquals(null, Instance) 
            ? null 
            : Instance.m_EntityWorlds;
        // (8.11.24) TODO | P3 - NetCode / Tech Debt | Make WorldManager a IDisposable and only use it when appropriate
        //  Right now we create a single WorldManager class whenever this menu spins up, but we could make this
        //  a bit cleaner if the state inside this manager only persisted when we're actually managing these worlds
        //  Rather than expose utility functions to create/clean worlds, we'd simply initialize the worlds on
        //  according to whether it was a Server or not, and then do the appropriate clean-up on Dispose()
        //  This way, we can ensure no lingering state poisons our world manager when going in-between connections
        //  (Also we should manage this statically inside of its own class rather than hiding it in here)
        EntityWorlds m_EntityWorlds;
        
        // TODO: These statics suck and should move into the IDisposeable WorldManager as constructor values,
        //  when that exists.
        public static string IpAddress { get; set; } = "127.0.0.1";
        public static ushort Port { get; set; } = 7979;
        public static bool IsServer { get; set; } = true;
        public static bool GameCanStart { get; set; }
        public static bool ShouldInitializeWorlds => GameCanStart
            && !EntityWorlds.AreInitialized 
            && !EntityWorlds.HasAttemptedInitialization;

        void Awake()
        {
            Instance = this;
            GameCanStart = false;
            IsServer = true;
            IpAddress = "127.0.0.1";
        }

        void Start()
        {
            m_EntityWorlds = new EntityWorlds(this);
            if (LoadWorldsOnPlay && Application.isEditor)
            {
                Debug.Log("[DEBUG] Setting world init flag.");
                GameCanStart = true;
            }
        }

        void Update()
        {
            if (!Application.isEditor || !LoadWorldsOnPlay || ImmediateLoadFinished)
                return;
            
            if (EntityWorlds.AreInitialized)
            {
                Debug.Log("[DEBUG] Starting game.");
                ImmediateLoadFinished = true;
                EntityWorlds.StartGameOnServer();
            }
        }

        void OnDisable()
        {
            m_EntityWorlds = null;
        }

        public static void InitializeWorlds(GameplaySceneReferences sceneReference)
        {
            Instance.m_EntityWorlds.InitializeWorlds(sceneReference, IpAddress, Port, IsServer);
        }
    }
    
    public partial class GameplaySceneLoaderSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<GameplaySceneReferences>();
        }

        protected override void OnUpdate()
        {
            if (!GameplaySceneLoader.ShouldInitializeWorlds)
                return;

            Debug.Log("Initializing gameplay scene.");
            var gameScenes = SystemAPI.GetSingleton<GameplaySceneReferences>();
            GameplaySceneLoader.InitializeWorlds(gameScenes);
        }
    }
}