using BeamKit.Core.Domain;
using BeamKit.Core.Serialization;

namespace BeamKit.Cli;

internal static class PlanJsonLoader
{
    public static Plan FromFile(string path)
    {
        return BeamKitPlanJson.FromFile(path);
    }

    public static Plan FromJson(string json)
    {
        return BeamKitPlanJson.FromJson(json);
    }
}
