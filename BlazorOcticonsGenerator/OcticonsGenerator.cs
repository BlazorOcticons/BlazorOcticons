using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace BlazorOcticonsGenerator
{
    [Generator(LanguageNames.CSharp)]
    public class OcticonsGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Get the project directory from analyzer config options
            var projectDirectoryProvider = context.AnalyzerConfigOptionsProvider
                .Select(static (options, _) =>
                {
                    options.GlobalOptions.TryGetValue("build_property.MSBuildProjectDirectory", out var projectDirectory);
                    return projectDirectory;
                });

            // Get all SVG files from AdditionalFiles
            var svgFilesProvider = context.AdditionalTextsProvider
                .Where(static file => file.Path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                .Select(static (file, ct) => new SvgFile(
                    Path.GetFileNameWithoutExtension(file.Path),
                    file.GetText(ct)?.ToString() ?? string.Empty))
                .Where(static svg => !string.IsNullOrEmpty(svg.Content))
                .Collect();

            // Combine project directory with SVG files
            var combinedProvider = projectDirectoryProvider.Combine(svgFilesProvider);

            // Register source output - this will be called when SVG files or project directory changes
            context.RegisterSourceOutput(combinedProvider, Execute);
        }

        private static void Execute(SourceProductionContext context, (string? ProjectDirectory, ImmutableArray<SvgFile> SvgFiles) input)
        {
            var (projectDirectory, svgFiles) = input;

            if (string.IsNullOrEmpty(projectDirectory))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "OCTICONS001",
                        "Missing Project Directory",
                        "MSBuildProjectDirectory should be specified",
                        "BlazorOcticonsGenerator",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    Location.None));
                return;
            }

            if (svgFiles.IsDefaultOrEmpty)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "OCTICONS002",
                        "No SVG Files Found",
                        "No SVG files were found in AdditionalFiles. Add SVG files with <AdditionalFiles Include=\"path\\to\\icons\\**\\*.svg\" />",
                        "BlazorOcticonsGenerator",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true),
                    Location.None));
                return;
            }

            var iconsFolder = Path.Combine(projectDirectory, "Octicons");
            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine("namespace BlazorOcticonsGenerator {");
            sourceBuilder.AppendLine("    // this is the list of files generated in the Octicons folder");
            sourceBuilder.AppendLine("    public static class OcticonsList {");

            var all = new List<string>();
            var orderedSvgFiles = svgFiles.OrderBy(f => f.FileName).ToList();

            var count = 0;
            var icons12 = new List<string>();
            var icons16 = new List<string>();
            var icons24 = new List<string>();
            var icons32 = new List<string>();
            var icons48 = new List<string>();
            var icons96 = new List<string>();

            foreach (var svgFile in orderedSvgFiles)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var svg = svgFile.Content;
                var fileName = svgFile.FileName.Replace(" ", "");
                fileName = string.Join("", fileName.Split('-').Select(i => $"{char.ToUpper(i[0])}{i.Substring(1)}"));

                if (fileName.Length < 2 || !int.TryParse(fileName.Substring(fileName.Length - 2, 2), out var size))
                {
                    continue;
                }

                switch (size)
                {
                    case 12: icons12.Add(fileName); break;
                    case 16: icons16.Add(fileName); break;
                    case 24: icons24.Add(fileName); break;
                    case 32: icons32.Add(fileName); break;
                    case 48: icons48.Add(fileName); break;
                    case 96: icons96.Add(fileName); break;
                }

                var code = $@"

@code
{{
  [Parameter]
  public string Color {{ get; set; }} = ""#000"";

  [Parameter]
  public int Size {{ get; set; }} = {size};
}}";
                sourceBuilder.AppendLine($"        public static string I{count} = \"{fileName}\";");
                count++;

                svg = Regex.Replace(svg, "width=\"[0-9]*\"", "width=\"@Size\"");
                svg = Regex.Replace(svg, "height=\"[0-9]*\"", "height=\"@Size\"");

                if (!Directory.Exists(iconsFolder))
                {
                    Directory.CreateDirectory(iconsFolder);
                }

                all.Add(fileName);
                var fileContent = $"{svg.Replace("path fill", "path fill=\"@Color\" fill").Replace("path d", "path fill=\"@Color\" d")}{code}";
                File.WriteAllText(Path.Combine(iconsFolder, $"{fileName}.razor"), fileContent);
            }

            sourceBuilder.AppendLine($"        public static string[] All = new[] {{ {string.Join(", ", all.Select(fn => $"\"{fn}\""))} }};");
            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine("}");

            // Generate IconsCollection.razor
            var iconsCollectionBuilder = new StringBuilder();
            iconsCollectionBuilder.AppendLine("<div class=\"py-4\">");

            AppendIconSection(iconsCollectionBuilder, "12px", icons12);
            AppendIconSection(iconsCollectionBuilder, "16px", icons16);
            AppendIconSection(iconsCollectionBuilder, "24px", icons24);
            AppendIconSection(iconsCollectionBuilder, "32px", icons32);
            AppendIconSection(iconsCollectionBuilder, "48px", icons48);
            AppendIconSection(iconsCollectionBuilder, "96px", icons96);

            iconsCollectionBuilder.AppendLine("</div>");

            File.WriteAllText(Path.Combine(projectDirectory, "IconsCollection.razor"), iconsCollectionBuilder.ToString());

            context.AddSource("OcticonsList.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

        private static void AppendIconSection(StringBuilder builder, string sizeLabel, List<string> iconNames)
        {
            if (iconNames.Count == 0) return;

            builder.AppendLine("  <div class=\"pb-3\">");
            builder.AppendLine($"    <span class=\"fw-bold fs-x-large\">{sizeLabel}</span>");
            builder.AppendLine("  </div>");
            builder.AppendLine("  <div class=\"d-flex pb-3 flex-wrap-wrap\">");

            foreach (var iconName in iconNames)
            {
                builder.AppendLine($"    <div class=\"p-3\"><a class=\"cursor-pointer\" @onclick=\"@(async ()=> await OnClick.Invoke(\"{iconName}\"))\"><{iconName} /></a></div>");
            }

            builder.AppendLine("  </div>");
        }
    }

    /// <summary>
    /// Represents an SVG file with its name and content.
    /// Must be a record/struct for proper incremental generator caching.
    /// </summary>
    internal readonly struct SvgFile : IEquatable<SvgFile>
    {
        public string FileName { get; }
        public string Content { get; }

        public SvgFile(string fileName, string content)
        {
            FileName = fileName;
            Content = content;
        }

        public bool Equals(SvgFile other) =>
            FileName == other.FileName && Content == other.Content;

        public override bool Equals(object? obj) =>
            obj is SvgFile other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (FileName.GetHashCode() * 397) ^ Content.GetHashCode();
            }
        }
    }
}
