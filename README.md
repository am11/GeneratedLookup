# 🔍 GeneratedLookup

**GeneratedLookup** is a C# source generator that builds fast, zero-allocation lookup methods at **compile time**.  
It's perfect for static mapping of string keys to constant values like types, strings, or integers — with zero runtime overhead.

## ✨ Features

- 🔒 Immutable static lookup
- ⚡ Zero allocations
- 🧠 Compile-time source generation
- 🚀 Ideal for interpreters, compilers, config maps, DSLs

---

## 🚀 Usage

Add the `[GeneratedLookup<T>]` attribute to a `partial static` method:

```csharp
using GeneratedLookup;

if (MyLookups.LookupType("string", out var type))
    Console.WriteLine(type); // System.String

if (MyLookups.LookupString("fruit", out var fruit))
    Console.WriteLine(fruit); // apple

if (MyLookups.LookupInt("radix", out var index))
    Console.WriteLine(index); // 8

public static partial class MyLookups
{
    [GeneratedLookup<Type>(
        ["number", "string", "object"],
        [typeof(int), typeof(string), typeof(object)])]
    public static partial bool LookupType(ReadOnlySpan<char> key, out Type value);

    [GeneratedLookup<string>(
        ["fruit", "flavor", "color"],
        ["apple", "peachy", "red"])]
    public static partial bool LookupString(ReadOnlySpan<char> key, out string value);

    [GeneratedLookup<int>(
        ["index", "offset", "radix"],
        [42, -42, 8])]
    public static partial bool LookupInt(ReadOnlySpan<char> key, out int value);
}
```

---

## 🛠 How it works

At build time, the generator produces highly optimized lookup methods using:

- A single concatenated key span
- Parallel arrays for offsets and lengths
- Minimal control flow
- Static readonly spans to avoid allocations

```csharp
ReadOnlySpan<char> keysConcat = "numberstringobject";
ReadOnlySpan<int> keyOffsets = [0, 6, 12];
ReadOnlySpan<int> keyLengths = [6, 6, 6];
ReadOnlySpan<Type> values = [typeof(int), typeof(string), typeof(object)];
```

It performs a manual, efficient substring match to return the correct value — all without dictionary lookups or heap allocations.

---

## 🧪 Testing

Generated methods can be compiled and tested dynamically using `Expression` trees or delegates. See `LookupDelegate` pattern in the tests for working examples.

---

## 📦 Requirements

- .NET 7+
- Roslyn Source Generators
- No runtime dependencies

---

## 💡 Ideal use cases

- Token-to-value maps
- Interpreters or transpilers
- Static configuration keys
- Fast constant dispatching

---

## 📄 License

MIT
