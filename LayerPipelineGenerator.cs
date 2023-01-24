using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Tokiko.SourceGenerators;

public static class LayerPipelineGenerator
{
    private const string OutputClassName = "LayerName";
    private const string SectionName     = "layer_names";

    public static void Setup(IncrementalGeneratorInitializationContext context, IncrementalValuesProvider<ImmutableArray<SectionData>> sectionsProvider)
        => context.RegisterSourceOutput(sectionsProvider, ParseInputSection);

    private class LayerData
    {
        public LayerData(string dimension) => _dimension = dimension;

        private readonly string       _dimension;
        public readonly  List<string> Render     = new();
        public readonly  List<string> Physics    = new();
        public readonly  List<string> Navigation = new();

        public override string ToString() => $@"    public static class Render{_dimension}
    {{
{string.Join("\n", Render)}
    }}
    public static class Physics{_dimension}
    {{
{string.Join("\n", Physics)}
    }}
    public static class Navigation{_dimension}
    {{
{string.Join("\n", Navigation)}
    }}";
    }

    private static void ParseInputSection(SourceProductionContext spc, ImmutableArray<SectionData> sections)
    {
        var layerSection = sections.FirstOrDefault(s => s.Name == SectionName);
        if (layerSection == null)
            return;

        var layer2d = new LayerData("2D");
        var layer3d = new LayerData("3D");
        foreach (var line in layerSection.Lines)
        {
            var equalPosition = line.IndexOf("=");
            if (equalPosition == -1)
                continue;

            var regex  = new Regex(@"(?<dimension>[23])d_(?<layer>\w+)/layer_(?<offset>\d+)=""(?<name>\w+)""");
            var groups = regex.Match(line).Groups;
            var layer = (groups["dimension"].Value == "2")
                            ? layer2d
                            : layer3d;
            var groupLayer = groups["layer"].Value;
            var container = groupLayer switch
                            {
                                "render"     => layer.Render,
                                "physics"    => layer.Physics,
                                "navigation" => layer.Navigation,
                                _            => null,
                            };
            if (container == null)
                continue;

            var offset = int.Parse(groups["offset"].Value) - 1;
            var name   = groups["name"].Value.SnakeToPascalCase();

            container.Add($"        public static int {name} = 1 << {offset};");
        }

        var src = $@"namespace {GodotUtilsGenerator.OutputNamespace};
public static class {OutputClassName}
{{
{layer2d}
{layer3d}
}}";
        spc.AddSource($"{OutputClassName}.g.cs", src);
    }
}