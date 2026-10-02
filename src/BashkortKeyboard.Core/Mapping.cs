namespace BashkortKeyboard.Core;

public sealed record Mapping(int VirtualKey, char Russian, char Bashkir)
{
    public string Label => $"{char.ToUpperInvariant(Russian)} → {char.ToUpperInvariant(Bashkir)}";
    public static readonly Mapping[] All =
    [
        new(0x46, 'а', 'ә'), new(0x4A, 'о', 'ө'), new(0x45, 'у', 'ү'),
        new(0x52, 'к', 'ҡ'), new(0x55, 'г', 'ғ'), new(0x43, 'с', 'ҫ'),
        new(0x50, 'з', 'ҙ'), new(0xDB, 'х', 'һ'), new(0x59, 'н', 'ң')
    ];
}
