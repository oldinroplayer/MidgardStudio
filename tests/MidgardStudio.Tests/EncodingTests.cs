using System.IO;
using System.Linq;
using System.Text;
using MidgardStudio.Core.Lua;
using MidgardStudio.Core.Schemas;
using MidgardStudio.Core.Serialization;
using Xunit;

namespace MidgardStudio.Tests;

/// <summary>
/// Covers the per-profile Display Encoding feature: server YAML is read as UTF-8 when valid and falls
/// back to the profile codepage for legacy/translated dbs, while loose client lua round-trips in the
/// configured codepage (so non-Western — e.g. Korean — client text saves correctly).
/// </summary>
public class EncodingTests
{
    private const string YamlBody =
        "Header:\n  Type: ITEM_DB\n  Version: 1\nBody:\n  - Id: 501\n    AegisName: Red_Potion\n    Name: Poção Vermelha\n";

    static EncodingTests() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    [Fact]
    public void LegacyLatin1Db_DecodesWithCodepageFallback()
    {
        // A translated item_db saved as Windows-1252 is NOT valid UTF-8; reading it as UTF-8 would yield
        // U+FFFD. The codepage fallback recovers the real name.
        string path = WriteTemp(Encoding.GetEncoding(1252).GetBytes(YamlBody));
        try
        {
            var file = new YamlDbReader().ReadFile(path, ItemDbSchema.Instance, fallbackCodepage: 1252);
            Assert.Equal("Poção Vermelha", file.Records.Single().GetString("Name"));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Utf8Db_StillDecodesCorrectly_EvenWithNonUtf8Fallback()
    {
        // Standard rAthena db is UTF-8 — auto-detect must keep using UTF-8 and NOT mojibake it through the
        // fallback codepage, regardless of which codepage the profile picked.
        string path = WriteTemp(new UTF8Encoding(false).GetBytes(YamlBody));
        try
        {
            var file = new YamlDbReader().ReadFile(path, ItemDbSchema.Instance, fallbackCodepage: 949);
            Assert.Equal("Poção Vermelha", file.Records.Single().GetString("Name"));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Utf8BomDb_IsStripped()
    {
        byte[] bom = { 0xEF, 0xBB, 0xBF };
        string path = WriteTemp(bom.Concat(new UTF8Encoding(false).GetBytes(YamlBody)).ToArray());
        try
        {
            var file = new YamlDbReader().ReadFile(path, ItemDbSchema.Instance);
            Assert.Equal("Poção Vermelha", file.Records.Single().GetString("Name"));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ClientCodec_Korean949_RoundTripsHangul()
    {
        // Korean client lua must read AND write as EUC-KR (949): saving Hangul to a 1252 file would throw.
        var codec = new LuaFileCodec(949);
        const string hangul = "한국어 아이템 이름";
        string path = WriteTemp(codec.EncodeText(hangul));
        try
        {
            Assert.Equal(hangul, codec.ReadText(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ClientCodec_1252_RejectsHangul_WithClearMessage()
    {
        // Under a Western profile, non-representable client text is refused (not silently turned into '?').
        var codec = new LuaFileCodec(1252);
        var ex = Assert.Throws<InvalidDataException>(() => codec.EncodeText("한국어"));
        Assert.Contains("codepage 1252", ex.Message);
    }

    private static string WriteTemp(byte[] bytes)
    {
        string path = Path.GetTempFileName();
        File.WriteAllBytes(path, bytes);
        return path;
    }
}
