using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Godot;
using Directory = System.IO.Directory;
using File = System.IO.File;
using Path = System.IO.Path;

/// <summary>
///   Calling interface from C# to the native code side of things for the native module
/// </summary>
public static class NativeLibraryHandler
{
    private static bool loadCalled;

    /// <summary>
    ///   Loads and checks the native library is good to use
    /// </summary>
    /// <exception cref="Exception">If the library is not fine</exception>
    /// <exception cref="DllNotFoundException">If finding the library failed</exception>
    public static void Load()
    {
        if (loadCalled)
            throw new InvalidOperationException("Load has been called already");

        loadCalled = true;

        // The default path handling for Godot mono seems really bad (at least on Linux) so we preload the native
        // library to ensure it gets loaded even though it already is at a sensible location
        InsertNativeLibPathHack();

        int version = Interop.CheckAPIVersion();

        if (version != NativeConstants.Version)
        {
            throw new Exception($"Failed to initialize Thrive native library, unexpected version {version} " +
                $"is not required: {NativeConstants.Version}");
        }

        GD.Print("Loaded native Thrive library version ", version);
    }

    /// <summary>
    ///   Performs any initialization needed by the native library
    /// </summary>
    /// <param name="settings">Current game settings</param>
    public static void Init(Settings settings)
    {
        // Settings are passed as probably in the future something needs to be setup right in the native side of
        // things for the initial settings
        _ = settings;

        var result = Interop.InitThriveLibrary();

        if (result != 0)
        {
            throw new InvalidOperationException($"Failed to initialize Thrive native library, code: {result}");
        }
    }

    /// <summary>
    ///   Releases all native resources and prepares the library for process exit
    /// </summary>
    public static void Shutdown()
    {
        Interop.ShutdownThriveLibrary();
    }

    // TODO: remove this method usage once we move to .NET 7
    // https://learn.microsoft.com/en-us/dotnet/standard/native-interop/cross-platform#custom-import-resolver
    // ReSharper disable once StringLiteralTypo
#pragma warning disable CA2101
    [DllImport("__Internal", EntryPoint = "mono_dllmap_insert", CharSet = CharSet.Ansi)]
    private static extern void MonoDllMapInsert(IntPtr assembly, string dll, string? func, string targetDll,
        string? targetFunc);
#pragma warning restore CA2101

    private static void InsertNativeLibPathHack()
    {
        // Needed before .NET 7 allows doing this nicely
        // Because the look path is for some reason just in ".mono/temp" folder we need this manual hack to be able
        // to specify a sensible path for mono to use

        var libraryName = ThriveNativeLibraryName();

        foreach (var libraryFolder in GetPotentialNativeLibraryFolders())
        {
            var path = Path.Combine(libraryFolder, libraryName);

            if (!File.Exists(path))
                continue;

            try
            {
                RegisterNativeLibrary("thrive_native", path);
            }
            catch (Exception e)
            {
                GD.PrintErr($"Register native library path \"{path}\" failed: ", e);
            }

            return;
        }

        GD.PrintErr($"Could not find native library ({libraryName}) anywhere to do an early load hack on it, " +
            "will likely fail next with an exception failing ot load it");
    }

    private static void RegisterNativeLibrary(string name, string libraryFilePath)
    {
        var path = Path.GetFullPath(libraryFilePath);

        MonoDllMapInsert(IntPtr.Zero, name, null, path, null);

        // MonoDllMapInsert(IntPtr.Zero, "thrive_native.dll", null, path, null);
        // MonoDllMapInsert(IntPtr.Zero, "thrive_native.so", null, path, null);
        // MonoDllMapInsert(IntPtr.Zero, "libthrive_native.so", null, path, null);
        // MonoDllMapInsert(IntPtr.Zero, "libthrive_native.dll", null, path, null);
        // MonoDllMapInsert(IntPtr.Zero, "libthrive_native.so.dll", null, path, null);
        // MonoDllMapInsert(IntPtr.Zero, path, null, path, null);
    }

    private static string ThriveNativeLibraryName()
    {
        switch (FeatureInformation.GetOS())
        {
            case FeatureInformation.PlatformWindows:
                return "thrive_native.dll";
            case FeatureInformation.PlatformLinux:
                return "libthrive_native.so";
            case FeatureInformation.PlatformMac:
                // TODO: this is untested
                return "libthrive_native.dylib";
            default:
                GD.PrintErr("Unknown native library name for current platform, using default");
                return "thrive_native.so";
        }
    }

    private static IEnumerable<string> GetPotentialNativeLibraryFolders()
    {
        var currentDirectory = Directory.GetCurrentDirectory();

        yield return currentDirectory;

        string? assemblyDerivedPath = null;

        try
        {
            assemblyDerivedPath = Assembly.GetEntryAssembly()?.Location ?? Assembly.GetExecutingAssembly().Location;
        }
        catch (Exception e)
        {
            GD.PrintErr("Could not determine current assembly folder for native library finding: ", e);
        }

        if (assemblyDerivedPath != null)
            yield return assemblyDerivedPath;

        // Editor layout paths
        var osName = FeatureInformation.GetOS().ToLowerInvariant();

        // TODO: allow specifying debug to be preferred?
        yield return Path.Combine(currentDirectory, $"native_libs/{osName}/release/lib");
        yield return Path.Combine(currentDirectory, $"native_libs/{osName}/release/bin");

        yield return Path.Combine(currentDirectory, $"native_libs/{osName}/debug/lib");
        yield return Path.Combine(currentDirectory, $"native_libs/{osName}/debug/bin");
    }
}
