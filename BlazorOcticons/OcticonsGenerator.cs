using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace BlazorOcticons
{
    [Generator]
    public class OcticonsGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context)
        {
            var sourceStart = @"
namespace BlazorOcticons {
    public static class Octicons {
";
            var properties = "";
            var assembly = Assembly.GetExecutingAssembly();
            var icons = assembly.GetManifestResourceNames()
                .Where(str => str.StartsWith("BlazorOcticons.icons"));
            var count = 0;
            foreach (var icon in icons)
            {
                using Stream stream = assembly.GetManifestResourceStream(icon);
                using StreamReader reader = new StreamReader(stream);
                var content = reader.ReadToEnd();
                properties += $@"
            public static string I{count} = {"\"" + icon.Replace(".", "").Replace(" ", "") + "\""};";
                count++;
            }

            var sourceEnd = @"
    }
}";
            context.AddSource("Octicons.cs", SourceText.From($"{sourceStart}{properties}{sourceEnd}", Encoding.UTF8));
        }
    }
}
