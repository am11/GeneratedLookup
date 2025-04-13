using GeneratedLookup;

if (MyLookups.LookupType("string", out var num))
    Console.WriteLine(num);

if (MyLookups.LookupString("fruit", out var fruit))
    Console.WriteLine(fruit);

if (MyLookups.LookupInt("radix", out var index))
    Console.WriteLine(index);

public static partial class MyLookups
{
    [GeneratedLookup<Type>(
        ["number", "string", "object"],
        [typeof(int), typeof(string), typeof(object)])]
    public static partial bool LookupType(ReadOnlySpan<char> key, out Type value);

    [GeneratedLookup<string>(["fruit", "flavor", "color"], ["apple", "peachy", "red"])]
    public static partial bool LookupString(ReadOnlySpan<char> key, out string value);

    [GeneratedLookup<int>(["index", "offset", "radix"], [42, -42, 8])]
    public static partial bool LookupInt(ReadOnlySpan<char> key, out int value);
}
