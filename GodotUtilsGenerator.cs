using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Tokiko.SourceGenerators;

[Generator]
internal partial class GodotUtilsGenerator : IIncrementalGenerator
{
    public const string ProjectGodotFile = "project.godot";
    public const string OutputNamespace  = "GodotExtensions";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var sectionsProvider = SectionParserPipeline.Setup(context);

        InputPipelineGenerator.Setup(context, sectionsProvider);
        LayerPipelineGenerator.Setup(context, sectionsProvider);
    }
}