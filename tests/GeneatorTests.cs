using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using GeneratedLookup;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace GeneratedLookup.Tests;

public class GeneratedLookupTests
{
    delegate bool LookupDelegate<T>(ReadOnlySpan<char> key, out T value);

    [Fact]
    public void LookupString_Test()
    {
        RunGeneratedLookupTest<string>(
            methodName: "LookupString",
            keys: ["a", "bb", "ccc"],
            values: ["A", "BB", "CCC"],
            hits: ["a", "bb", "ccc"],
            expectedResults: ["A", "BB", "CCC"],
            misses: ["zzz", "xyz"]
        );
    }

    [Fact]
    public void LookupInt_Test()
    {
        RunGeneratedLookupTest<int>(
            methodName: "LookupInt",
            keys: ["one", "two", "three"],
            values: [1, 2, 3],
            hits: ["one", "two"],
            expectedResults: [1, 2],
            misses: ["ten", "zero"]
        );
    }

    [Fact]
    public void LookupType_Test()
    {
        RunGeneratedLookupTest<Type>(
            methodName: "LookupType",
            keys: ["int", "string", "obj"],
            values: [typeof(int), typeof(string), typeof(object)],
            hits: ["int", "obj"],
            expectedResults: [typeof(int), typeof(object)],
            misses: ["float", "nope"]
        );
    }

    private static void RunGeneratedLookupTest<T>(
        string methodName,
        string[] keys,
        T[] values,
        string[] hits,
        T[] expectedResults,
        string[] misses)
    {
        string valueList = string.Join(", ", values.Select(v => FormatValue(v!)));
        string keyList = string.Join(", ", keys.Select(k => $"\"{k}\""));

        string sourceCode = $$"""
        using System;
        using GeneratedLookup;

        public static partial class MyLookupClass
        {
            [GeneratedLookup<{{typeof(T).Name}}>([{{keyList}}], [{{valueList}}])]
            public static partial bool {{methodName}}(ReadOnlySpan<char> key, out {{typeof(T).FullName}} value);
        }
        """;

        string generatedCode = GetGeneratedOutput(sourceCode);
        Assembly assembly = CompileGeneratedCode(sourceCode, generatedCode);

        MethodInfo method = assembly
            .GetType("MyLookupClass")!
            .GetMethod(methodName)!;

        var spanParam = Expression.Parameter(typeof(ReadOnlySpan<char>), "key");
        var outParam = Expression.Parameter(typeof(T).MakeByRefType(), "value");

        foreach ((string key, T expected) in hits.Zip(expectedResults, (k, v) => (k, v)))
        {
            // Create the method call expression
            var callExpr = Expression.Call(method, spanParam, outParam);

            // Build the lambda expression
            var lambda = Expression.Lambda<LookupDelegate<T>>(callExpr, spanParam, outParam).Compile();

            // Call the lambda
            bool success = lambda(key.AsSpan(), out T result);

            Assert.True(success);
            Assert.Equal(expected, result);
        }

        foreach (string key in misses)
        {
            // Create the method call expression
            var callExpr = Expression.Call(method, spanParam, outParam);

            // Build the lambda expression
            var lambda = Expression.Lambda<LookupDelegate<T>>(callExpr, spanParam, outParam).Compile();

            // Call the lambda
            bool success = lambda(key.AsSpan(), out T result);

            Assert.False(success);
            Assert.Equal(default(T), result);
        }

        static string FormatValue(object value)
        {
            return value switch
            {
                string s => $"\"{s}\"",
                Type t => $"typeof({t.FullName})",
                _ => value.ToString()!
            };
        }
    }

    private static string GetGeneratedOutput(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic)
            .Select(assembly => assembly.Location)
            .Where(assembly => !string.IsNullOrEmpty(assembly))
            .Select(s => MetadataReference.CreateFromFile(s))
            .Cast<MetadataReference>();

        var compilation = CSharpCompilation.Create(nameof(GeneratedLookupTests),
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new GeneratedLookup();

        CSharpGeneratorDriver.Create(generator)
            .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        Assert.Empty(diagnostics);

        return outputCompilation.SyntaxTrees.Skip(1).LastOrDefault()?.ToString() ??
            throw new InvalidOperationException($"Generated code cannot be null. Source: {sourceCode}");
    }

    private static Assembly CompileGeneratedCode(string sourceCode, string generatedCode)
    {
        // Compile the generated code into an assembly in memory
        var syntaxTreeSource = CSharpSyntaxTree.ParseText(sourceCode);
        var syntaxTreeGenerated = CSharpSyntaxTree.ParseText(generatedCode);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic)
            .Select(assembly => assembly.Location)
            .Where(assembly => !string.IsNullOrEmpty(assembly))
            .Select(s => MetadataReference.CreateFromFile(s))
            .Cast<MetadataReference>();

        var compilation = CSharpCompilation.Create(nameof(GeneratedLookupTests),
            [syntaxTreeSource, syntaxTreeGenerated],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using (var ms = new System.IO.MemoryStream())
        {
            var result = compilation.Emit(ms);
            if (!result.Success)
            {
                var errors = string.Join(Environment.NewLine, result.Diagnostics);
                throw new InvalidOperationException($"Compilation failed: {errors}");
            }

            ms.Seek(0, System.IO.SeekOrigin.Begin);
            return Assembly.Load(ms.ToArray());
        }
    }
}
