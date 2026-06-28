using System;
using MidgardStudio.Core.Model;
using MidgardStudio.Core.Schema;
using MidgardStudio.Core.Validation;

namespace MidgardStudio.Core.Schemas;

/// <summary>
/// Schema for mob_avail (MOB_AVAIL_DB v1): re-skin a mob by pointing it at an existing sprite — another mob, a
/// player <c>JOB_*</c>, or an NPC sprite. Data lives only in the import layer. The 10 cosmetic fields are
/// "job-only" (rAthena rejects the entry if set while Sprite isn't a player job), so they show only when
/// Sprite is a job. PetEquip is gated on the Mob being a pet (a context-aware applicability check). See
/// ADR-0005 / [[rathena-client]] ▸ "Mob sprite: reuse vs register".
/// </summary>
public static class MobAvailSchema
{
    private const string GId = "Identity";
    private const string GDisguise = "Disguise (job sprite)";
    private const string GPet = "Pet";
    private const string GOptions = "Options";

    /// <summary>Job-only fields apply only when Sprite resolves to a player job constant.</summary>
    private static bool JobSprite(DbRecord r) => MobAvailConstants.IsJobSprite(r.GetString("Sprite"));

    private static FieldSchema ItemRef(string name, string label, string group, Func<DbRecord, bool>? applicable = null) => new()
    {
        Name = name,
        Label = label,
        Kind = FieldKind.Reference,
        Group = group,
        Enum = EnumSource.Reference(name, "item_db"),
        ReferenceSeverity = ValidationSeverity.Error, // rAthena rejects the whole entry on an invalid item
        IsApplicable = applicable,
    };

    public static readonly DbSchema Instance = new()
    {
        Id = "mob_avail",
        DisplayName = "Mob Sprite Reuse",
        HeaderType = "MOB_AVAIL_DB",
        HeaderVersion = 1,
        Key = KeyStrategy.Str("Mob"),
        Layout = new FileLayout
        {
            RenewalFiles = Array.Empty<string>(),
            PreRenewalFiles = Array.Empty<string>(),
            ImportFile = "import/mob_avail.yml",
        },
        Fields = new[]
        {
            new FieldSchema
            {
                Name = "Mob", Label = "Mob", Kind = FieldKind.Reference, IsKey = true, IsDisplay = true, Group = GId,
                Enum = EnumSource.Reference("AvailMob", "mob_db"), ReferenceSeverity = ValidationSeverity.Error,
                Description = "The mob to disguise (the entry's key).",
            },
            new FieldSchema
            {
                Name = "Sprite", Label = "Sprite", Kind = FieldKind.Reference, Group = GId,
                Enum = EnumSource.Reference("AvailSprite", MobAvailConstants.SpriteRefDb),
                Description = "What the mob looks like — another mob, a player JOB_* sprite, or an NPC sprite constant.",
            },

            new FieldSchema { Name = "Sex", Label = "Sex", Kind = FieldKind.Enum, Enum = MobAvailConstants.Sex, Default = "Female", Group = GDisguise, IsApplicable = JobSprite },
            new FieldSchema { Name = "HairStyle", Label = "Hair Style", Kind = FieldKind.Int, Min = 0, Default = 0, Group = GDisguise, IsApplicable = JobSprite },
            new FieldSchema { Name = "HairColor", Label = "Hair Color", Kind = FieldKind.Int, Min = 0, Default = 0, Group = GDisguise, IsApplicable = JobSprite },
            new FieldSchema { Name = "ClothColor", Label = "Cloth Color", Kind = FieldKind.Int, Min = 0, Default = 0, Group = GDisguise, IsApplicable = JobSprite },
            ItemRef("Weapon", "Weapon", GDisguise, JobSprite),
            ItemRef("Shield", "Shield", GDisguise, JobSprite),
            ItemRef("HeadTop", "Head Top", GDisguise, JobSprite),
            ItemRef("HeadMid", "Head Mid", GDisguise, JobSprite),
            ItemRef("HeadLow", "Head Low", GDisguise, JobSprite),
            ItemRef("Robe", "Robe", GDisguise, JobSprite),

            // PetEquip shows only when the Mob is a defined pet (a context-aware check against pet_db); a
            // validator also errors if it's somehow set while the Mob isn't a pet.
            new FieldSchema
            {
                Name = "PetEquip", Label = "Pet Equip", Kind = FieldKind.Reference, Group = GPet,
                Enum = EnumSource.Reference("PetEquip", "item_db"), ReferenceSeverity = ValidationSeverity.Error,
                Description = "Pet accessory — only applies when the Mob is a defined pet.",
                IsApplicableRefs = (r, idx) => idx.Contains("pet_db", r.GetString("Mob") ?? string.Empty),
            },

            FieldSchema.FlagsField("Options", "Options", MobAvailConstants.Options, GOptions),
        },
    };
}
