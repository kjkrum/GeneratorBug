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

				// The comparer sets Compared and Changed on
				// the transformed values when it compares them.
				// Putting WithComparer after Where seemed to
				// make no difference.
				.WithComparer(TransformedComparer.Instance)

				// Where #1: Nothing is generated because
				// this Where excludes everything. This makes
				// sense. Nothing is compared on the first
				// run, so the comparer never sets Changed. 
				//.Where(o => o.Changed)

				// Where #2: Add a condition to pass values
				// that have not been compared. As expected,
				// this passes both values on the first run
				// because Compared = false.
				.Where(o => !o.Compared || o.Changed)

				// On subsequent runs, Collect includes
				// values that DO NOT pass the Where; i.e.,
				// values with Compared = true, Changed = false.
				// Why doesn't it do this on the first run?
				.Collect();
			initContext.RegisterSourceOutput(valueProvider, Generate);
		}

		private void Generate(SourceProductionContext sourceContext, ImmutableArray<Transformed> inputs)
		{
			foreach(var input in inputs)
			{
				sourceContext.AddSource($"{input.Name}.g.cs", $"// Generated at {DateTime.Now}. [Compared: {input.Compared}, Changed: {input.Changed}]");
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
			internal bool Compared { get; set; }
			internal bool Changed { get; set; }
		}

		private class TransformedComparer : IEqualityComparer<Transformed>
		{
			internal static readonly TransformedComparer Instance = new();

			public bool Equals(Transformed x, Transformed y)
			{
				var equal = Enumerable.SequenceEqual(x.SourceTexts, y.SourceTexts, SourceTextComparer.Instance);
				x.Compared = y.Compared = true;
				x.Changed = y.Changed = !equal;
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
