using System;
using System.Collections;
using System.Collections.Generic;
using Simulation;
using UnityEngine;

namespace Presentation
{
    public struct RatPickedUp
    {
        public Vector3 RatPosition;
        public Vector3 PlayerPosition;
    }

    public struct RatThrown
    {
        public Vector3 Position;
        public Quaternion Direction;
    }
    
    public class GameEventQueues : MonoBehaviour
    {
        public static GameEventQueues Instance { get; private set; }

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

                gui.AddRat();
            }

            while (RatsThrown.Count > 0)
            {
                RatThrown thrownRat = RatsThrown.Dequeue();
                Instantiate(ratThrowSfx, thrownRat.Position, Quaternion.identity);

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
