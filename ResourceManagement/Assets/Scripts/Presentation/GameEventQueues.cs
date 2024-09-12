using System;
using System.Collections;
using System.Collections.Generic;
using Simulation;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Presentation
{
    public struct RatPickedUp
    {
        public bool IsThisPlayer;
        public Vector3 RatPosition;
        public Vector3 PlayerPosition;
    }

    public struct RatThrown
    {
        public bool IsThisPlayer;
        public Vector3 Position;
        public Quaternion Direction;
    }
    
    public class GameEventQueues : MonoBehaviour
    {
        public static GameEventQueues Instance { get; private set; }

        Dictionary<Entity, int> m_RatCounts;

        // (Devin) I'll push structs into these queues as things happen in ECS world
        public Queue<PendingRatScored> RatsScored;
        public Queue<RatPickedUp> RatsPickedUp;
        public Queue<RatThrown> RatsThrown;
        //public Queue<> ;

        [SerializeField] private GameObject scoreRatSfx;
        [SerializeField] private GameObject ratPickupSfx;
        [SerializeField] private GameObject ratThrowSfx;

        private GuiPresentation gui;

        [SerializeField] private bool testEvents;

        void Start()
        {
            Instance = this;
            RatsScored = new Queue<PendingRatScored>();
            RatsPickedUp = new Queue<RatPickedUp>();
            RatsThrown = new Queue<RatThrown>();
            m_RatCounts = new Dictionary<Entity, int>();

            if (testEvents)
                StartCoroutine(TestEvents());

            GameObject guiGO = GameObject.FindGameObjectWithTag("GUI");
            if( guiGO )
            {
                gui = guiGO.GetComponent<GuiPresentation>();
            }
        }
        
        private void Update()
        {
            if( !gui )
            {
                Debug.LogError("GameEventQueues::Update::No gui found for event queues to render to, BAIL!");
                return;
            }
            
            // (Devin) Turns out it's kind of hard to raise events for some of the rat stuff because
            //  not all of it is even calculated on client, so we're just going to kind of fake it here...
            var allPlayers = CurrentGame.AllPlayers;
            foreach (var player in allPlayers)
            {
                if (!CurrentGame.TryGetRatBuffer(player, out var rats))
                    continue;
                
                int ratDelta = 0;
                if (m_RatCounts.ContainsKey(player))
                {
                    if (m_RatCounts[player] != rats.Length)
                    {
                        ratDelta = rats.Length - m_RatCounts[player];
                        m_RatCounts[player] = rats.Length;
                    }
                }
                else
                {
                    ratDelta = rats.Length;
                    m_RatCounts.Add(player, rats.Length);
                }

                if (ratDelta == 0)
                    continue;

                var thisPlayer = CurrentGame.ThisPlayer;
                var isThisPlayer = thisPlayer == player;
                if (!CurrentGame.TryGetTransform(player, out var playerTf))
                {
                    Debug.LogWarning("something went wrong trying to fetch player position");
                    continue;
                }

                var ratTf = playerTf;
                if (rats.Length > 0 && !CurrentGame.TryGetTransform(rats[^1].Follower, out ratTf))
                {
                    Debug.LogWarning("something went wrong trying to fetch rat position");
                    continue;
                }
                if (ratDelta > 0)
                {
                    
                    RatsPickedUp.Enqueue(new RatPickedUp()
                    {
                        IsThisPlayer = isThisPlayer,
                        PlayerPosition = playerTf.Position,
                        RatPosition = ratTf.Position
                    });
                }
                else
                {
                    RatsThrown.Enqueue(new RatThrown()
                    {
                        IsThisPlayer = isThisPlayer,
                        Direction = playerTf.Rotation,
                        Position = playerTf.Position
                    });
                }
            }

            while (RatsScored.Count > 0)
            {
                PendingRatScored score = RatsScored.Dequeue();
                Instantiate(scoreRatSfx, score.ReceptacleCenter, Quaternion.identity);

                gui.UpdateScore();
            }

            while (RatsPickedUp.Count > 0)
            {
                RatPickedUp pickup = RatsPickedUp.Dequeue();
                Instantiate(ratPickupSfx, pickup.RatPosition, Quaternion.identity);

                if (pickup.IsThisPlayer)
                    gui.AddRat();
            }

            while (RatsThrown.Count > 0)
            {
                RatThrown thrownRat = RatsThrown.Dequeue();
                Instantiate(ratThrowSfx, thrownRat.Position, Quaternion.identity);

                if (thrownRat.IsThisPlayer) 
                    gui.RemoveRat();
            }
        }

        private IEnumerator TestEvents()
        {
            yield return new WaitForSeconds(3);
            RatsPickedUp.Enqueue(new RatPickedUp());

            yield return new WaitForSeconds(3);
            RatsThrown.Enqueue(new RatThrown());

            yield return new WaitForSeconds(3);
            RatsScored.Enqueue(new PendingRatScored());
        }
    }
}
