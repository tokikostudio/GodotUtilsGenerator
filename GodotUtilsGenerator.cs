using Microsoft.CodeAnalysis;

namespace Tokiko.SourceGenerators;

[Generator]
internal partial class GodotUtilsGenerator : IIncrementalGenerator
{
    public const string ProjectGodotFile = "project.godot";
    public const string OutputNamespace  = "GodotUtils";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var sectionsProvider = SectionParserPipeline.Setup(context);
// #if DEBUG
//         if (!Debugger.IsAttached)
//             Debugger.Launch();
// #endif

        InputNamePipeline.Setup(context, sectionsProvider);
        LayerNamePipeline.Setup(context, sectionsProvider);
        BindExportPipeline.Setup(context);
    }
}