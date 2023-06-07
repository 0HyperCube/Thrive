using System;
using System.Runtime.InteropServices;
using Godot;

public class PhysicsBody : IDisposable
{
    private bool disposed;
    private IntPtr nativeInstance;

    internal PhysicsBody(IntPtr nativeInstance)
    {
        this.nativeInstance = nativeInstance;

        if (this.nativeInstance.ToInt64() == 0)
        {
            // TODO: should this crash the game?
            GD.PrintErr(
                "Physics body can't be created from null native pointer, we probably ran out of physics bodies");
        }
    }

    ~PhysicsBody()
    {
        Dispose(false);
    }

    /// <summary>
    ///   Makes this body unable to move on the given axis. Used to make microbes move only in a 2D plane.
    /// </summary>
    /// <param name="axis">The axis to lock this body on</param>
    /// <remarks>
    ///   <para>
    ///     TODO: test if this can be used before/after adding the body to the world
    ///   </para>
    /// </remarks>
    public void AddAxisLockConstraint(Vector3 axis)
    {
        NativeMethods.PhysicsBodyAddAxisLock(AccessBodyInternal(), new JVecF3(axis));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    internal IntPtr AccessBodyInternal()
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(PhysicsBody));

        return nativeInstance;
    }

    protected virtual void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources();
        if (disposing)
        {
            disposed = true;
        }
    }

    private void ReleaseUnmanagedResources()
    {
        if (nativeInstance.ToInt64() != 0)
        {
            NativeMethods.ReleasePhysicsBodyReference(nativeInstance);
            nativeInstance = new IntPtr(0);
        }
    }
}

/// <summary>
///   Thrive native library methods related to bodies
/// </summary>
internal static partial class NativeMethods
{
    [DllImport("thrive_native")]
    internal static extern void PhysicsBodyAddAxisLock(IntPtr body, JVecF3 axis);

    [DllImport("thrive_native")]
    internal static extern void ReleasePhysicsBodyReference(IntPtr body);
}
