using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Generator
{
	[Generator(LanguageNames.CSharp)]
	public class Generator : IIncrementalGenerator
	{
		private const string TriggerAttribute = "Target.TriggerAttribute";

		public void Initialize(IncrementalGeneratorInitializationContext initContext)
		{
			//System.Diagnostics.Debugger.Launch();
			var valueProvider =
				initContext.SyntaxProvider.CreateSyntaxProvider(
					(node, _) => node is AttributeSyntax, // TODO pre-check name
					Transform)
				.Where(o => o != default)
				.WithComparer(GeneratorInputComparer.Instance);
				//.Collect();
			initContext.RegisterSourceOutput(valueProvider, Generate);
		}

		private static GeneratorInput Transform(GeneratorSyntaxContext syntaxContext, CancellationToken cancel)
		{
			var symbolInfo = syntaxContext.SemanticModel.GetSymbolInfo(syntaxContext.Node, cancel);
			// because there is still no official way to get a qualified name from an INamedTypeSymbol!
			var qualifiedName = symbolInfo.Symbol.ContainingType.ToString();
			if (qualifiedName != TriggerAttribute) return default;
			var classNode = syntaxContext.Node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
			var classSymbol = syntaxContext.SemanticModel.GetDeclaredSymbol(classNode, cancel) as INamedTypeSymbol;
			var input = new GeneratorInput()
			{
				Name = classSymbol.Name,
				Sources = classSymbol.DeclaringSyntaxReferences.Select(o => o.GetSyntax().GetText())
			};
			return input;
		}

		private void Generate(SourceProductionContext sourceContext, GeneratorInput input)
		{
			//foreach (var input in inputs)
			//{
				sourceContext.AddSource($"{input.Name}.g.cs", $"// Generated at {DateTime.Now}. [Changed={input.Changed}]");
			//}
		}

		private class GeneratorInput
		{
			internal string Name { get; set; }
			internal IEnumerable<SourceText> Sources { get; set; }
			internal bool Changed { get; set; }
		}

		private class GeneratorInputComparer : IEqualityComparer<GeneratorInput>
		{
			internal static readonly GeneratorInputComparer Instance = new();

			public bool Equals(GeneratorInput x, GeneratorInput y)
			{
				if (x == null || y == null) return false;
				var equal = Enumerable.SequenceEqual(x.Sources, y.Sources, SourceTextComparer.Instance);
				y.Changed = !equal;
				return equal;
			}
				 

			public int GetHashCode(GeneratorInput obj) => 1;
		}

		private class SourceTextComparer : IEqualityComparer<SourceText>
		{
			internal static readonly SourceTextComparer Instance = new();

			public bool Equals(SourceText x, SourceText y) =>
				x != null & y != null && x.ContentEquals(y);

			public int GetHashCode(SourceText obj) => 1;
		}
	}
}
