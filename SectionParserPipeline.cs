using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Tokiko.SourceGenerators;

public class SectionData
{
    public SectionData(string name)
    {
        Name = name;
    }

    public readonly string       Name;
    public readonly List<string> Lines = new();
}

public static class SectionParserPipeline
{
    public static IncrementalValuesProvider<ImmutableArray<SectionData>> Setup(IncrementalGeneratorInitializationContext context)
    {
        return context.AdditionalTextsProvider.Where(static file => file.Path.EndsWith(GodotUtilsGenerator.ProjectGodotFile))
                      .Select((file, token) => file.GetText(token)!.ToString())
                      .Select(ParseContent);
    }

    private static ImmutableArray<SectionData> ParseContent(string content, CancellationToken token)
    {
        SectionData section  = null;
        var         sections = new List<SectionData>();
        var         splits   = content.Split('\n');
        foreach (var line in splits)
        {
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                if (section != null)
                    sections.Add(section);

                section = new SectionData(line.Substring(1, line.Length - 2));
                continue;
            }

            if (section == null)
                continue;

            if (!string.IsNullOrEmpty(line))
                section.Lines.Add(line);
        }

        return sections.ToImmutableArray();
    }
}