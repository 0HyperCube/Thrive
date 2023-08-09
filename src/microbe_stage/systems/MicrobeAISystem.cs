﻿namespace Systems
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Microbe AI logic
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Without the attached component here stops this from running for microbes in colonies
    ///   </para>
    /// </remarks>
    [With(typeof(MicrobeAI))]
    [With(typeof(ManualPhysicsControl))]
    [With(typeof(WorldPosition))]
    [Without(typeof(AttachedToEntity))]
    public sealed class MicrobeAISystem : AEntitySetSystem<float>
    {
        private readonly Compound atp;
        private readonly Compound glucose;
        private readonly Compound iron;
        private readonly Compound oxytoxy;
        private readonly Compound ammonia;
        private readonly Compound phosphates;

        private readonly MicrobeWorldSimulation worldSimulation;

        /// <summary>
        ///   Stored random instances for use by the individual AI methods which may run in multiple threads
        /// </summary>
        private readonly List<Random> thinkRandoms = new();

        private readonly IReadonlyCompoundClouds clouds;

        // Cached data about the world given to the AI entities when they are thinking
        private readonly List<Microbe> allMicrobes = new();
        private readonly List<Entity> allChunks = new();

        private Random aiThinkRandomSource = new();

        private bool printedPlayerControlMessage;

        private int usedAIThinkRandomIndex;

        private bool skipAI;

        public MicrobeAISystem(MicrobeWorldSimulation worldSimulation, IReadonlyCompoundClouds cloudSystem, World world,
            IParallelRunner runner) : base(world, runner)
        {
            this.worldSimulation = worldSimulation;
            clouds = cloudSystem;

            atp = SimulationParameters.Instance.GetCompound("atp");
            glucose = SimulationParameters.Instance.GetCompound("glucose");
            iron = SimulationParameters.Instance.GetCompound("iron");
            oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");
            ammonia = SimulationParameters.Instance.GetCompound("ammonia");
            phosphates = SimulationParameters.Instance.GetCompound("phosphates");
        }

        public void OverrideAIRandomSeed(int seed)
        {
            lock (thinkRandoms)
            {
                thinkRandoms.Clear();
                usedAIThinkRandomIndex = 0;

                aiThinkRandomSource = new Random(seed);
            }
        }

        protected override void PreUpdate(float delta)
        {
            base.PreUpdate(delta);

            skipAI = CheatManager.NoAI;
            usedAIThinkRandomIndex = 0;

            // TODO: it would be nice to only rebuild these lists if some AI think interval has elapsed and these are
            // actually needed (could maybe use Lazy here?)
            allMicrobes.Clear();
            allChunks.Clear();

            // TODO: fetch all microbes and chunks

            throw new NotImplementedException();

            // For chunks we filter out chunks already eaten by someone else
            // var allChunks = worldSimulation.Entities.OfType<FloatingChunk>().Where(c => !c.AttachedToAnEntity).ToList();
        }

        protected override void Update(float delta, in Entity entity)
        {
            if (skipAI)
                return;

            ref var ai = ref entity.Get<MicrobeAI>();

            ai.TimeUntilNextThink -= delta;

            if (ai.TimeUntilNextThink > 0)
                return;

            // TODO: would be nice to add a tiny bit of randomness to the times here so that not all cells think at once
            ai.TimeUntilNextThink = Constants.MICROBE_AI_THINK_INTERVAL;

            throw new NotImplementedException();

            // TODO: reimplement the following conditions
            // if (IsPlayerMicrobe)
            // {
            //     if (!printedPlayerControlMessage)
            //     {
            //         GD.Print("AI is controlling the player microbe");
            //         printedPlayerControlMessage = true;
            //     }
            // }
            //
            // if (Dead || IsForPreviewOnly || PhagocytosisStep != PhagocytosisPhase.None)
            //     return;

            ref var position = ref entity.Get<WorldPosition>();
            ref var physicsControl = ref entity.Get<ManualPhysicsControl>();

            try
            {
                AIThink(Constants.MICROBE_AI_THINK_INTERVAL, GetNextAIRandom(), ref ai, ref position,
                    ref physicsControl);
            }
#pragma warning disable CA1031 // AI needs to be boxed good
            catch (Exception e)
#pragma warning restore CA1031
            {
                // TODO: the AI doesn't seem as bad of a crash source as it used to be so the catch above is probably
                // fine to remove
                GD.PrintErr("Microbe AI failure! ", e);
            }
        }

        /// <summary>
        ///   Main AI think function for cells
        /// </summary>
        private void AIThink(float delta, Random random, ref MicrobeAI ai, ref WorldPosition selfPosition,
            ref ManualPhysicsControl control)
        {
            // TODO: reimplement microbe total absorption tracking
            throw new NotImplementedException();

            // ai.PreviouslyAbsorbedCompounds ??= new Dictionary<Compound, float>(microbe.TotalAbsorbedCompounds);

            ai.CompoundsSearchWeights ??= new Dictionary<Compound, float>();

            ai.TimeSinceSignalSniffing += delta;

            if (ai.TimeSinceSignalSniffing > Constants.MICROBE_AI_SIGNAL_REACT_INTERVAL)
            {
                ai.TimeSinceSignalSniffing = 0;

                throw new NotImplementedException();

                // if (microbe.HasSignalingAgent)
                //     DetectSignalingAgents(data.AllMicrobes.Where(m => m.Species == microbe.Species));
            }

            var signaler = ai.LastFoundSignalEmitter;

            if (signaler != default)
            {
                throw new NotImplementedException();

                // ai.ReceivedCommand = signaler.SignalCommand;
            }

            throw new NotImplementedException();

            // ChooseActions(random, data, signaler);

            // Store the absorbed compounds for run and rumble

            throw new NotImplementedException();

            // PreviouslyAbsorbedCompounds.Clear();

            throw new NotImplementedException();

            // foreach (var compound in microbe.TotalAbsorbedCompounds)
            // {
            //     ai.PreviouslyAbsorbedCompounds[compound.Key] = compound.Value;
            // }

            // We clear here for update, this is why we stored above!

            throw new NotImplementedException();

            // microbe.TotalAbsorbedCompounds.Clear();
        }

        //
        // TODO: make the following code moved from MicrobeAI work again
        //
        /*private float SpeciesAggression => microbe.Species.Behaviour.Aggression *
            (ReceivedCommand == MicrobeSignalCommand.BecomeAggressive ? 1.5f : 1.0f);

        private float SpeciesFear => microbe.Species.Behaviour.Fear *
            (ReceivedCommand == MicrobeSignalCommand.BecomeAggressive ? 0.75f : 1.0f);

        private float SpeciesActivity => microbe.Species.Behaviour.Activity *
            (ReceivedCommand == MicrobeSignalCommand.BecomeAggressive ? 1.25f : 1.0f);

        private float SpeciesFocus => microbe.Species.Behaviour.Focus;
        private float SpeciesOpportunism => microbe.Species.Behaviour.Opportunism;

        private bool IsSessile => SpeciesActivity < Constants.MAX_SPECIES_ACTIVITY / 10;*/

        /*private void ChooseActions(Random random, MicrobeAICommonData data, Microbe? signaler)
        {
            // If nothing is engulfing me right now, see if there's something that might want to hunt me
            // TODO: https://github.com/Revolutionary-Games/Thrive/issues/2323
            Vector3? predator = GetNearestPredatorItem(data.AllMicrobes)?.GlobalTransform.origin;
            if (predator.HasValue &&
                DistanceFromMe(predator.Value) < (1500.0 * SpeciesFear / Constants.MAX_SPECIES_FEAR))
            {
                FleeFromPredators(random, predator.Value);
                return;
            }

            // If this microbe is out of ATP, pick an amount of time to rest
            if (microbe.Compounds.GetCompoundAmount(atp) < 1.0f)
            {
                // Keep the maximum at 95% full, as there is flickering when near full
                ATPThreshold = 0.95f * SpeciesFocus / Constants.MAX_SPECIES_FOCUS;
            }

            if (ATPThreshold > 0.0f)
            {
                if (microbe.Compounds.GetCompoundAmount(atp) < microbe.Compounds.Capacity * ATPThreshold
                    && microbe.Compounds.Any(compound => IsVitalCompound(compound.Key) && compound.Value > 0.0f))
                {
                    SetMoveSpeed(0.0f);
                    return;
                }

                ATPThreshold = 0.0f;
            }

            // Follow received commands if we have them
            // TODO: tweak the balance between following commands and doing normal behaviours
            // TODO: and also probably we want to add some randomness to the positions and speeds based on distance
            switch (ReceivedCommand)
            {
                case MicrobeSignalCommand.MoveToMe:
                    if (signaler != null)
                    {
                        MoveToLocation(signaler.Translation);
                        return;
                    }

                    break;
                case MicrobeSignalCommand.FollowMe:
                    if (signaler != null && DistanceFromMe(signaler.Translation) > Constants.AI_FOLLOW_DISTANCE_SQUARED)
                    {
                        MoveToLocation(signaler.Translation);
                        return;
                    }

                    break;
                case MicrobeSignalCommand.FleeFromMe:
                    if (signaler != null && DistanceFromMe(signaler.Translation) < Constants.AI_FLEE_DISTANCE_SQUARED)
                    {
                        microbe.State = MicrobeState.Normal;
                        SetMoveSpeed(Constants.AI_BASE_MOVEMENT);

                        // Direction is calculated to be the opposite from where we should flee
                        TargetPosition = microbe.Translation + (microbe.Translation - signaler.Translation);
                        microbe.LookAtPoint = TargetPosition;
                        SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
                        return;
                    }

                    break;
            }

            // If I'm very far from the player, and I have not been near the player yet, get on stage
            if (!HasBeenNearPlayer)
            {
                var player = data.AllMicrobes.Where(otherMicrobe => otherMicrobe.IsPlayerMicrobe).FirstOrDefault();
                if (player != null)
                {
                    // Only move if we aren't sessile
                    if (DistanceFromMe(player.GlobalTransform.origin) >
                        Math.Pow(Constants.SPAWN_SECTOR_SIZE, 2) * 0.75f &&
                        !IsSessile)
                    {
                        MoveToLocation(player.GlobalTransform.origin);
                        return;
                    }

                    HasBeenNearPlayer = true;
                }
            }

            // If there are no threats, look for a chunk to eat
            // TODO: still consider engulfing things if we're in a colony that can engulf (has engulfer cells)
            if (microbe.CanEngulf)
            {
                var targetChunk = GetNearestChunkItem(data.AllChunks, data.AllMicrobes, random);
                if (targetChunk != null && targetChunk.PhagocytosisStep == PhagocytosisPhase.None)
                {
                    throw new NotImplementedException();

                    // PursueAndConsumeChunks(targetChunk.Translation, random);
                    return;
                }
            }

            // If there are no chunks, look for living prey to hunt
            var possiblePrey = GetNearestPreyItem(data.AllMicrobes);
            if (possiblePrey != null && possiblePrey.PhagocytosisStep == PhagocytosisPhase.None)
            {
                var prey = possiblePrey.GlobalTransform.origin;

                bool engulfPrey = microbe.CanEngulfObject(possiblePrey) == Microbe.EngulfCheckResult.Ok &&
                    DistanceFromMe(possiblePrey.GlobalTransform.origin) < 10.0f * microbe.EngulfSize;

                EngagePrey(prey, random, engulfPrey);
                return;
            }

            // There is no reason to be engulfing at this stage
            microbe.State = MicrobeState.Normal;

            // Otherwise just wander around and look for compounds
            if (!IsSessile)
            {
                SeekCompounds(random, data);
            }
            else
            {
                // This organism is sessile, and will not act until the environment changes
                SetMoveSpeed(0.0f);
            }
        }

        private FloatingChunk? GetNearestChunkItem(List<FloatingChunk> allChunks, List<Microbe> allMicrobes,
            Random random)
        {
            FloatingChunk? chosenChunk = null;

            // If the microbe cannot absorb, no need for this
            // TODO: still consider engulfing things if we're in a colony that can engulf (has engulfer cells)
            if (!microbe.CanEngulf)
            {
                return null;
            }

            // Retrieve nearest potential chunk
            foreach (var chunk in allChunks)
            {
                if (chunk.Compounds.Compounds.Count <= 0)
                    continue;

                throw new NotImplementedException();

                /*if (microbe.EngulfSize > chunk.EngulfSize * Constants.ENGULF_SIZE_RATIO_REQ
                    && (chunk.Translation - microbe.Translation).LengthSquared()
                    <= (20000.0 * SpeciesFocus / Constants.MAX_SPECIES_FOCUS) + 1500.0
                    && chunk.PhagocytosisStep == PhagocytosisPhase.None)
                {
                    if (chunk.Compounds.Compounds.Any(x => microbe.Compounds.IsUseful(x.Key) && x.Key.Digestible))
                    {
                        if (chosenChunk == null ||
                            (chosenChunk.Translation - microbe.Translation).LengthSquared() >
                            (chunk.Translation - microbe.Translation).LengthSquared())
                        {
                            chosenChunk = chunk;
                        }
                    }
                }#1#
            }

            // Don't bother with chunks when there's a lot of microbes to compete with
            if (chosenChunk != null)
            {
                throw new NotImplementedException();

                /*var rivals = 0;
                var distanceToChunk = (microbe.Translation - chosenChunk.Translation).LengthSquared();
                foreach (var rival in allMicrobes)
                {
                    if (rival != microbe)
                    {
                        var rivalDistance = (rival.GlobalTransform.origin - chosenChunk.Translation).LengthSquared();
                        if (rivalDistance < 500.0f &&
                            rivalDistance < distanceToChunk)
                        {
                            rivals++;
                        }
                    }
                }#1#

                int rivalThreshold;
                if (SpeciesOpportunism < Constants.MAX_SPECIES_OPPORTUNISM / 3)
                {
                    rivalThreshold = 1;
                }
                else if (SpeciesOpportunism < Constants.MAX_SPECIES_OPPORTUNISM * 2 / 3)
                {
                    rivalThreshold = 3;
                }
                else
                {
                    rivalThreshold = 5;
                }

                // In rare instances, microbes will choose to be much more ambitious
                if (RollCheck(SpeciesFocus, Constants.MAX_SPECIES_FOCUS, random))
                {
                    rivalThreshold *= 2;
                }

                throw new NotImplementedException();

                // if (rivals > rivalThreshold)
                // {
                //     chosenChunk = null;
                // }
            }

            return chosenChunk;
        }

        /// <summary>
        ///   Gets the nearest prey item. And builds the prey list
        /// </summary>
        /// <returns>The nearest prey item.</returns>
        /// <param name="allMicrobes">All microbes.</param>
        private Microbe? GetNearestPreyItem(List<Microbe> allMicrobes)
        {
            var focused = FocusedPrey.Value;
            if (focused != null)
            {
                var distanceToFocusedPrey = DistanceFromMe(focused.GlobalTransform.origin);
                if (!focused.Dead && focused.PhagocytosisStep == PhagocytosisPhase.None && distanceToFocusedPrey <
                    (3500.0f * SpeciesFocus / Constants.MAX_SPECIES_FOCUS))
                {
                    if (distanceToFocusedPrey < PursuitThreshold)
                    {
                        // Keep chasing, but expect to keep getting closer
                        LowerPursuitThreshold();
                        return focused;
                    }

                    // If prey hasn't gotten closer by now, it's probably too fast, or juking you
                    // Remember who focused prey is, so that you don't fall for this again
                    return null;
                }

                FocusedPrey.Value = null;
            }

            Microbe? chosenPrey = null;

            foreach (var otherMicrobe in allMicrobes)
            {
                if (!otherMicrobe.Dead && otherMicrobe.PhagocytosisStep == PhagocytosisPhase.None)
                {
                    if (DistanceFromMe(otherMicrobe.GlobalTransform.origin) <
                        (2500.0f * SpeciesAggression / Constants.MAX_SPECIES_AGGRESSION)
                        && CanTryToEatMicrobe(otherMicrobe))
                    {
                        if (chosenPrey == null ||
                            (chosenPrey.GlobalTransform.origin - microbe.Translation).LengthSquared() >
                            (otherMicrobe.GlobalTransform.origin - microbe.Translation).LengthSquared())
                        {
                            chosenPrey = otherMicrobe;
                        }
                    }
                }
            }

            FocusedPrey.Value = chosenPrey;

            if (chosenPrey != null)
            {
                PursuitThreshold = DistanceFromMe(chosenPrey.GlobalTransform.origin) * 3.0f;
            }

            return chosenPrey;
        }

        /// <summary>
        ///   Building the predator list and setting the scariest one to be predator
        /// </summary>
        /// <param name="allMicrobes">All microbes.</param>
        private Microbe? GetNearestPredatorItem(List<Microbe> allMicrobes)
        {
            var fleeThreshold = 3.0f - (2 *
                (SpeciesFear / Constants.MAX_SPECIES_FEAR) *
                (10 - (9 * microbe.Hitpoints / microbe.MaxHitpoints)));
            Microbe? predator = null;
            foreach (var otherMicrobe in allMicrobes)
            {
                if (otherMicrobe == microbe)
                    continue;

                // Based on species fear, threshold to be afraid ranges from 0.8 to 1.8 microbe size.
                if (otherMicrobe.Species != microbe.Species
                    && !otherMicrobe.Dead && otherMicrobe.PhagocytosisStep == PhagocytosisPhase.None
                    && otherMicrobe.EngulfSize > microbe.EngulfSize * fleeThreshold)
                {
                    if (predator == null || DistanceFromMe(predator.GlobalTransform.origin) >
                        DistanceFromMe(otherMicrobe.GlobalTransform.origin))
                    {
                        predator = otherMicrobe;
                    }
                }
            }

            return predator;
        }

        private void PursueAndConsumeChunks(Vector3 chunk, Random random)
        {
            // This is a slight offset of where the chunk is, to avoid a forward-facing part blocking it
            TargetPosition = chunk + new Vector3(0.5f, 0.0f, 0.5f);
            microbe.LookAtPoint = TargetPosition;
            SetEngulfIfClose();

            // Just in case something is obstructing chunk engulfing, wiggle a little sometimes
            if (random.NextDouble() < 0.05)
            {
                MoveWithRandomTurn(0.1f, 0.2f, random);
            }

            // If this Microbe is right on top of the chunk, stop instead of spinning
            if (DistanceFromMe(chunk) < Constants.AI_ENGULF_STOP_DISTANCE)
            {
                SetMoveSpeed(0.0f);
            }
            else
            {
                SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
            }
        }

        private void FleeFromPredators(Random random, Vector3 predatorLocation)
        {
            microbe.State = MicrobeState.Normal;

            TargetPosition = (2 * (microbe.Translation - predatorLocation)) + microbe.Translation;

            microbe.LookAtPoint = TargetPosition;

            if (DistanceFromMe(predatorLocation) < 100.0f)
            {
                if (microbe.SlimeJets.Count > 0 && RollCheck(SpeciesFear, Constants.MAX_SPECIES_FEAR, random))
                {
                    // There's a chance to jet away if we can
                    SecreteSlime(random);
                }
                else if (RollCheck(SpeciesAggression, Constants.MAX_SPECIES_AGGRESSION, random))
                {
                    // If the predator is right on top of us there's a chance to try and swing with a pilus
                    MoveWithRandomTurn(2.5f, 3.0f, random);
                }
            }

            // If prey is confident enough, it will try and launch toxin at the predator
            if (SpeciesAggression > SpeciesFear &&
                DistanceFromMe(predatorLocation) >
                300.0f - (5.0f * SpeciesAggression) + (6.0f * SpeciesFear) &&
                RollCheck(SpeciesAggression, Constants.MAX_SPECIES_AGGRESSION, random))
            {
                LaunchToxin(predatorLocation);
            }

            // No matter what, I want to make sure I'm moving
            SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
        }

        private void EngagePrey(Vector3 target, Random random, bool engulf)
        {
            microbe.State = engulf ? MicrobeState.Engulf : MicrobeState.Normal;
            TargetPosition = target;
            microbe.LookAtPoint = TargetPosition;
            if (CanShootToxin())
            {
                LaunchToxin(target);

                if (RollCheck(SpeciesAggression, Constants.MAX_SPECIES_AGGRESSION / 5, random))
                {
                    SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
                }
            }
            else
            {
                SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
            }

            // Predators can use slime jets as an ambush mechanism
            if (RollCheck(SpeciesAggression, Constants.MAX_SPECIES_AGGRESSION, random))
            {
                SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
                SecreteSlime(random);
            }
        }

        private void SeekCompounds(Random random, MicrobeAICommonData data)
        {
            // More active species just try to get distance to avoid over-clustering
            if (RollCheck(SpeciesActivity, Constants.MAX_SPECIES_ACTIVITY + (Constants.MAX_SPECIES_ACTIVITY / 2),
                    random))
            {
                SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
                return;
            }

            if (random.Next(Constants.AI_STEPS_PER_SMELL) == 0)
            {
                SmellForCompounds(data);
            }

            // If the AI has smelled a compound (currently only possible with a chemoreceptor), go towards it.
            if (LastSmelledCompoundPosition != null)
            {
                var distance = DistanceFromMe(LastSmelledCompoundPosition.Value);

                // If the compound isn't getting closer, either something else has taken it, or we're stuck
                LowerPursuitThreshold();
                if (distance > PursuitThreshold)
                {
                    LastSmelledCompoundPosition = null;
                    RunAndTumble(random);
                    return;
                }

                if (distance > 3.0f)
                {
                    TargetPosition = LastSmelledCompoundPosition.Value;
                    microbe.LookAtPoint = TargetPosition;
                }
                else
                {
                    SetMoveSpeed(0.0f);
                    SmellForCompounds(data);
                }
            }
            else
            {
                RunAndTumble(random);
            }
        }

        private void SmellForCompounds(MicrobeAICommonData data)
        {
            ComputeCompoundsSearchWeights();

            var detections = microbe.GetDetectedCompounds(data.Clouds)
                .OrderBy(detection => CompoundsSearchWeights.TryGetValue(detection.Compound, out var weight) ?
                    weight :
                    0).ToList();

            if (detections.Count > 0)
            {
                LastSmelledCompoundPosition = detections[0].Target;
                PursuitThreshold = DistanceFromMe(LastSmelledCompoundPosition.Value)
                    * (1 + (SpeciesFocus / Constants.MAX_SPECIES_FOCUS));
            }
            else
            {
                LastSmelledCompoundPosition = null;
            }
        }

        // For doing run and tumble
        /// <summary>
        ///   For doing run and tumble
        /// </summary>
        /// <param name="random">Random values to use</param>
        private void RunAndTumble(Random random)
        {
            // If this microbe is currently stationary, just initialize by moving in a random direction.
            // Used to get newly spawned microbes to move.
            if (microbe.MovementDirection.Length() == 0)
            {
                MoveWithRandomTurn(0, Mathf.Pi, random);
                return;
            }

            // Run and tumble
            // A biased random walk, they turn more if they are picking up less compounds.
            // The scientifically accurate algorithm has been flipped to account for the compound
            // deposits being a lot smaller compared to the microbes
            // https://www.mit.edu/~kardar/teaching/projects/chemotaxis(AndreaSchmidt)/home.htm

            ComputeCompoundsSearchWeights();

            float gradientValue = 0.0f;
            foreach (var compoundWeight in CompoundsSearchWeights)
            {
                // Note this is about absorbed quantities (which is all microbe has access to) not the ones in the clouds.
                // Gradient computation is therefore cell-centered, and might be different for different cells.
                float compoundDifference = 0.0f;

                microbe.TotalAbsorbedCompounds.TryGetValue(compoundWeight.Key, out float quantityAbsorbedThisStep);
                PreviouslyAbsorbedCompounds.TryGetValue(compoundWeight.Key, out float quantityAbsorbedPreviousStep);

                compoundDifference += quantityAbsorbedThisStep - quantityAbsorbedPreviousStep;

                compoundDifference *= compoundWeight.Value;
                gradientValue += compoundDifference;
            }

            // Implement a detection threshold to possibly rule out too tiny variations
            // TODO: possibly include cell capacity correction
            float differenceDetectionThreshold = Constants.AI_GRADIENT_DETECTION_THRESHOLD;

            // If food density is going down, back up and see if there's some more
            if (gradientValue < -differenceDetectionThreshold && random.Next(0, 10) < 9)
            {
                MoveWithRandomTurn(2.5f, 3.0f, random);
            }

            // If there isn't any food here, it's a good idea to keep moving
            if (Math.Abs(gradientValue) <= differenceDetectionThreshold && random.Next(0, 10) < 5)
            {
                MoveWithRandomTurn(0.0f, 0.4f, random);
            }

            // If positive last step you gained compounds, so let's move toward the source
            if (gradientValue > differenceDetectionThreshold)
            {
                // There's a decent chance to turn by 90° to explore gradient
                // 180° is useless since previous position let you absorb less compounds already
                if (random.Next(0, 10) < 4)
                {
                    MoveWithRandomTurn(0.0f, 1.5f, random);
                }
            }
        }

        /// <summary>
        ///   Prioritizing compounds that are stored in lesser quantities.
        ///   If ATP-producing compounds are low (less than half storage capacities),
        ///   non ATP-related compounds are discarded.
        ///   Updates compoundsSearchWeights instance dictionary.
        /// </summary>
        private void ComputeCompoundsSearchWeights()
        {
            IEnumerable<Compound> usefulCompounds = microbe.Compounds.Compounds.Keys;

            // If this microbe lacks vital compounds don't bother with ammonia and phosphate
            if (usefulCompounds.Any(
                    compound => IsVitalCompound(compound) &&
                        microbe.Compounds.GetCompoundAmount(compound) < 0.5f * microbe.Compounds.Capacity))
            {
                usefulCompounds = usefulCompounds.Where(x => x != ammonia && x != phosphates);
            }

            CompoundsSearchWeights.Clear();
            foreach (var compound in usefulCompounds)
            {
                // The priority of a compound is inversely proportional to its availability
                // Should be tweaked with consumption
                var compoundPriority = 1 - microbe.Compounds.GetCompoundAmount(compound) / microbe.Compounds.Capacity;

                CompoundsSearchWeights.Add(compound, compoundPriority);
            }
        }

        /// <summary>
        ///   Tells if a compound is vital to this microbe.
        ///   Vital compounds are *direct* ATP producers
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     TODO: what is used here is a shortcut linked to the current game state: such compounds could be used for
        ///     other processes in future versions
        ///   </para>
        /// </remarks>
        private bool IsVitalCompound(Compound compound)
        {
            // TODO: looking for mucilage should be prevented
            return microbe.Compounds.IsUseful(compound) &&
                (compound == glucose || compound == iron);
        }

        private void SetEngulfIfClose()
        {
            // Turn on engulf mode if close
            // Sometimes "close" is hard to discern since microbes can range from straight lines to circles
            if ((microbe.Translation - TargetPosition).LengthSquared() <= microbe.EngulfSize * 2.0f)
            {
                microbe.State = MicrobeState.Engulf;
            }
            else
            {
                microbe.State = MicrobeState.Normal;
            }
        }

        private void LaunchToxin(Vector3 target)
        {
            if (microbe.Hitpoints > 0 && microbe.AgentVacuoleCount > 0 &&
                (microbe.Translation - target).LengthSquared() <= SpeciesFocus * 10.0f)
            {
                if (CanShootToxin())
                {
                    microbe.LookAtPoint = target;

                    // Hold fire until the target is lined up.
                    if (microbe.FacingDirection().Normalized().AngleTo(microbe.LookAtPoint.Normalized()) <
                        0.1f + SpeciesActivity / (Constants.AI_BASE_TOXIN_SHOOT_ANGLE_PRECISION * SpeciesFocus))
                    {
                        microbe.QueueEmitToxin(oxytoxy);
                    }
                }
            }
        }

        private void SecreteSlime(Random random)
        {
            if (microbe.Hitpoints > 0 && microbe.SlimeJets.Count > 0)
            {
                // Randomise the time spent ejecting slime, from 0 to 3 seconds
                microbe.QueueSecreteSlime(3 * random.NextFloat());
            }
        }

        private void MoveWithRandomTurn(float minTurn, float maxTurn, Random random)
        {
            var turn = random.Next(minTurn, maxTurn);
            if (random.Next(2) == 1)
            {
                turn = -turn;
            }

            var randDist = random.Next(SpeciesActivity, Constants.MAX_SPECIES_ACTIVITY);
            TargetPosition = microbe.Translation
                + new Vector3(Mathf.Cos(PreviousAngle + turn) * randDist,
                    0,
                    Mathf.Sin(PreviousAngle + turn) * randDist);
            PreviousAngle += turn;
            microbe.LookAtPoint = TargetPosition;
            SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
        }

        private void MoveToLocation(Vector3 location)
        {
            microbe.State = MicrobeState.Normal;
            TargetPosition = location;
            microbe.LookAtPoint = TargetPosition;
            SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
        }

        private void DetectSignalingAgents(IEnumerable<Microbe> ownSpeciesMicrobes)
        {
            // We kind of simulate how strong the "smell" of a signal is by finding the closest active signal
            float? closestSignalSquared = null;
            Microbe? selectedMicrobe = null;

            var previous = LastFoundSignalEmitter.Value;

            if (previous != null && previous.SignalCommand != MicrobeSignalCommand.None)
            {
                selectedMicrobe = previous;
                closestSignalSquared = DistanceFromMe(previous.Translation);
            }

            foreach (var speciesMicrobe in ownSpeciesMicrobes)
            {
                if (speciesMicrobe.SignalCommand == MicrobeSignalCommand.None)
                    continue;

                // Don't detect your own signals
                if (speciesMicrobe == microbe)
                    continue;

                var distance = DistanceFromMe(speciesMicrobe.Translation);

                if (closestSignalSquared == null || distance < closestSignalSquared.Value)
                {
                    selectedMicrobe = speciesMicrobe;
                    closestSignalSquared = distance;
                }
            }

            // TODO: should there be a max distance after which the signaling agent is considered to be so weak that it
            // is not detected?

            LastFoundSignalEmitter.Value = selectedMicrobe;
        }

        private void SetMoveSpeed(float speed)
        {
            microbe.MovementDirection = new Vector3(0, 0, -speed);
        }

        private void LowerPursuitThreshold()
        {
            PursuitThreshold *= 0.95f;
        }

        private bool CanTryToEatMicrobe(Microbe targetMicrobe)
        {
            var sizeRatio = microbe.EngulfSize / targetMicrobe.EngulfSize;

            return targetMicrobe.Species != microbe.Species && (
                (SpeciesOpportunism > Constants.MAX_SPECIES_OPPORTUNISM * 0.3f && CanShootToxin())
                || (sizeRatio >= Constants.ENGULF_SIZE_RATIO_REQ));
        }

        private bool CanShootToxin()
        {
            return microbe.Compounds.GetCompoundAmount(oxytoxy) >=
                Constants.MAXIMUM_AGENT_EMISSION_AMOUNT * SpeciesFocus / Constants.MAX_SPECIES_FOCUS;
        }

        private float DistanceFromMe(Vector3 target)
        {
            return (target - microbe.Translation).LengthSquared();
        }

        private bool RollCheck(float ourStat, float dc, Random random)
        {
            return random.Next(0.0f, dc) <= ourStat;
        }

        private bool RollReverseCheck(float ourStat, float dc, Random random)
        {
            return ourStat <= random.Next(0.0f, dc);
        }*/

        private Random GetNextAIRandom()
        {
            lock (thinkRandoms)
            {
                while (usedAIThinkRandomIndex >= thinkRandoms.Count)
                {
                    thinkRandoms.Add(new Random(aiThinkRandomSource.Next()));
                }

                return thinkRandoms[usedAIThinkRandomIndex++];
            }
        }
    }
}