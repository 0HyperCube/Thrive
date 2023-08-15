using DefaultEcs.Threading;
using Godot;
using Newtonsoft.Json;
using Systems;

/// <summary>
///   Contains all the parts needed to simulate a microbial world. Separate from (but used by) the
///   <see cref="MicrobeStage"/> to also allow other parts of the code to easily run a microbe simulation
/// </summary>
public class MicrobeWorldSimulation : WorldSimulationWithPhysics
{
    private readonly IParallelRunner nonParallelRunner = new DefaultParallelRunner(1);

    private GameProperties gameProperties = null!;

    private FluidCurrentsSystem fluidCurrentsSystem = null!;
    private MicrobeAISystem microbeAI = null!;

#pragma warning disable CA2213
    private Node visualsParent = null!;
#pragma warning restore CA2213

    // External system references

    [JsonIgnore]
    public CompoundCloudSystem CloudSystem { get; private set; } = null!;

    // TODO: check that
    [JsonProperty]
    [AssignOnlyChildItemsOnDeserialize]
    public SpawnSystem SpawnSystem { get; private set; } = null!;

    [JsonIgnore]
    public ProcessSystem ProcessSystem { get; private set; } = null!;

    [JsonIgnore]
    public TimedLifeSystem TimedLifeSystem { get; private set; } = null!;

    /// <summary>
    ///   First initialization step which creates all the system objects. When loading from a save objects of this
    ///   type should have <see cref="AssignOnlyChildItemsOnDeserializeAttribute"/> and this method should be called
    ///   before those child properties are loaded.
    /// </summary>
    /// <param name="visualDisplayRoot">Godot Node to place all simulation graphics underneath</param>
    /// <param name="cloudSystem">
    ///   Compound cloud simulation system. This method will call <see cref="CompoundCloudSystem.Init"/>
    /// </param>
    public void Init(Node visualDisplayRoot, CompoundCloudSystem cloudSystem)
    {
        visualsParent = visualDisplayRoot;

        // TODO: add threading
        var parallelRunner = new DefaultParallelRunner(1);
        fluidCurrentsSystem = new FluidCurrentsSystem(EntitySystem, parallelRunner);

        CloudSystem = cloudSystem;
        cloudSystem.Init(fluidCurrentsSystem);

        // TODO: this definitely needs to be (along with the process system) the first systems to be multithreaded
        microbeAI = new MicrobeAISystem(this, cloudSystem, EntitySystem, parallelRunner);

        ProcessSystem = new ProcessSystem(EntitySystem, parallelRunner);

        TimedLifeSystem = new TimedLifeSystem(this, EntitySystem, parallelRunner);

        SpawnSystem = new SpawnSystem(this);

        OnInitialized();
    }

    /// <summary>
    ///   Second phase initialization that requires access to the current game info
    /// </summary>
    /// <param name="currentGame">Currently started game</param>
    public void InitForCurrentGame(GameProperties currentGame)
    {
        gameProperties = currentGame;
    }

    public override void ProcessFrameLogic(float delta)
    {
        ThrowIfNotInitialized();
    }

    public void SetSimulationBiome(BiomeConditions biomeConditions)
    {
        ProcessSystem.SetBiome(biomeConditions);
    }

    internal void OverrideMicrobeAIRandomSeed(int seed)
    {
        microbeAI.OverrideAIRandomSeed(seed);
    }

    protected override void OnProcessFixedLogic(float delta)
    {
        fluidCurrentsSystem.Update(delta);

        ProcessSystem.Update(delta);

        TimedLifeSystem.Update(delta);

        if (RunAI)
        {
            // Update AI for the cells (note that the AI system itself can also be disabled, due to cheats)
            microbeAI.Update(delta);
        }

        SpawnSystem.Update(delta);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            nonParallelRunner.Dispose();
            fluidCurrentsSystem.Dispose();
            microbeAI.Dispose();
            ProcessSystem.Dispose();
            TimedLifeSystem.Dispose();
            SpawnSystem.Dispose();
        }

        base.Dispose(disposing);
    }
}
