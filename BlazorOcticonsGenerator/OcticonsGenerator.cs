using System;
using System.Collections.Generic;
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
            var all = new List<string>();
            var assembly = Assembly.GetExecutingAssembly();
            var icons = assembly.GetManifestResourceNames().Where(str => str.StartsWith("BlazorOcticonsGenerator.icons"));
            var count = 0;
            var icons12 = new List<string>();
            var icons16 = new List<string>();
            var icons24 = new List<string>();
            foreach (var icon in icons)
            {
                using var stream = assembly.GetManifestResourceStream(icon);
                using var reader = new StreamReader(stream);
                var svg = reader.ReadToEnd();
                var fileName = icon.Replace("BlazorOcticonsGenerator.icons.", "").Replace(".svg", "").Replace(" ", "");
                fileName = string.Join("", fileName.Split('-').Select(i => $"{i[0].ToString().ToUpper()}{i.Substring(1)}"));
                var size = Convert.ToInt32(fileName.Substring(fileName.Length - 2, 2));
                switch (size)
                {
                    case 12: icons12.Add(fileName); break;
                    case 16: icons16.Add(fileName); break;
                    case 24: icons24.Add(fileName); break;
                }
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
                all.Add(fileName);
                File.WriteAllText(Path.Combine(iconsFolder, $"{fileName}.razor"), $"{svg.Replace("path fill", "path fill=\"@Color\" fill")}{code}");
            }
            properties += $@"
            public static string[] All = new[] {{ {string.Join(",", all.Select(fn => "\"" + fn + "\""))} }};";

            var divIcons12 = icons12.Aggregate("", (current, icon12) => current + $"    <div class=\"p-3\"><a href=\"/Icon/{icon12}\"><{icon12} /></a></div>{Environment.NewLine}");
            var divIcons16 = icons16.Aggregate("", (current, icon16) => current + $"    <div class=\"p-3\"><a href=\"/Icon/{icon16}\"><{icon16} /></a></div>{Environment.NewLine}");
            var divIcons24 = icons24.Aggregate("", (current, icon24) => current + $"    <div class=\"p-3\"><a href=\"/Icon/{icon24}\"><{icon24} /></a></div>{Environment.NewLine}");
            var sourceEnd = @"
    }
}";
            File.WriteAllText(Path.Combine(projectDirectory, "IconsCollection.razor"), 
                $@"
<div class=""py-4"">
  <div class=""pb-3"">
    <span class=""fw-bold fs-x-large"">12px</span>
  </div>
  <div class=""d-flex pb-3 flex-wrap-wrap"">
    {divIcons12}
  </div>
  <div class=""pb-3"">
    <span class=""fw-bold fs-x-large"">16px</span>
  </div>
  <div class=""d-flex pb-3 flex-wrap-wrap"">
    {divIcons16}
  </div>
  <div class=""d-flex pb-3"">
    <span class=""fw-bold fs-x-large"">24px</span>
  </div>
  <div class=""d-flex pb-3 flex-wrap-wrap"">
    {divIcons24}
  </div>
</div>");
            context.AddSource("Octicons.cs", SourceText.From($"{sourceStart}{properties}{sourceEnd}", Encoding.UTF8));
        }
    }
}
