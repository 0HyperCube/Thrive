using System;
using Godot;

/// <summary>
///   Handles drawing debug lines
/// </summary>
public class DebugDrawer : ControlWithInput
{
    /// <summary>
    ///   Needs to match what's defined in PhysicalWorld.hpp
    /// </summary>
    private const int MaxPhysicsDebugLevel = 6;

    private static DebugDrawer? instance;

#pragma warning disable CA2213
    private ImmediateGeometry lineDrawer = null!;
    private ImmediateGeometry triangleDrawer = null!;
#pragma warning restore CA2213

    private int currentPhysicsDebugLevel;

    private bool physicsDebugSupported;
    private bool warnedAboutNotBeingSupported;

    // Note that only one debug draw geometry can be going on at once so drawing lines intermixed with triangles is
    // note very efficient
    private bool lineDrawStarted;
    private bool triangleDrawStarted;

    private bool drawnThisFrame;

    private DebugDrawer()
    {
        instance = this;
    }

    public delegate void OnPhysicsDebugLevelChanged(int level);

    public delegate void OnPhysicsDebugCameraPositionChanged(Vector3 position);

    public event OnPhysicsDebugLevelChanged? OnPhysicsDebugLevelChangedHandler;
    public event OnPhysicsDebugCameraPositionChanged? OnPhysicsDebugCameraPositionChangedHandler;

    public static DebugDrawer Instance => instance ?? throw new InstanceNotLoadedYetException();

    public int DebugLevel => currentPhysicsDebugLevel;
    public Vector3 DebugCameraLocation { get; private set; }

    public static void DumpPhysicsState(PhysicalWorld world)
    {
        var path = ProjectSettings.GlobalizePath(Constants.PHYSICS_DUMP_PATH);

        GD.Print("Starting dumping of physics world state to: ", path);

        if (world.DumpPhysicsState(path))
        {
            GD.Print("Physics dump finished");
        }
    }

    public override void _Ready()
    {
        lineDrawer = GetNode<ImmediateGeometry>("LineDrawer");
        triangleDrawer = GetNode<ImmediateGeometry>("TriangleDrawer");

        physicsDebugSupported = NativeInterop.RegisterDebugDrawer(DrawLine, DrawTriangle);

        // Make sure the debug stuff is always rendered
        lineDrawer.SetCustomAabb(new AABB(float.MinValue, float.MinValue, float.MinValue, float.MaxValue,
            float.MaxValue, float.MaxValue));
        triangleDrawer.SetCustomAabb(new AABB(float.MinValue, float.MinValue, float.MinValue, float.MaxValue,
            float.MaxValue, float.MaxValue));

        // TODO: implement debug text drawing (this is a Control to support that in the future)

        if (GetTree().DebugCollisionsHint)
        {
            GD.Print("Enabling physics debug drawing on next frame as debug for that was enabled on the scene tree");
            Invoke.Instance.Queue(IncrementPhysicsDebugLevel);
        }
        else
        {
            if (Constants.AUTOMATICALLY_TURN_ON_PHYSICS_DEBUG_DRAW)
            {
                GD.Print("Starting with debug draw on due to debug draw constant being enabled");
                Invoke.Instance.Queue(IncrementPhysicsDebugLevel);
            }
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        NativeInterop.RemoveDebugDrawer();
    }

    public override void _Process(float delta)
    {
        if (drawnThisFrame)
        {
            // Finish the geometry
            if (lineDrawStarted)
            {
                lineDrawStarted = false;
                lineDrawer.End();
            }

            if (triangleDrawStarted)
            {
                triangleDrawStarted = false;
                triangleDrawer.End();
            }

            lineDrawer.Visible = true;
            triangleDrawer.Visible = true;
            drawnThisFrame = false;

            // Send camera position to the debug draw for LOD purposes
            try
            {
                DebugCameraLocation = GetViewport().GetCamera().GlobalTranslation;

                OnPhysicsDebugCameraPositionChangedHandler?.Invoke(DebugCameraLocation);
            }
            catch (Exception e)
            {
                GD.PrintErr("Failed to send camera position to physics debug draw", e);
            }
        }
        else if (currentPhysicsDebugLevel < 1)
        {
            lineDrawer.Visible = false;
            triangleDrawer.Visible = false;
        }
    }

    [RunOnKeyDown("d_physics_debug", Priority = -2)]
    public void IncrementPhysicsDebugLevel()
    {
        if (!physicsDebugSupported)
        {
            if (!warnedAboutNotBeingSupported)
            {
                GD.PrintErr("The version of the loaded native Thrive library doesn't support physics " +
                    "debug drawing, debug drawing will not be attempted");
                warnedAboutNotBeingSupported = true;
            }
        }
        else
        {
            currentPhysicsDebugLevel += 1 % MaxPhysicsDebugLevel;

            GD.Print("Setting physics debug level to: ", currentPhysicsDebugLevel);

            OnPhysicsDebugLevelChangedHandler?.Invoke(currentPhysicsDebugLevel);
        }
    }

    private void DrawLine(Vector3 from, Vector3 to, Color colour)
    {
        try
        {
            StartDrawingIfNotYetThisFrame();

            if (!lineDrawStarted)
            {
                if (triangleDrawStarted)
                {
                    triangleDrawStarted = false;
                    triangleDrawer.End();
                }

                lineDrawStarted = true;
                lineDrawer.Begin(Mesh.PrimitiveType.Lines);
            }

            lineDrawer.SetColor(colour);

            // lineDrawer.SetColor(Colors.Chocolate);
            lineDrawer.AddVertex(from);
            lineDrawer.AddVertex(to);
        }
        catch (Exception e)
        {
            GD.PrintErr("Error in debug drawing: ", e);
        }
    }

    private void DrawTriangle(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, Color colour)
    {
        try
        {
            StartDrawingIfNotYetThisFrame();

            if (!triangleDrawStarted)
            {
                if (lineDrawStarted)
                {
                    lineDrawStarted = false;
                    lineDrawer.End();
                }

                triangleDrawStarted = true;
                triangleDrawer.Begin(Mesh.PrimitiveType.Triangles);
            }

            triangleDrawer.SetColor(colour);

            triangleDrawer.AddVertex(vertex1);
            triangleDrawer.AddVertex(vertex2);
            triangleDrawer.AddVertex(vertex3);
        }
        catch (Exception e)
        {
            GD.PrintErr("Error in debug drawing: ", e);
        }
    }

    private void StartDrawingIfNotYetThisFrame()
    {
        if (drawnThisFrame)
            return;

        lineDrawer.Clear();
        lineDrawStarted = false;

        triangleDrawer.Clear();
        triangleDrawStarted = false;

        drawnThisFrame = true;
    }
}
