using System;
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

        void Start()
        {
            Instance = this;
            RatsScored = new Queue<PendingRatScored>();
            RatsPickedUp = new Queue<RatPickedUp>();
            RatsThrown = new Queue<RatThrown>();
        }
    }
}
