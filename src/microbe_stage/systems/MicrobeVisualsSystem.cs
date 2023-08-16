namespace Systems
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Generates the visuals needed for microbes. Handles the membrane and organelle graphics.
    /// </summary>
    [With(typeof(OrganelleContainer))]
    [With(typeof(CellProperties))]
    [With(typeof(SpatialInstance))]
    [With(typeof(EntityMaterial))]
    public sealed class MicrobeVisualsSystem : AEntitySetSystem<float>
    {
        private Lazy<PackedScene> membraneScene =
            new(() => GD.Load<PackedScene>("res://src/microbe_stage/Membrane.tscn"));

        // TODO: implement membrane background generation
        private Dictionary<Entity, Membrane> generatedMembranes;

        private uint membraneGenerationRequestNumber;

        public MicrobeVisualsSystem(World world, IParallelRunner runner) : base(world, runner)
        {
        }

        protected override void PreUpdate(float state)
        {
            base.PreUpdate(state);

            ;
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var organelleContainer = ref entity.Get<OrganelleContainer>();

            if (organelleContainer.OrganelleVisualsCreated)
                return;

            // Skip if no organelle data
            if (organelleContainer.Organelles == null)
            {
                GD.PrintErr("Missing organelles list for MicrobeVisualsSystem");
                return;
            }

            ref var cellProperties = ref entity.Get<CellProperties>();

            // TODO: background thread membrane generation

            ref var spatialInstance = ref entity.Get<SpatialInstance>();
            spatialInstance.GraphicalInstance?.QueueFree();
            spatialInstance.GraphicalInstance = new Spatial();

            ref var materialStorage = ref entity.Get<EntityMaterial>();

            // TODO: remove if this approach isn't used
            // var createdMaterials = new ShaderMaterial[1];

            // TODO: only recreate membrane entirely if missing
            var membrane = membraneScene.Value.Instance<Membrane>();
            ++membraneGenerationRequestNumber;

            membrane.OrganellePositions = organelleContainer.Organelles.Select(o =>
            {
                var pos = Hex.AxialToCartesian(o.Position);
                return new Vector2(pos.x, pos.z);
            }).ToList();

            membrane.Type = cellProperties.MembraneType;
            membrane.WigglyNess = cellProperties.MembraneType.BaseWigglyness;
            membrane.MovementWigglyNess = cellProperties.MembraneType.MovementWigglyness;

            spatialInstance.GraphicalInstance.AddChild(membrane);

            // Material is initialized in _Ready so this is after AddChild
            materialStorage.Material =
                membrane.MaterialToEdit ?? throw new Exception("Membrane didn't set material to edit");

            // TODO: health value applying

            // TODO: colour control, like with the microbe shader system or another one based on the colour as the
            // flash animations need to work

            // TODO: organelle visuals

            organelleContainer.OrganelleVisualsCreated = true;
        }

        protected override void PostUpdate(float state)
        {
            base.PostUpdate(state);

            // Clear any ready resources that weren't required to not keep them forever
        }
    }
}
