using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Tokiko.SourceGenerators;

public static class LayerPipelineGenerator
{
    private const string OutputClassName = "Layer";
    private const string SectionName     = "layer_names";

    public static void Setup(IncrementalGeneratorInitializationContext context, IncrementalValuesProvider<ImmutableArray<SectionData>> sectionsProvider)
        => context.RegisterSourceOutput(sectionsProvider, ParseInputSection);

    private class LayerPair
    {
        public LayerPair(string name, int shift)
        {
            Name  = name;
            Shift = shift;
        }

        public readonly string Name;
        public readonly int    Shift;
    }

    private class LayerData
    {
        public LayerData(string dimension) => Dimension = dimension;

        public readonly string          Dimension;
        public readonly List<LayerPair> Render     = new();
        public readonly List<LayerPair> Physics    = new();
        public readonly List<LayerPair> Navigation = new();

        // container.Add(
        private ImmutableArray<string> TransformPairsToMask(List<LayerPair> layerPairs)
        {
            return layerPairs.Select(lp => $"        public static uint {lp.Name} = 1 << {lp.Shift};")
                             .ToImmutableArray();
        }

        public string GenerateMaskConstants() => $@"    public static class Render{Dimension}
    {{
{string.Join("\n", TransformPairsToMask(Render))}
    }}
    public static class Physics{Dimension}
    {{
{string.Join("\n", TransformPairsToMask(Physics))}
    }}
    public static class Navigation{Dimension}
    {{
{string.Join("\n", TransformPairsToMask(Navigation))}
    }}";

        public void GenerateGodotClassExtensions(SourceProductionContext spc)
        {
            OutputGodotExtensionClass(spc, $"CollisionObject{Dimension}", "Physics", Physics, "CollisionLayer", "CollisionMask");
            OutputGodotExtensionClass(spc, $"VisualInstance{Dimension}", "Render", Render, "Layers");
            OutputGodotExtensionClass(spc, $"NavigationLink{Dimension}", "Navigation", Navigation, "NavigationLayers");
            OutputGodotExtensionClass(spc, $"NavigationAgent{Dimension}", "Navigation", Navigation, "NavigationLayers");
            OutputGodotExtensionClass(spc, $"NavigationRegion{Dimension}", "Navigation", Navigation, "NavigationLayers");
        }

        private void OutputGodotExtensionClass(SourceProductionContext spc,
                                               string                  className,
                                               string                  layerName,
                                               List<LayerPair>         layerPairs,
                                               params string[]         nodeFieldNames)
        {
            var layerPrefix     = $"{OutputClassName}.{layerName}{Dimension}";
            var classExtensions = $"{className}Extensions";
            var hasMethods      = new List<string>();
            foreach (var name in nodeFieldNames)
            foreach (var layerPair in layerPairs)
                hasMethods.Add($"    public static bool Has{name}{layerPair.Name}(this {className} node) => (node.{name} & {layerPrefix}.{layerPair.Name}) != 0;");

            var src = $@"using Godot;
using {GodotUtilsGenerator.OutputNamespace};
public static class {classExtensions}
{{
{string.Join("\n", hasMethods)}
}}";
            spc.AddSource($"{classExtensions}.g.cs", src);
        }
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

            var shift = int.Parse(groups["offset"].Value) - 1;
            var name  = groups["name"].Value.SnakeToPascalCase();

            container.Add(new LayerPair(name, shift));
        }

        var src = $@"namespace {GodotUtilsGenerator.OutputNamespace};
public static class {OutputClassName}
{{
{layer2d.GenerateMaskConstants()}
{layer3d.GenerateMaskConstants()}
}}";
        spc.AddSource($"{OutputClassName}.g.cs", src);

        layer2d.GenerateGodotClassExtensions(spc);
        layer3d.GenerateGodotClassExtensions(spc);
    }
}