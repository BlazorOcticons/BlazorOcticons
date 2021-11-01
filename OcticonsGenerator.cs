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
            var source = @"
namespace BlazorOcticons {
    public class Test {
        public string Title { get; set; }
    }
}";
            context.AddSource("Test.cs", SourceText.From(source, Encoding.UTF8));
        }
    }
}
