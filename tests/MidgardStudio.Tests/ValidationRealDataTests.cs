using System.Collections.Generic;
using System.IO;
using System.Linq;
using MidgardStudio.Core.Lookup;
using MidgardStudio.Core.Schema;
using MidgardStudio.Core.Schemas;
using MidgardStudio.Core.Validation;
using MidgardStudio.Core.Workspace;

namespace MidgardStudio.Tests;

/// <summary>
/// Regression guard against the false-positive class the curated enum lists caused: rAthena's own
/// official base data must NEVER trip an enum/flag membership rule (every value it uses is valid by
/// definition). Runs the real databases through a full-scan validation and asserts no membership flags.
/// </summary>
public class ValidationRealDataTests
{
    [Fact]
    public void Official_base_data_produces_no_membership_false_positives()
    {
        var paths = WorkspacePaths.CreateDefault(WorkspaceConfigService.DefaultRepoRoot);
        if (!Directory.Exists(paths.ServerDbRoot)) return; // skip when repo data is absent

        DbSchema[] schemas =
        {
            ItemDbSchema.Instance,
            MobDbSchema.Instance,
            ItemGroupSchema.Instance,
            SkillDbSchema.Instance,
            AchievementDbSchema.Instance,
            PetDbSchema.Instance,
            MobSummonSchema.Instance,
            AbraDbSchema.Instance,
        };

        var loader = new WorkspaceLoader();
        var engine = ValidationEngine.CreateDefault();
        // Empty reference index => XREF checks are skipped; this test targets enum/flag membership only.
        var ctx = ValidationContext.Create(new InMemoryReferenceIndex(), ServerMode.Renewal);

        // Rules that represent a definite defect rAthena's own data cannot contain — they must never
        // fire on official base records. (Best-practice/Info nudges may legitimately fire and are excluded.)
        var mustBeClean = new HashSet<string>
        {
            "FIELD.ENUM_INVALID", "FIELD.FLAG_INVALID", "FIELD.MAXLENGTH", "FIELD.REQUIRED",
            "FIELD.BOUNDS", "SCRIPT.UNBALANCED", "ITEM.EQUIP_NO_LOC", "ITEM.LEVEL_ORDER",
            "MOB.ID_RANGE", "DUP.AEGISNAME",
        };

        var offenders = new List<string>();
        foreach (var schema in schemas)
        {
            var overlay = loader.LoadOverlay(schema, paths, ServerMode.Renewal);
            if (overlay.BaseCount == 0) continue;

            foreach (var issue in engine.ValidateOverlay(overlay, ValidationScope.FullScan, ctx))
                if (issue.RuleId is not null && mustBeClean.Contains(issue.RuleId))
                    offenders.Add($"{issue.RuleId} — {issue.DbId} #{issue.Key} [{issue.Field}]: {issue.Message}");
        }

        Assert.True(offenders.Count == 0,
            $"Official base data tripped {offenders.Count} rule(s) it never should (false positives). First offenders:\n"
            + string.Join("\n", offenders.Take(40)));
    }
}
