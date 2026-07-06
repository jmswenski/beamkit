using System.Reflection;
using System.Xml.Linq;

namespace BeamKit.Architecture.Tests;

internal static class ProjectReferenceReader
{
    public static IReadOnlySet<string> ProjectReferenceNames(Assembly assembly)
    {
        var projectPath = ProjectPathFor(assembly);
        if (!File.Exists(projectPath))
        {
            throw new FileNotFoundException($"Project file was not found for assembly '{assembly.GetName().Name}'.", projectPath);
        }

        var document = XDocument.Load(projectPath);
        return document
            .Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Path.GetFileNameWithoutExtension(value!.Replace('\\', Path.DirectorySeparatorChar)) ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
    }

    public static IReadOnlySet<string> AssemblyReferenceNames(Assembly assembly)
    {
        return assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
    }

    public static IReadOnlySet<string> DeclaredExternalReferenceNames(Assembly assembly)
    {
        var document = XDocument.Load(ProjectPathFor(assembly));
        var packageReferences = document
            .Descendants()
            .Where(element => element.Name.LocalName == "PackageReference")
            .Select(element => element.Attribute("Include")?.Value);
        var assemblyReferences = document
            .Descendants()
            .Where(element => element.Name.LocalName == "Reference")
            .Select(element => element.Attribute("Include")?.Value);
        var hintPathReferences = document
            .Descendants()
            .Where(element => element.Name.LocalName == "HintPath")
            .Select(element => Path.GetFileNameWithoutExtension(element.Value.Replace('\\', Path.DirectorySeparatorChar)));

        return packageReferences
            .Concat(assemblyReferences)
            .Concat(hintPathReferences)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string ProjectPathFor(Assembly assembly)
    {
        var assemblyName = assembly.GetName().Name ?? throw new InvalidOperationException("Assembly name is required.");
        return Path.Combine(FindRepositoryRoot(), "src", assemblyName, $"{assemblyName}.csproj");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BeamKit.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }
}
