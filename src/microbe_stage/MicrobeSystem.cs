using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Handles processing <see cref="Microbe"/>s in a multithreaded way
/// </summary>
public class MicrobeSystem
{
    private readonly List<Task> tasks = new();

    private readonly MicrobeWorldSimulation worldSimulation;

    public MicrobeSystem(MicrobeWorldSimulation worldSimulation)
    {
        this.worldSimulation = worldSimulation;
    }

    public void Process(float delta)
    {
        var microbes = worldSimulation.Entities.OfType<Microbe>().ToList();
        var microbeCount = microbes.Count;

        // Start of async early processing
        var executor = TaskExecutor.Instance;

        for (int i = 0; i < microbeCount; i += Constants.MICROBE_AI_OBJECTS_PER_TASK)
        {
            int start = i;

            var task = new Task(() =>
            {
                for (int a = start;
                     a < start + Constants.MICROBE_AI_OBJECTS_PER_TASK && a < microbeCount;
                     ++a)
                {
                    var microbe = microbes[a];
                    microbe.ProcessEarlyAsync(delta);
                }
            });

            tasks.Add(task);
        }

        // Start and wait for tasks to finish
        executor.RunTasks(tasks);
        tasks.Clear();

        // And then process the synchronous part for all microbes
        foreach (var microbe in microbes)
        {
            microbe.NotifyExternalProcessingIsUsed();
            microbe.ProcessSync(delta);
        }
    }
}
