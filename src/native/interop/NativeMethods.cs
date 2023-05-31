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
public static class NativeMethods
{
    // DL Open flag (hopefully this is not different on any unix system)
    private const int RTLD_NOW = 2;

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
        PerformNativeLibPathLoadHack();

        int version = CheckAPIVersion();

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

        var result = InitThriveLibrary();

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
        ShutdownThriveLibrary();
    }

    [DllImport("libthrive_native.so")]
    private static extern int CheckAPIVersion();

    [DllImport("thrive_native")]
    private static extern int InitThriveLibrary();

    [DllImport("thrive_native")]
    private static extern void ShutdownThriveLibrary();

    private static void PerformNativeLibPathLoadHack()
    {
        // Needed before .NET 7 allows doing this nicely
        // Because the look path is for some reason just in ".mono/temp" folder we need this manual hack to be able
        // to load a library in our current / executable folder

        var libraryName = ThriveNativeLibraryName();

        foreach (var libraryFolder in GetPotentialNativeLibraryFolders())
        {
            var path = Path.Combine(libraryFolder, libraryName);

            if (!File.Exists(path))
                continue;

            try
            {
                LoadNativeLibrary(path);
            }
            catch (Exception e)
            {
                GD.PrintErr($"Loading native library ({path}) early failed: ", e);
            }

            return;
        }

        GD.PrintErr($"Could not find native library ({libraryName}) anywhere to do an early load hack on it, " +
            "will likely fail next with an exception failing ot load it");
    }

    // ReSharper disable StringLiteralTypo

    [DllImport("libdl.so", EntryPoint = "dlopen")]
    private static extern IntPtr DLOpenLinux([MarshalAs(UnmanagedType.LPWStr)] string filename, int flags);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibrary(string filename);

    [DllImport("libdl.dylib", EntryPoint = "dlopen")]
    private static extern IntPtr DLOpenMac([MarshalAs(UnmanagedType.LPWStr)] string fileName, int flags);

    // ReSharper restore StringLiteralTypo

    private static void LoadNativeLibrary(string libraryFilePath)
    {
        switch (FeatureInformation.GetOS())
        {
            case FeatureInformation.PlatformWindows:
            {
                var moduleHandle = LoadLibrary(libraryFilePath);
                if (moduleHandle.ToInt64() == 0)
                    throw new Exception("Failed to load the library (handle is null)");

                break;
            }

            case FeatureInformation.PlatformLinux:
            {
                var moduleHandle = DLOpenLinux(libraryFilePath, RTLD_NOW);
                if (moduleHandle.ToInt64() == 0)
                    throw new Exception("Failed to open the library (handle is null)");

                break;
            }

            case FeatureInformation.PlatformMac:
            {
                var moduleHandle = DLOpenMac(libraryFilePath, RTLD_NOW);
                if (moduleHandle.ToInt64() == 0)
                    throw new Exception("Failed to open the library (handle is null)");

                break;
            }

            default:
                throw new InvalidOperationException("Unknown method to load native library for this platform");
        }
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
