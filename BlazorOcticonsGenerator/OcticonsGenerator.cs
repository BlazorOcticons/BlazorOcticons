using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;

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
            var all = new List<string>();
            var orderedSvgFiles = svgFiles.OrderBy(f => f.FileName).ToList();

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

                var code = $@"

@code
{{
  [Parameter]
  public string Color {{ get; set; }} = ""#000"";

  [Parameter]
  public int Size {{ get; set; }} = {size};
}}";

                svg = Regex.Replace(svg, "width=\"[0-9]*\"", "width=\"@Size\"");
                svg = Regex.Replace(svg, "height=\"[0-9]*\"", "height=\"@Size\"");

                if (!Directory.Exists(iconsFolder))
                {
                    Directory.CreateDirectory(iconsFolder);
                }

                all.Add(fileName);
                var fileContent = $"{svg.Replace("path fill", "path fill=\"@Color\" fill").Replace("path d", "path fill=\"@Color\" d")}{code}";
                WriteFileWithRetry(Path.Combine(iconsFolder, $"{fileName}.razor"), fileContent);
            }

            // Delete orphaned .razor files that no longer have a corresponding SVG
            if (Directory.Exists(iconsFolder))
            {
                var generatedFileNames = new HashSet<string>(all.Select(f => $"{f}.razor"), StringComparer.OrdinalIgnoreCase);
                foreach (var existingFile in Directory.GetFiles(iconsFolder, "*.razor"))
                {
                    var existingFileName = Path.GetFileName(existingFile);
                    if (!generatedFileNames.Contains(existingFileName))
                    {
                        DeleteFileWithRetry(existingFile);
                    }
                }
            }
        }

        private static void WriteFileWithRetry(string path, string content, int maxRetries = 3)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    File.WriteAllText(path, content);
                    return;
                }
                catch (IOException) when (i < maxRetries - 1)
                {
                    Thread.Sleep(100 * (i + 1));
                }
            }
        }

        private static void DeleteFileWithRetry(string path, int maxRetries = 3)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    File.Delete(path);
                    return;
                }
                catch (IOException) when (i < maxRetries - 1)
                {
                    Thread.Sleep(100 * (i + 1));
                }
            }
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
