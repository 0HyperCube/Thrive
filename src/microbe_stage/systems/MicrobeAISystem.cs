namespace Systems
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
        private readonly List<Microbe> AllMicrobes = new();
        private readonly List<Entity> AllChunks = new();

        private Random aiThinkRandomSource = new();

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
            AllMicrobes.Clear();
            AllChunks.Clear();

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

            ref var position = ref entity.Get<WorldPosition>();
            ref var physicsControl = ref entity.Get<ManualPhysicsControl>();

            AIThink(Constants.MICROBE_AI_THINK_INTERVAL, GetNextAIRandom(), ref ai, ref position, ref physicsControl);
        }

        /// <summary>
        ///   Main AI think function for cells
        /// </summary>
        private void AIThink(float delta, Random random, ref MicrobeAI ai, ref WorldPosition selfPosition,
            ref ManualPhysicsControl control)
        {
            ai.PreviouslyAbsorbedCompounds ??= new Dictionary<Compound, float>(microbe.TotalAbsorbedCompounds);
            ai.CompoundsSearchWeights ??= new Dictionary<Compound, float>();

            throw new NotImplementedException();


        }

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
