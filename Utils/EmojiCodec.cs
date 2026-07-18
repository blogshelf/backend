namespace backend.Utils;

public static class EmojiCodec
{
    private static readonly string[] Table =
    [
        // 0x00-0x0F 面部
        "\U0001F600", "\U0001F601", "\U0001F602", "\U0001F923", "\U0001F603", "\U0001F604", "\U0001F605", "\U0001F606",
        "\U0001F609", "\U0001F60A", "\U0001F60B", "\U0001F60E", "\U0001F60D", "\U0001F970", "\U0001F618", "\U0001F617",
        // 0x10-0x1F 面部续
        "\U0001F619", "\U0001F61A", "\U0001F642", "\U0001F917", "\U0001F929", "\U0001F914", "\U0001FAE1", "\U0001F928",
        "\U0001F610", "\U0001F611", "\U0001F636", "\U0001FAE5", "\U0001F60F", "\U0001F612", "\U0001F644", "\U0001F62C",
        // 0x20-0x2F 手势
        "\U0001F44B", "\U0001F91A", "\U0001F590", "\U0001F64C", "\U0001F596", "\U0001FAF1", "\U0001FAF2", "\U0001FAF3",
        "\U0001FAF4", "\U0001F44C", "\U0001F90C", "\U0001F90F", "\U0001F91F", "\U0001F91E", "\U0001FAF0", "\U0001FAF8",
        // 0x30-0x3F 手势续
        "\U0001F918", "\U0001F919", "\U0001F448", "\U0001F449", "\U0001F446", "\U0001F595", "\U0001F447", "\U0001FAF6",
        "\U0001FAF5", "\U0001F44D", "\U0001F44E", "\U0001F44A", "\U0001F91B", "\U0001F91C", "\U0001F44F", "\U0001FAF7",
        // 0x40-0x4F 动物
        "\U0001F436", "\U0001F431", "\U0001F42D", "\U0001F439", "\U0001F430", "\U0001F98A", "\U0001F43B", "\U0001F43C",
        "\U0001F9F1", "\U0001F428", "\U0001F42F", "\U0001F981", "\U0001F42E", "\U0001F437", "\U0001F438", "\U0001F435",
        // 0x50-0x5F 动物续
        "\U0001F414", "\U0001F427", "\U0001F426", "\U0001F424", "\U0001F986", "\U0001F985", "\U0001F989", "\U0001F987",
        "\U0001F43A", "\U0001F417", "\U0001F434", "\U0001F984", "\U0001F41D", "\U0001FAB7", "\U0001F41B", "\U0001F98B",
        // 0x60-0x6F 植物/自然
        "\U0001F333", "\U0001F334", "\U0001F335", "\U0001F33E", "\U0001F33F", "\U0001F332", "\U0001F340", "\U0001F342",
        "\U0001F33A", "\U0001F33B", "\U0001F337", "\U0001F338", "\U0001F339", "\U0001FAB5", "\U0001F331", "\U0001F341",
        // 0x70-0x7F 天气
        "\U0001F321", "\U0001F324", "\U0001F325", "\U0001F326", "\U0001F327", "\U0001F328", "\U0001F329", "\U0001F32A",
        "\U0001F32B", "\U0001F32C", "\U0001F525", "\U0001F4A7", "\U0001F4A6", "\U0001F30A", "\U0001F30D", "\U0001F30E",
        // 0x80-0x8F 天空/宇宙
        "\U0001F30F", "\U0001F310", "\U0001F311", "\U0001F312", "\U0001F313", "\U0001F314", "\U0001F315", "\U0001F316",
        "\U0001F317", "\U0001F318", "\U0001F319", "\U0001F31A", "\U0001F31B", "\U0001F31C", "\U0001F31D", "\U0001F31E",
        // 0x90-0x9F 水果
        "\U0001F347", "\U0001F348", "\U0001F349", "\U0001F34A", "\U0001F34B", "\U0001F34C", "\U0001F34D", "\U0001F34E",
        "\U0001F34F", "\U0001F350", "\U0001F351", "\U0001F352", "\U0001F353", "\U0001FAD0", "\U0001F95D", "\U0001F345",
        // 0xA0-0xAF 蔬菜
        "\U0001F951", "\U0001F346", "\U0001F954", "\U0001F955", "\U0001F33D", "\U0001F336", "\U0001FAD1", "\U0001F952",
        "\U0001F96C", "\U0001F966", "\U0001F9C4", "\U0001F9C5", "\U0001F344", "\U0001F95C", "\U0001FAD8", "\U0001F330",
        // 0xB0-0xBF 食物
        "\U0001F354", "\U0001F355", "\U0001F356", "\U0001F357", "\U0001F358", "\U0001F359", "\U0001F35A", "\U0001F35B",
        "\U0001F35C", "\U0001F35D", "\U0001F35E", "\U0001F35F", "\U0001F360", "\U0001F361", "\U0001F362", "\U0001F363",
        // 0xC0-0xCF 交通
        "\U0001F697", "\U0001F695", "\U0001F699", "\U0001F68C", "\U0001F68E", "\U0001F3CE", "\U0001F692", "\U0001F691",
        "\U0001F6F0", "\U0001F6FB", "\U0001F69A", "\U0001F69B", "\U0001F69C", "\U0001F6F5", "\U0001F6F4", "\U0001F6F6",
        // 0xD0-0xDF 电子产品
        "\U0001F4F1", "\U0001F4BB", "\U0001F4F9", "\U0001F5A8", "\U0001F5B1", "\U0001FAE9", "\U0001F3AE", "\U0001F5C9",
        "\U0001F5BD", "\U0001F4BE", "\U0001F4BF", "\U0001F4C0", "\U0001F4FC", "\U0001F4F7", "\U0001F4F8", "\U0001F4F2",
        // 0xE0-0xEF 工具
        "\U0001F527", "\U0001F528", "\U0001F6E0", "\U0001F529", "\U0001FA9A", "\U0001F52A", "\U0001F6E1", "\U0001F6E2",
        "\U0001F48E", "\U0001FA9C", "\U0001F9F0", "\U0001F9F2", "\U0001FA9B", "\U0001FAE4", "\U0001FAE3", "\U0001FA99",
        // 0xF0-0xFF 运动
        "\U0001F3C0", "\U0001F3C8", "\U0001F3BE", "\U0001F3D0", "\U0001F3C9", "\U0001F3CF", "\U0001F3B1", "\U0001FA80",
        "\U0001F3D3", "\U0001F3D8", "\U0001F3D2", "\U0001F945", "\U0001F3D1", "\U0001F3D4", "\U0001F3D5", "\U0001F3D6"
    ];

    private static readonly Dictionary<string, byte> Reverse = Table
        .Select((e, i) => (e, i))
        .GroupBy(x => x.e, x => (byte)x.i)
        .ToDictionary(g => g.Key, g => g.First());

    public static string Encode(byte[] bytes)
    {
        return string.Concat(bytes.Select(b => Table[b]));
    }

#if DEBUG
    public static byte[] Decode(string emojis)
    {
        var result = new List<byte>();
        var i = 0;
        while (i < emojis.Length)
        {
            var matched = false;
            for (var len = Math.Min(12, emojis.Length - i); len > 0; len--)
            {
                var candidate = emojis.Substring(i, len);
                if (!Reverse.TryGetValue(candidate, out var b)) continue;
                result.Add(b);
                i += len;
                matched = true;
                break;
            }

            if (!matched) throw new ArgumentException($"Unknown emoji at {i}");
        }

        return result.ToArray();
    }
#endif
}