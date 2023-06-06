﻿using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Tests / stress tests the physics system
/// </summary>
public class PhysicsTest : Node
{
    [Export]
    public TestType Type = TestType.Spheres;

    /// <summary>
    ///   Sets MultiMesh position data with a single array assignment. Faster when all of the data has changed, but
    ///   slower when a lot of the data has not changed.
    /// </summary>
    [Export]
    public bool UseSingleVectorMultiMeshUpdate;

    [Export]
    public NodePath? WorldVisualsPath;

    private readonly List<PhysicsBody> allCreatedBodies = new();
    private readonly List<PhysicsBody> sphereBodies = new();

    private readonly List<Spatial> sphereVisuals = new();

#pragma warning disable CA2213
    private Node worldVisuals = null!;
    private MultiMesh? sphereMultiMesh;
    private PhysicalWorld physicalWorld = null!;
#pragma warning restore CA2213

    private float timeSincePhysicsReport;

    public enum TestType
    {
        Spheres,
        SpheresIndividualNodes,
        SpheresGodotPhysics,
    }

    public override void _Ready()
    {
        worldVisuals = GetNode(WorldVisualsPath);

        physicalWorld = PhysicalWorld.Create();
        SetupPhysicsBodies();
    }

    public override void _PhysicsProcess(float delta)
    {
        base._PhysicsProcess(delta);

        if (Type == TestType.SpheresGodotPhysics)
        {
        }
    }

    public override void _Process(float delta)
    {
        UpdateGUI(delta);

        if (Type == TestType.SpheresGodotPhysics)
            return;

        if (!physicalWorld.ProcessPhysics(delta))
            return;

        if (Type == TestType.Spheres)
        {
            // Display the spheres
            if (sphereMultiMesh == null)
            {
                sphereMultiMesh = new MultiMesh
                {
                    Mesh = CreateSphereMesh().Mesh,
                    TransformFormat = MultiMesh.TransformFormatEnum.Transform3d,
                };

                worldVisuals.AddChild(new MultiMeshInstance
                {
                    Multimesh = sphereMultiMesh,
                });
            }

            if (sphereMultiMesh.InstanceCount != sphereBodies.Count)
                sphereMultiMesh.InstanceCount = sphereBodies.Count;

            var count = sphereBodies.Count;

            if (!UseSingleVectorMultiMeshUpdate)
            {
                for (int i = 0; i < count; ++i)
                {
                    sphereMultiMesh.SetInstanceTransform(i, physicalWorld.ReadBodyTransform(sphereBodies[i]));
                }
            }
            else
            {
                var transformData = new Vector3[count * 4];

                for (int i = 0; i < count; ++i)
                {
                    var transform = physicalWorld.ReadBodyTransform(sphereBodies[i]);

                    transformData[i * 4] = transform[0];
                    transformData[i * 4 + 1] = transform[1];
                    transformData[i * 4 + 2] = transform[2];
                    transformData[i * 4 + 3] = transform[3];
                }

                sphereMultiMesh.TransformArray = transformData;
            }
        }
        else if (Type == TestType.SpheresIndividualNodes)
        {
            // To not completely destroy things we need to generate the shape once
            var sphereVisual = new Lazy<Mesh>(() => CreateSphereMesh().Mesh);

            var count = sphereBodies.Count;
            for (int i = 0; i < count; ++i)
            {
                if (i >= sphereVisuals.Count)
                {
                    var sphere = new MeshInstance
                    {
                        Mesh = sphereVisual.Value,
                    };

                    sphere.Transform = physicalWorld.ReadBodyTransform(sphereBodies[i]);
                    worldVisuals.AddChild(sphere);
                    sphereVisuals.Add(sphere);
                }
                else
                {
                    sphereVisuals[i].Transform = physicalWorld.ReadBodyTransform(sphereBodies[i]);
                }
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var body in allCreatedBodies)
            {
                physicalWorld.DestroyBody(body);
            }

            allCreatedBodies.Clear();

            if (WorldVisualsPath != null)
            {
                WorldVisualsPath.Dispose();
                physicalWorld.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void UpdateGUI(float delta)
    {
        // Console logging of performance
        timeSincePhysicsReport += delta;

        if (timeSincePhysicsReport > 0.5)
        {
            timeSincePhysicsReport = 0;
            GD.Print($"Physics time: {GetPhysicsTime()} Physics FPS limit: " +
                $"{1 / GetPhysicsTime()}, FPS: {Engine.GetFramesPerSecond()}");
        }

        // The actual GUI update part

        // deltaLabel.Text = new LocalizedString("FRAME_DURATION", delta).ToString();

        // TODO: GUI with text on it to show the physics times and FPS
        // fpsLabel.Text = new LocalizedString("FPS", Engine.GetFramesPerSecond()).ToString();
    }

    private void SetupPhysicsBodies()
    {
        var random = new Random(234654642);

        if (Type == TestType.SpheresGodotPhysics)
        {
            var sphere = new SphereShape
            {
                Radius = 0.5f,
            };

            var visuals = CreateSphereMesh();
            int created = 0;

            for (int x = -100; x < 100; x += 2)
            {
                for (int z = -100; z < 100; z += 2)
                {
                    var body = new RigidBody();
                    body.AddChild(new MeshInstance
                    {
                        Mesh = visuals.Mesh,
                    });
                    var owner = body.CreateShapeOwner(body);
                    body.ShapeOwnerAddShape(owner, sphere);

                    body.Translation = new Vector3(x, 1 + (float)random.NextDouble() * 25, z);

                    worldVisuals.AddChild(body);
                    ++created;
                }
            }

            GD.Print("Created Godot rigid bodies: ", created);

            var groundShape = new BoxShape
            {
                Extents = new Vector3(100, 0.05f, 100),
            };

            var ground = new StaticBody();
            var groundShapeOwner = ground.CreateShapeOwner(ground);
            ground.ShapeOwnerAddShape(groundShapeOwner, groundShape);

            ground.Translation = new Vector3(0, -0.025f, 0);
            worldVisuals.AddChild(ground);

            return;
        }

        if (Type is TestType.Spheres or TestType.SpheresIndividualNodes)
        {
            var sphere = PhysicsShape.CreateSphere(0.5f);

            for (int x = -100; x < 100; x += 2)
            {
                for (int z = -100; z < 100; z += 2)
                {
                    sphereBodies.Add(physicalWorld.CreateMovingBody(sphere,
                        new Vector3(x, 1 + (float)random.NextDouble() * 25, z), Quat.Identity));
                }
            }

            GD.Print("Created physics spheres: ", sphereBodies.Count);

            var groundShape = PhysicsShape.CreateBox(new Vector3(100, 0.05f, 100));

            allCreatedBodies.Add(physicalWorld.CreateStaticBody(groundShape, new Vector3(0, -0.025f, 0),
                Quat.Identity));

            allCreatedBodies.AddRange(sphereBodies);
        }
    }

    private float GetPhysicsTime()
    {
        if (Type == TestType.SpheresGodotPhysics)
            return Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess);

        return physicalWorld.AveragePhysicsDuration;
    }

    private CSGMesh CreateSphereMesh()
    {
        var sphere = new CSGMesh
        {
            Mesh = new SphereMesh
            {
                Radius = 0.5f,
                Height = 1.0f,
            },
        };

        return sphere;
    }
}
