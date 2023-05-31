using System.Runtime.InteropServices;

/// <summary>
///   C# side of the interop
/// </summary>
public static class Interop
{
    [DllImport("thrive_native")]
    internal static extern int CheckAPIVersion();

    [DllImport("thrive_native")]
    internal static extern int InitThriveLibrary();

    [DllImport("thrive_native")]
    internal static extern void ShutdownThriveLibrary();
}
