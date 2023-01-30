using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Design;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Tokiko.SourceGenerators;

public static class InputNamePipeline
{
    private const string OutputClassName = "InputName";
    private const string SectionName     = "input";

    public static void Setup(IncrementalGeneratorInitializationContext context, IncrementalValuesProvider<ImmutableArray<SectionData>> sectionsProvider)
        => context.RegisterSourceOutput(sectionsProvider, ParseInputSection);

    private static void ParseInputSection(SourceProductionContext spc, ImmutableArray<SectionData> sections)
    {
        var inputSection = sections.FirstOrDefault(s => s.Name == SectionName);
        if (inputSection == null)
            return;

        var inputs = new List<string>();
        foreach (var line in inputSection.Lines)
        {
            var equalPosition = line.IndexOf("=");
            if (equalPosition == -1)
                continue;

            var godotName = line.Substring(0, equalPosition);
            var camelName = godotName.SnakeToPascalCase();

            inputs.Add($"    public const string {camelName} = \"{godotName}\";");
        }

        var src = $@"namespace {GodotUtilsGenerator.OutputNamespace};
public static class {OutputClassName}
{{
{string.Join("\n", inputs)}
}}";
        spc.AddSource($"{OutputClassName}.g.cs", src);
    }
}