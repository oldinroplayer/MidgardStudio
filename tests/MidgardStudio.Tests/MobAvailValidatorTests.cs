using System.Linq;
using MidgardStudio.Core.Lookup;
using MidgardStudio.Core.Model;
using MidgardStudio.Core.Overlay;
using MidgardStudio.Core.Schema;
using MidgardStudio.Core.Schemas;
using MidgardStudio.Core.Validation;
using MidgardStudio.Core.Validation.Validators;

namespace MidgardStudio.Tests;

/// <summary>mob_avail's bespoke rules: job-only fields require a job sprite, PetEquip requires a pet mob.</summary>
public class MobAvailValidatorTests
{
    private static OverlayTable Table() => new(MobAvailSchema.Instance, new DbLayer(), new DbLayer(), "x");

    private static DbRecord Record(params (string Field, object Value)[] fields)
    {
        var r = new DbRecord(MobAvailSchema.Instance) { Origin = RecordOrigin.NewCustom };
        foreach (var (f, v) in fields) r.SetRaw(f, v);
        return r;
    }

    [Fact]
    public void Job_only_field_without_a_job_sprite_errors_with_a_clear_fix()
    {
        var r = Record(("Mob", "PORING"), ("Sprite", "BAPHOMET"), ("Weapon", "Knife")); // Sprite is a mob, not a job
        var issues = new MobAvailValidator().Validate(r, Table(), ValidationContext.Create(new InMemoryReferenceIndex())).ToList();

        var issue = Assert.Single(issues, i => i.RuleId == "MOBAVAIL.JOB_ONLY_FIELD");
        Assert.Equal(ValidationSeverity.Error, issue.Severity);
        Assert.Equal("Weapon", issue.Field);

        Assert.NotNull(issue.Fix);
        issue.Fix!.Apply();
        Assert.False(r.Has("Weapon")); // cleared
    }

    [Fact]
    public void Job_only_fields_are_fine_when_sprite_is_a_job()
    {
        var r = Record(("Sprite", "JOB_STALKER"), ("Weapon", "Knife"), ("Sex", "Male"));
        var issues = new MobAvailValidator().Validate(r, Table(), ValidationContext.Create(new InMemoryReferenceIndex()));
        Assert.DoesNotContain(issues, i => i.RuleId == "MOBAVAIL.JOB_ONLY_FIELD");
    }

    [Fact]
    public void PetEquip_errors_unless_the_mob_is_a_pet()
    {
        var r = Record(("Mob", "POPORING"), ("Sprite", "PORING"), ("PetEquip", "Backpack"));

        var notPet = new MobAvailValidator().Validate(r, Table(), ValidationContext.Create(new InMemoryReferenceIndex()));
        Assert.Contains(notPet, i => i.RuleId == "MOBAVAIL.PETEQUIP_NOT_PET");

        var idx = new InMemoryReferenceIndex().Add("pet_db", "POPORING"); // now it's a pet
        var asPet = new MobAvailValidator().Validate(r, Table(), ValidationContext.Create(idx));
        Assert.DoesNotContain(asPet, i => i.RuleId == "MOBAVAIL.PETEQUIP_NOT_PET");
    }
}
