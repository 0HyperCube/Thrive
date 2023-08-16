namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;

    [With(typeof(MicrobeShaderParameters))]
    [With(typeof(EntityMaterial))]
    public sealed class MicrobePhysicsSystem : AEntitySetSystem<float>
    {
        public MicrobePhysicsSystem(World world, IParallelRunner runner) : base(world, runner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var shaderParameters = ref entity.Get<MicrobeShaderParameters>();

            if (shaderParameters.ParametersApplied && !shaderParameters.PlayAnimations)
                return;
        }
    }
}
