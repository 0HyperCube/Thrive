using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

public class FloatingChunk
{
#pragma warning disable CA2213
    private MeshInstance? chunkMesh;
    private Particles? particles;
#pragma warning restore CA2213

    [JsonProperty]
    private bool isDissolving;

    [JsonProperty]
    private bool isFadingParticles;

    [JsonProperty]
    private float particleFadeTimer;

    [JsonProperty]
    private float dissolveEffectValue;

    [JsonProperty]
    private int renderPriority;

    [JsonProperty]
    private float engulfSize;

    public int DespawnRadiusSquared { get; set; }

    [JsonIgnore]
    public bool DisallowDespawning => false;

    [JsonIgnore]
    public float EntityWeight => 1000.0f;

    [JsonIgnore]
    public GeometryInstance EntityGraphics
    {
        get
        {
            if (chunkMesh != null)
                return chunkMesh;

            if (particles != null)
                return particles;

            throw new InstanceNotLoadedYetException();
        }
    }

    [JsonIgnore]
    public Spatial VisualNode { get; private set; } = new();

    [JsonIgnore]
    public int RenderPriority
    {
        get => renderPriority;
        set
        {
            renderPriority = value;
            ApplyRenderPriority();
        }
    }

    /// <summary>
    ///   If true this chunk is destroyed when all compounds are vented
    /// </summary>
    public bool Dissolves { get; set; }

    /// <summary>
    ///   When true, the chunk will despawn when the despawn timer finishes
    /// </summary>
    public bool UsesDespawnTimer { get; set; }

    /// <summary>
    ///   How much time has passed since a chunk that uses this timer has been spawned
    /// </summary>
    [JsonProperty]
    public float DespawnTimer { get; private set; }

    /// <summary>
    ///   This is both the digestion and dissolve effect progress value for now.
    /// </summary>
    [JsonIgnore]
    public float DigestedAmount
    {
        get => dissolveEffectValue;
        set
        {
            dissolveEffectValue = Mathf.Clamp(value, 0.0f, 1.0f);
            UpdateDissolveEffect();
        }
    }

    /// <summary>
    ///   Processes this chunk
    /// </summary>
    /// <returns>True if this wants to be destroyed</returns>
    public bool ProcessChunk(float delta, CompoundCloudSystem compoundClouds)
    {
        if (PhagocytosisStep != PhagocytosisPhase.None)
            return false;

        if (isDissolving)
        {
            if (HandleDissolving(delta))
            {
                return true;
            }
        }

        if (isFadingParticles)
        {
            particleFadeTimer -= delta;

            if (particleFadeTimer <= 0)
            {
                return true;
            }
        }

        VentCompounds(elapsedSinceProcess, compoundClouds);

        if (UsesDespawnTimer)
            DespawnTimer += elapsedSinceProcess;

        // Check contacts
        foreach (var microbe in touchingMicrobes)
        {
            // TODO: is it possible that this throws the disposed exception?
            if (microbe.Dead)
                continue;

            // Damage
            if (Damages > 0)
            {
                if (DeleteOnTouch)
                {
                    microbe.Damage(Damages, DamageType);
                }
                else
                {
                    microbe.Damage(Damages * elapsedSinceProcess, DamageType);
                }
            }

            if (DeleteOnTouch)
            {
                if (DissolveOrRemove())
                {
                    return true;
                }

                break;
            }
        }

        if (DespawnTimer > Constants.DESPAWNING_CHUNK_LIFETIME)
        {
            VentAllCompounds(compoundClouds);
            if (DissolveOrRemove())
            {
                return true;
            }
        }

        elapsedSinceProcess = 0;
        return false;
    }

    /// <summary>
    ///   Handles the dissolving effect for the chunks when they run out of compounds.
    /// </summary>
    private bool HandleDissolving(float delta)
    {
        if (chunkMesh == null)
            throw new InvalidOperationException("Chunk without a mesh can't dissolve");

        if (PhagocytosisStep != PhagocytosisPhase.None)
            return false;

        // Disable collisions
        DisableAllCollisions();

        DigestedAmount += delta * Constants.FLOATING_CHUNKS_DISSOLVE_SPEED;

        if (DigestedAmount >= Constants.FULLY_DIGESTED_LIMIT)
        {
            return true;
        }

        return false;
    }

    private void UpdateDissolveEffect()
    {
        if (chunkMesh == null)
            throw new InvalidOperationException("Chunk without a mesh can't dissolve");

        if (chunkMesh.MaterialOverride is ShaderMaterial material)
            material.SetShaderParam("dissolveValue", dissolveEffectValue);
    }

    private void ApplyRenderPriority()
    {
        if (chunkMesh == null)
            throw new InvalidOperationException("Chunk without a mesh can't be applied a render priority");

        chunkMesh.MaterialOverride.RenderPriority = RenderPriority;
    }

    private void OnContactBegin(NativePhysicsBody physicsBody, int collidedSubShapeDataOurs, int bodyShape)
    {
        throw new NotImplementedException();

        /*_ = bodyID;
        _ = localShape;

        if (body is Microbe microbe)
        {
            // Can't engulf with a pilus
            if (microbe.IsPilus(microbe.ShapeFindOwner(bodyShape)))
                return;

            var target = microbe.GetMicrobeFromShape(bodyShape);
            if (target != null)
                touchingMicrobes.Add(target);
        }*/
    }

    private void OnContactEnd(int bodyID, Node body, int bodyShape, int localShape)
    {
        throw new NotImplementedException();
        /*_ = bodyID;
        _ = localShape;

        if (body is Microbe microbe)
        {
            var shapeOwner = microbe.ShapeFindOwner(bodyShape);

            // This can happen when a microbe unbinds while also touching a floating chunk
            // TODO: Do something more elegant to stop the error messages in the log
            if (shapeOwner == 0)
            {
                touchingMicrobes.Remove(microbe);
                return;
            }

            // This might help in a case where the cell is touching with both a pilus and non-pilus part
            if (microbe.IsPilus(shapeOwner))
                return;

            var target = microbe.GetMicrobeFromShape(bodyShape);

            if (target != null)
                touchingMicrobes.Remove(target);
        }*/
    }

    private bool DissolveOrRemove()
    {
        if (Dissolves)
        {
            isDissolving = true;
        }
        else if (particles != null && !isFadingParticles)
        {
            isFadingParticles = true;

            DisableAllCollisions();

            particles.Emitting = false;
            particleFadeTimer = particles.Lifetime;
        }
        else if (particles == null)
        {
            return true;
        }

        return false;
    }
}
