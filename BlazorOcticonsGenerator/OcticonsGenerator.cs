﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace BlazorOcticonsGenerator
{
    [Generator]
    public class OcticonsGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.MSBuildProjectDirectory", out var projectDirectory) == false)
            {
                throw new ArgumentException("MSBuildProjectDirectory should be specified");
            }
            var iconsFolder = Path.Combine(projectDirectory, "Octicons");
            var sourceStart = @"
namespace BlazorOcticonsGenerator {
    // this is the list of files generated in the Octicons folder
    public static class Octicons {
";
            var properties = "";
            var assembly = Assembly.GetExecutingAssembly();
            var icons = assembly.GetManifestResourceNames()
                .Where(str => str.StartsWith("BlazorOcticonsGenerator.icons"));
            var count = 0;
            foreach (var icon in icons)
            {
                using var stream = assembly.GetManifestResourceStream(icon);
                using var reader = new StreamReader(stream);
                var svg = reader.ReadToEnd();
                var fileName = icon.Replace("BlazorOcticonsGenerator.icons.", "").Replace(".svg", "").Replace(" ", "");
                fileName = string.Join("", fileName.Split('-').Select(i => $"{i[0].ToString().ToUpper()}{i.Substring(1)}"));
                var size = Convert.ToInt32(fileName.Substring(fileName.Length - 2, 2));
                var code = $@"

@code
{{
  [Parameter]
  public string Color {{ get; set; }} = ""#000"";

  [Parameter]
  public int Size {{ get; set; }} = {size};
}}";
                properties += $@"
            public static string I{count} = {"\"" + fileName + "\""};";
                count++;
                svg = Regex.Replace(svg, "width=\"[0-9]*\"", "width=\"@Size\"");
                svg = Regex.Replace(svg, "height=\"[0-9]*\"", "height=\"@Size\"");
                if (!Directory.Exists(iconsFolder))
                {
                    Directory.CreateDirectory(iconsFolder);
                }
                File.WriteAllText(Path.Combine(iconsFolder, $"{fileName}.razor"), $"{svg.Replace("path fill", "path fill=\"@Color\" fill")}{code}");
            }

            var sourceEnd = @"
    }
}";
            context.AddSource("Octicons.cs", SourceText.From($"{sourceStart}{properties}{sourceEnd}", Encoding.UTF8));
        }
    }
}
