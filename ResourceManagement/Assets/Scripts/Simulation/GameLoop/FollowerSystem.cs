using Presentation;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Simulation
{
    // TODO? Run this locally instead of on the Server
    //  Location of rat followers is only really relevant for the presentation layer. If we can figure out how to 
    //  replicate down only the spawn location, clients can run all the follower code locally and not rely on the Server
    //  to constantly be updating these transforms
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct FollowerSystem : ISystem
    {
        static readonly float3 up = new float3(0f, 1, 0);
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            //var ecb = new EntityCommandBuffer(Allocator.Temp);
            /* To simulate collisions with movement we could store all *pickedUp* rat's locations 
             * in an array of [maxThrowableFollower] size. For each subsequent rat we can check
             * if the distance between it's transform and any transforms in the array are less
             * than [tbd] units away. For all "colliding" transforms, calculate the normal
             * vector and move [tbd] units in that direction
             */

            /* There may be jitter depending on the order of processing rat locations and if
             * the array locations should be persistent or ephemeral
             */
            /* define temp empty array to hold locations of throwable rats */                           /* (unsure how to decide length of array)  arbitrarily set to 15? */
            var locationsOfPickedUpRats = new float3[] { new float3(1f, 0, 1), new float3(1f, 0, 1), new float3(1f, 0, 1), new float3(1f, 0, 1), new float3(1f, 0, 1), new float3(1f, 0, 1), new float3(1f, 0, 1), new float3(1f, 0, 1), new float3(1f, 0, 1), new float3(1f, 0, 1), new float3(1f, 0, 1), new float3(1f, 0, 1), new float3(1f, 0, 1), new float3(1f, 0, 1), new float3(1f, 0, 1), new float3(1f, 0, 1) };
            // set location values to (1,0,1) initially
            // check if click input was registered from input script
            var receiveInputRightClick = (Input.GetMouseButtonDown(1)); //GameObject.Find("").GetComponent<inputclassname>().passInputToFollowerSystem;
            foreach (var (tf, pickUp, follower) in SystemAPI
                         .Query<RefRW<LocalTransform>, RefRO<Ownership>, RefRW<Follower>>()
                         .WithNone<ConvertToProjectile, NeedsOwnerAssignment>())
            {
                if (!pickUp.ValueRO.HasConfiguredOwner)
                {
                    receiveInputRightClick = false;
                    continue;
                }

                
                /* if throwableRatArray[i] is within goaldistance of rat */
                        /* append throwableRatArray[i] to throwableCollidingRatArray[] */
                        /* set throwableRatArray[OwnerQueueRank] to targetTf */
                        /* travelVector = direction * follower.ValueRO.Speed * deltaTime; */
                        /* normalize vector */
                        /* add distance */

                /* transform of owner */
                var targetTf = SystemAPI.GetComponent<LocalTransform>(pickUp.ValueRO.Owner);
                var ownerData = state.EntityManager.GetComponentData<CharacterFollowerThrowing>(pickUp.ValueRO.Owner);
                /*  current number of rats - ? + total lifetime thrown rats) */
                // OwnerQueueRank = NumThrowableFollowers + NumThrownFollowers;     from PickUpOwnerAssignmentSystem.cs
                // -OwnerQueueRank + NumThrownFollowers = -NumThrowableFollowers;  rearranging above equation yields this
                var placeInLine = ownerData.NumThrowableFollowers - (follower.ValueRO.OwnerQueueRank - ownerData.NumThrownFollowers);
                // if both above are true. placeInLine should be    placeInLine = NumThrowableFollowers - (OwnerQueueRank - NumThrownFollowers)
                //                                                  placeInLine = NumThrowableFollowers - OwnerQueueRank + NumThrownFollowers)
                //                                                  placeInLine = NumThrowableFollowers + (-OwnerQueueRank + NumThrownFollowers)
                //                                                  placeInLine = NumThrowableFollowers + (-NumThrowableFollowers)
                //                                                  placeInLine = 0
                if (receiveInputRightClick)
                    Debug.Log("place in line: " + placeInLine);
                var goalDistance = follower.ValueRO.GoalDistance + placeInLine * 1.2f;
                // goalDistance is directly correlated with placeInLine
                // placeInLine is inversely correlated with OwnerQueueRank
                // OwnerQueueRank value is lower for newly picked up rats
                //      are newly picked up rats close or far?
                //      which rats are thrown first? 
                /* define normalVector of nearby rats for multiplication later */
                var desiredNormalVectorToCollidingRats = new float3(0f, 0, 0);
                /* iterate through throwableRatArray[] for locations of previously foreach'd rats */
                for (int l = 0; l < locationsOfPickedUpRats.Length; l++)
                {
                    var throwableRatLocation = locationsOfPickedUpRats[l];
                    /* if location of rat is not default */
                    if (!throwableRatLocation.Equals( new float3(1f, 0, 1)))
                    {
                        /* multiply direction vector by -distance to each throwableCollidingRatArray[ ] */
                        if (receiveInputRightClick)
                            Debug.Log("Rat[" + l + "]'s position is  " + throwableRatLocation);
                        var vectorFromThrowableRat = math.normalize(-1 * (throwableRatLocation + tf.ValueRW.Position) );
                        var distanceFromThrowableRat = math.distance((throwableRatLocation), tf.ValueRW.Position);
                        // changing this from 15f to 300f DRASTICALLY changes behavior

                        Debug.Log("sanity check & dist for [" + l + "] = " +distanceFromThrowableRat);
                        if (distanceFromThrowableRat < 2f)
                        {
                            /* multiply direction vector by -distance to each throwableCollidingRatArray[ ] */
                            Debug.Log("test < 0.5 :)");
                            desiredNormalVectorToCollidingRats += vectorFromThrowableRat * 10;
                        }
                        else if (receiveInputRightClick)
                            Debug.Log("distanceFromThrowableRat[" + l + "]<4 && = " + distanceFromThrowableRat);
                    }
                }
                /* save relative location of active rat */       /* this does not account for rat throwing and would need to be cleared onThrow */
                if (receiveInputRightClick)
                    Debug.Log("setting[" + (ownerData.NumThrowableFollowers - (follower.ValueRO.OwnerQueueRank - ownerData.NumThrownFollowers)) + "] to " + tf.ValueRW.Position);
                // Debug.Log(tf.ValueRW.Position);
                Debug.Log("setting[" + (ownerData.NumThrowableFollowers - (follower.ValueRO.OwnerQueueRank - ownerData.NumThrownFollowers)) + "] to " + tf.ValueRW.Position);
                locationsOfPickedUpRats[ownerData.NumThrowableFollowers - (follower.ValueRO.OwnerQueueRank - ownerData.NumThrownFollowers)] = /*targetTf.Position - */tf.ValueRW.Position;


                if (!desiredNormalVectorToCollidingRats.Equals(new float3 (0f, 0, 0)))
                {
                    /* copy logic from existing direction rotation speed and position-translation from below
                     * replacing vectorToPlayer with vectorFromCollidingRatNormal
                     */
                    var normalDirection2 = math.normalize(desiredNormalVectorToCollidingRats);
                    var travelVector2 = normalDirection2 * follower.ValueRO.Speed * deltaTime;
                    travelVector2.y = 0f;
                    tf.ValueRW.Rotation = quaternion.LookRotation(travelVector2, up);
                    tf.ValueRW.Position += travelVector2;
                    follower.ValueRW.CurrentSpeed = follower.ValueRO.Speed;
                    /* this seems to register infrequently when rats clump 
                     * causing all of them to scatter for multiple seconds
                     * before eventually coming back
                     */
                    receiveInputRightClick = false;
                    continue;
                }




                var currentDistance = math.distance(targetTf.Position, tf.ValueRW.Position);

                var direction = math.normalize(targetTf.Position - tf.ValueRW.Position);
                var travelVector = direction * follower.ValueRO.Speed * deltaTime;
                travelVector.y = 0f;
                tf.ValueRW.Rotation = quaternion.LookRotation(travelVector, up);
                if (currentDistance < goalDistance)
                {
                    follower.ValueRW.CurrentSpeed = 0f;
                    receiveInputRightClick = false;
                    continue;
                }
                tf.ValueRW.Position += travelVector;
                follower.ValueRW.CurrentSpeed = follower.ValueRO.Speed;

                // TODO: Figure out how to do this with physics velocities so the rats can collide with each other
                //velocity.ValueRW.Linear = travelVector;
                receiveInputRightClick = false;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct FollowerAnimationSystem : ISystem
    {
        static readonly int k_Speed = Animator.StringToHash("Speed");
        static readonly int k_RollUp = Animator.StringToHash("RollUp");

        public void OnCreate(ref SystemState state)
        {
            
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (animatorLink, follower) in SystemAPI
                         .Query<AnimatorLink, RefRO<Follower>>()
                         .WithNone<ConvertToProjectile, NeedsOwnerAssignment>())
            {
                animatorLink.Animator.SetFloat(k_Speed, follower.ValueRO.CurrentSpeed / follower.ValueRO.Speed);
            }
            // (In theory this is being done in the FollowerThrowingSystem)
            // foreach (var (animatorLink, follower) in SystemAPI
            //              .Query<AnimatorLink, RefRO<Follower>>()
            //              .WithNone<NeedsOwnerAssignment>()
            //              .WithAll<ConvertToProjectile>())
            // {
            //     animatorLink.Animator.SetBool(k_RollUp, true);
            // }
        }
    }
}
