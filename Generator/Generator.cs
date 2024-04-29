using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
				initContext.SyntaxProvider.ForAttributeWithMetadataName(
					TriggerAttribute,
					(node, _) => node is ClassDeclarationSyntax,
					Transform)
				.WithComparer(TransformedComparer.Instance)
				.Collect();
			initContext.RegisterSourceOutput(valueProvider, Generate);
		}

		private void Generate(SourceProductionContext sourceContext, ImmutableArray<Transformed> inputs)
		{
			foreach(var input in inputs)
			{
				sourceContext.AddSource($"{input.Name}.g.cs", $"// Generated at {DateTime.Now}. [Changed: {input.Changed}]");
			}
		}

		private Transformed Transform(GeneratorAttributeSyntaxContext syntaxContext, CancellationToken _)
		{
			var symbol = syntaxContext.TargetSymbol as INamedTypeSymbol;
			var texts = symbol.DeclaringSyntaxReferences.Select(o => o.GetSyntax().GetText());
			return new Transformed() { Name = symbol.Name, SourceTexts = texts };
		}

		private class Transformed
		{
			internal string Name { get; set; }
			internal IEnumerable<SourceText> SourceTexts { get; set; }
			internal bool Changed { get; set; }
		}

		private class TransformedComparer : IEqualityComparer<Transformed>
		{
			internal static readonly TransformedComparer Instance = new();

			public bool Equals(Transformed x, Transformed y)
			{
				var equal = Enumerable.SequenceEqual(x.SourceTexts, y.SourceTexts, SourceTextComparer.Instance);
				y.Changed = !equal;
				return equal;
			}				

			public int GetHashCode(Transformed obj) => 1;
		}

		private class SourceTextComparer : IEqualityComparer<SourceText>
		{
			internal static readonly SourceTextComparer Instance = new();

			public bool Equals(SourceText x, SourceText y) => x.ContentEquals(y);

			public int GetHashCode(SourceText obj) => 1;
		}
	}
}
