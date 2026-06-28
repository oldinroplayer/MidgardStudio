using System;
using System.IO;
using System.Linq;
using MidgardStudio.Core.Model;
using MidgardStudio.Core.Schemas;
using MidgardStudio.Core.Serialization;

namespace MidgardStudio.Tests;

/// <summary>The full mob_avail field set round-trips through the generic schema-driven reader/writer
/// (mob/job/NPC sprites, the job-disguise fields, item references, and the Options flag map).</summary>
public class MobAvailSchemaTests
{
    private const string Yaml = """
        Header:
          Type: MOB_AVAIL_DB
          Version: 1
        Body:
          - Mob: PORING
            Sprite: BAPHOMET
          - Mob: BOW_GUARDIAN
            Sprite: JOB_ASSASSIN_CROSS
            Sex: Male
            HairStyle: 1
            HairColor: 1
            ClothColor: 1
            Weapon: Jamadhar
            HeadTop: Sahkkat
            Robe: Archangel_Wing
            Options:
              Falcon: true
          - Mob: E_OBEAUNE
            Sprite: 4_M_BARBER
            PetEquip: Backpack
        """;

    [Fact]
    public void Reads_all_fields_including_options_and_job_disguise()
    {
        var file = new YamlDbReader().Read(Yaml, MobAvailSchema.Instance);
        Assert.Equal(3, file.Records.Count);

        var guardian = file.Records[1];
        Assert.Equal("JOB_ASSASSIN_CROSS", guardian.GetString("Sprite"));
        Assert.Equal("Male", guardian.GetString("Sex"));
        Assert.Equal(1, guardian.GetInt("HairStyle"));
        Assert.Equal("Jamadhar", guardian.GetString("Weapon"));
        Assert.Equal("Archangel_Wing", guardian.GetString("Robe"));
        Assert.True(guardian.GetSet("Options")!.Contains("Falcon"));

        Assert.Equal("4_M_BARBER", file.Records[2].GetString("Sprite")); // NPC sprite preserved verbatim
        Assert.Equal("Backpack", file.Records[2].GetString("PetEquip"));
    }

    [Fact]
    public void Write_is_idempotent_and_emits_the_right_shape()
    {
        var schema = MobAvailSchema.Instance;
        var writer = new YamlDbWriter();
        var file = new YamlDbReader().Read(Yaml, schema);

        string first = writer.WriteToString(schema, file);
        Assert.Contains("Sprite: JOB_ASSASSIN_CROSS", first);
        Assert.Contains("Falcon: true", first);     // Options is a sparse name:true map
        Assert.Contains("PetEquip: Backpack", first);
        Assert.DoesNotContain("HairStyle: 0", first); // defaults omitted

        string second = writer.WriteToString(schema, new YamlDbReader().Read(first, schema));
        Assert.Equal(first, second);
    }

    [Fact]
    public void WriteFile_preserves_the_documented_import_file_end_to_end()
    {
        const string original =
            "# rAthena banner — keep me\n###\nHeader:\n  Type: MOB_AVAIL_DB\n  Version: 1\n\n#Body:\n#  - Mob: PORING\n#    Sprite: BAPHOMET\n";
        string dir = Path.Combine(Path.GetTempPath(), "midgard-ma-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "mob_avail.yml");
        try
        {
            File.WriteAllText(path, original);

            var file = new DbFile { HeaderType = "MOB_AVAIL_DB", HeaderVersion = 1 };
            var rec = new DbRecord(MobAvailSchema.Instance);
            rec.SetRaw("Mob", "E_OBEAUNE");
            rec.SetRaw("Sprite", "PORING");
            file.Records.Add(rec);
            new YamlDbWriter().WriteFile(path, MobAvailSchema.Instance, file);

            string after = File.ReadAllText(path);
            Assert.Contains("# rAthena banner — keep me", after); // banner preserved
            Assert.Contains("#    Sprite: BAPHOMET", after);       // commented example preserved
            Assert.Contains("Mob: E_OBEAUNE", after);              // the new entry was written
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best effort */ } }
    }

    [Fact]
    public void Sprite_field_uses_the_synthetic_mob_plus_job_source()
    {
        var sprite = MobAvailSchema.Instance.Field("Sprite");
        Assert.Equal(MobAvailConstants.SpriteRefDb, sprite!.Enum!.ReferenceDb);
        Assert.True(MobAvailConstants.IsJobSprite("JOB_STALKER"));
        Assert.False(MobAvailConstants.IsJobSprite("PORING"));
        Assert.Contains("JOB_STALKER", MobAvailConstants.Jobs);
    }
}
