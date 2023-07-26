﻿using System.Collections.Immutable;
using System.Text;
using System.Xml;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MethodsForPropertiesGenerator;

[Generator(LanguageNames.CSharp)]
public class MethodsForPropertiesGenerator : IIncrementalGenerator
{
    private static readonly ImmutableHashSet<string> _reservedKeywords = SyntaxFacts.GetReservedKeywordKinds().Select(SyntaxFacts.GetText).ToImmutableHashSet(StringComparer.InvariantCultureIgnoreCase);

    private const string Indentation = "    ";

    private const int IndentationLength = 4;

    private const string IEnumerableOfTQualifiedName = "System.Collections.Generic.IEnumerable`1";

    private const string AttributeUsageAttributeQualifiedName = "System.AttributeUsageAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var typeSymbols = context.SyntaxProvider.CreateSyntaxProvider(FilterNodes, (context, cancellationToken) => (INamedTypeSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node)!);

        context.RegisterSourceOutput(typeSymbols, (context, source) => context.AddSource($"{source.ToQualifiedName()}.g.cs", SourceText.From(GenerateMethods(source), Encoding.UTF8)));
    }

    private bool FilterNodes(SyntaxNode node, CancellationToken cancellationToken)
    {
        if (node is TypeDeclarationSyntax typeDeclarationSyntax && typeDeclarationSyntax.Modifiers.Any(SyntaxKind.PublicKeyword))
        {
            var name = typeDeclarationSyntax.Identifier.ToString();
            return name.EndsWith("Properties") || name.EndsWith("Options");
        }

        return false;
    }

    private string GenerateMethods(INamedTypeSymbol typeSymbol)
    {
        StringWriter stringWriter = new();
        WriteAutoGeneratedComment(stringWriter);
        stringWriter.WriteLine();

        WriteNullableDirective(stringWriter);
        stringWriter.WriteLine();

        WriteNamespace(stringWriter, typeSymbol);
        stringWriter.WriteLine();

        WriteTypeDeclaration(stringWriter, typeSymbol);
        stringWriter.Write("{");
        foreach (var property in GetProperties(typeSymbol))
        {
            WriteWithMethod(stringWriter, property);

            WriteAddMethodsIfApplicable(stringWriter, property);
        }
        stringWriter.WriteLine("}");

        return stringWriter.ToString();
    }

    private void WriteAutoGeneratedComment(StringWriter stringWriter)
    {
        stringWriter.WriteLine("// <auto-generated/>");
    }

    private static void WriteNullableDirective(StringWriter stringWriter)
    {
        stringWriter.WriteLine("#nullable enable");
    }

    private static void WriteNamespace(StringWriter stringWriter, ITypeSymbol typeSymbol)
    {
        var containingNamespace = typeSymbol.ContainingNamespace;
        if (containingNamespace.IsGlobalNamespace)
            return;

        stringWriter.Write("namespace ");
        stringWriter.Write(containingNamespace.ToDisplayString());
        stringWriter.WriteLine(";");
    }

    private static void WriteTypeDeclaration(StringWriter stringWriter, ITypeSymbol typeSymbol)
    {
        stringWriter.Write("partial ");
        stringWriter.Write(typeSymbol switch
        {
            { TypeKind: TypeKind.Interface } => "interface ",
            { IsReferenceType: true, IsRecord: false } => "class ",
            { IsReferenceType: true, IsRecord: true } => "record ",
            { IsValueType: true, IsRecord: false } => "struct ",
            { IsValueType: true, IsRecord: true } => "record struct ",
            _ => throw new ArgumentException($"Unsupported type kind: {typeSymbol.TypeKind}", nameof(typeSymbol)),
        });
        stringWriter.WriteLine(typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
    }

    private static IEnumerable<PropertyData> GetProperties(INamedTypeSymbol typeSymbol)
    {
        bool inherited = false;
        var symbol = typeSymbol;
        do
        {
            foreach (var property in symbol.GetMembers()
                                           .OfType<IPropertySymbol>()
                                           .Where(p => p.SetMethod is not null))
            {
                yield return new(property, typeSymbol, inherited);
            }

            inherited = true;
        }
        while ((symbol = symbol!.BaseType) is not null);
    }

    private void WriteWithMethod(StringWriter stringWriter, PropertyData property)
    {
        var symbol = property.Symbol;
        var containingType = property.ContainingType.ToDisplayString();
        var propertyName = symbol.Name;
        var parameterName = ToCamelCaseName(propertyName);

        stringWriter.WriteLine();

        WriteXmlComment(stringWriter, symbol);

        CopyApplicableAttributes(stringWriter, symbol);

        WriteIndentation(stringWriter, 1);
        stringWriter.Write("public ");
        if (property.Inherited)
            stringWriter.Write("new ");
        stringWriter.Write(containingType);
        stringWriter.Write(" With");
        stringWriter.Write(propertyName);
        stringWriter.Write("(");
        stringWriter.Write(symbol.Type.ToDisplayString());
        stringWriter.Write(" ");
        stringWriter.Write(parameterName);
        stringWriter.WriteLine(")");

        WriteIndentation(stringWriter, 1);
        stringWriter.WriteLine("{");

        WriteIndentation(stringWriter, 2);
        stringWriter.Write(propertyName);
        stringWriter.Write(" = ");
        stringWriter.Write(parameterName);
        stringWriter.WriteLine(";");

        WriteIndentation(stringWriter, 2);
        stringWriter.WriteLine("return this;");

        WriteIndentation(stringWriter, 1);
        stringWriter.WriteLine("}");
    }

    private void WriteAddMethodsIfApplicable(StringWriter stringWriter, PropertyData property)
    {
        var symbol = property.Symbol;

        if (symbol.Type is not INamedTypeSymbol propertyType || propertyType.ToQualifiedName() != IEnumerableOfTQualifiedName)
            return;

        var inherited = property.Inherited;
        var containingType = property.ContainingType.ToDisplayString();
        var genericType = propertyType.TypeArguments[0].ToDisplayString();
        var propertyName = symbol.Name;
        var parameterName = ToCamelCaseName(propertyName);

        // First overload
        stringWriter.WriteLine();

        WriteXmlComment(stringWriter, symbol);

        CopyApplicableAttributes(stringWriter, symbol);

        WriteIndentation(stringWriter, 1);
        stringWriter.Write($"public ");
        if (inherited)
            stringWriter.Write("new ");
        stringWriter.Write(containingType);
        stringWriter.Write(" Add");
        stringWriter.Write(propertyName);
        stringWriter.Write("(System.Collections.Generic.IEnumerable<");
        stringWriter.Write(genericType);
        stringWriter.Write("> ");
        stringWriter.Write(parameterName);
        stringWriter.WriteLine(")");

        WriteIndentation(stringWriter, 1);
        stringWriter.WriteLine("{");

        WriteIndentation(stringWriter, 2);
        stringWriter.Write("var temp");
        stringWriter.Write(propertyName);
        stringWriter.Write(" = ");
        stringWriter.Write(propertyName);
        stringWriter.WriteLine(";");

        WriteIndentation(stringWriter, 2);
        stringWriter.Write("return With");
        stringWriter.Write(propertyName);
        stringWriter.Write("(temp");
        stringWriter.Write(propertyName);
        stringWriter.Write(" is null ? (");
        stringWriter.Write(parameterName);
        stringWriter.Write(" ?? throw new System.ArgumentNullException(nameof(");
        stringWriter.Write(parameterName);
        stringWriter.Write("))) : System.Linq.Enumerable.Concat(temp");
        stringWriter.Write(propertyName);
        stringWriter.Write(", ");
        stringWriter.Write(parameterName);
        stringWriter.WriteLine("));");

        WriteIndentation(stringWriter, 1);
        stringWriter.WriteLine("}");

        // Second overload
        stringWriter.WriteLine();

        WriteXmlComment(stringWriter, symbol);

        CopyApplicableAttributes(stringWriter, symbol);

        WriteIndentation(stringWriter, 1);
        stringWriter.Write($"public ");
        if (inherited)
            stringWriter.Write("new ");
        stringWriter.Write(containingType);
        stringWriter.Write(" Add");
        stringWriter.Write(propertyName);
        stringWriter.Write("(params ");
        stringWriter.Write(genericType);
        stringWriter.Write("[] ");
        stringWriter.Write(parameterName);
        stringWriter.WriteLine(")");

        WriteIndentation(stringWriter, 1);
        stringWriter.WriteLine("{");

        WriteIndentation(stringWriter, 2);
        stringWriter.Write("return Add");
        stringWriter.Write(propertyName);
        stringWriter.Write("(");
        stringWriter.Write("(System.Collections.Generic.IEnumerable<");
        stringWriter.Write(genericType);
        stringWriter.Write(">)");
        stringWriter.Write(parameterName);
        stringWriter.WriteLine(");");

        WriteIndentation(stringWriter, 1);
        stringWriter.WriteLine("}");
    }

    private static void WriteXmlComment(StringWriter stringWriter, IPropertySymbol property)
    {
        var comment = property.GetDocumentationCommentXml();
        if (string.IsNullOrEmpty(comment))
            return;

        using StringReader stringReader = new(comment);
        using var xmlReader = XmlReader.Create(stringReader);
        xmlReader.Read();
        var trimmedComment = xmlReader.ReadInnerXml();

        using StringReader commentReader = new(trimmedComment);

        string commentLine;
        if (!string.IsNullOrWhiteSpace(commentLine = commentReader.ReadLine()))
            AppendCommentLine(commentLine);

        while ((commentLine = commentReader.ReadLine()) != null)
            AppendCommentLine(commentLine);

        void AppendCommentLine(string commentLine)
        {
            WriteIndentation(stringWriter, 1);
            stringWriter.Write("/// ");
            if (commentLine.StartsWith(Indentation))
                stringWriter.Write(commentLine.AsSpan(IndentationLength));
            else
                stringWriter.Write(commentLine);

            stringWriter.WriteLine();
        }
    }

    private static void CopyApplicableAttributes(StringWriter stringWriter, IPropertySymbol propertySymbol)
    {
        foreach (var attribute in propertySymbol.GetAttributes()
                                                .Where(a => IsAttributeApplicable(a, AttributeTargets.Method)))
        {
            WriteAttribute(stringWriter, attribute);
        }
    }

    private static bool IsAttributeApplicable(AttributeData attribute, AttributeTargets targets)
    {
        var attributeUsageAttribute = attribute.AttributeClass!.GetAttributes()
                                                               .FirstOrDefault(a => a.AttributeClass!.ToQualifiedName() == AttributeUsageAttributeQualifiedName);

        return attributeUsageAttribute is null || ((AttributeTargets)attributeUsageAttribute.ConstructorArguments[0].Value!).HasFlag(targets);
    }

    private static void WriteAttribute(StringWriter stringWriter, AttributeData attribute)
    {
        WriteIndentation(stringWriter, 1);
        stringWriter.Write("[");
        attribute.ApplicationSyntaxReference!.GetSyntax().WriteTo(stringWriter);
        stringWriter.WriteLine("]");
    }

    private Span<char> ToCamelCaseName(string name)
    {
        if (_reservedKeywords.Contains(name))
        {
            Span<char> copy = new char[name.Length + 1];
            copy[0] = '@';
            name.AsSpan(1).CopyTo(copy[2..]);
            copy[1] = char.ToLowerInvariant(name[0]);
            return copy;
        }
        else
        {
            Span<char> copy = new char[name.Length];
            name.AsSpan(1).CopyTo(copy[1..]);
            copy[0] = char.ToLowerInvariant(name[0]);
            return copy;
        }
    }

    private static void WriteIndentation(StringWriter stringWriter, int indentation)
    {
        for (int i = 0; i < indentation; i++)
            stringWriter.Write(Indentation);
    }
}
