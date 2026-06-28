using MidgardStudio.Core.Model;
using MidgardStudio.Core.Schemas;
using MidgardStudio.Core.Validation;

namespace MidgardStudio.Tests;

/// <summary>
/// The item cross-file rules, now in Core, tested through fake ports — the logic that was previously
/// stranded in the untestable App WorkspaceValidator. Fixes are verified by checking the editor port.
/// </summary>
public class ItemClientFileValidatorTests
{
    private sealed class FakeClient : IClientItemProbe
    {
        public readonly Dictionary<int, ClientItemFacts> Items = new();
        public bool Exists(int id) => Items.ContainsKey(id);
        public ClientItemFacts? Get(int id) => Items.TryGetValue(id, out var f) ? f : null;
    }

    private sealed class FakeGrf : IGrfIconProbe
    {
        public bool IsConfigured { get; set; }
        public readonly HashSet<string> Icons = new();
        public bool IconExists(string r) => Icons.Contains(r);
    }

    private sealed class FakeAccmap : IAccessoryMapProbe
    {
        public bool IsAvailable { get; set; }
        public readonly HashSet<int> Mapped = new();
        public IReadOnlySet<int> MappedViewIds() => Mapped;
    }

    private sealed class RecordingEditor : IClientItemEditor
    {
        public readonly List<string> Calls = new();
        public void SetSlots(int id, int slots) => Calls.Add($"SetSlots({id},{slots})");
        public void SetClassNum(int id, int classNum) => Calls.Add($"SetClassNum({id},{classNum})");
        public void CreateText(int id, string name, int slots, int classNum) => Calls.Add($"CreateText({id})");
        public void Remove(int id) => Calls.Add($"Remove({id})");
    }

    private static DbRecord Item(int id, int slots = 0, int view = 0, string[]? locations = null)
    {
        var r = new DbRecord(ItemDbSchema.Instance);
        r.SetRaw("Id", id);
        r.SetRaw("AegisName", "ITEM_" + id);
        r.SetRaw("Name", "Item " + id);
        r.SetRaw("Slots", slots);
        r.SetRaw("View", view);
        if (locations is not null) r.SetRaw("Locations", new HashSet<string>(locations));
        return r;
    }

    private static List<ValidationIssue> Run(DbRecord item, FakeClient client,
        FakeGrf? grf = null, FakeAccmap? accmap = null, RecordingEditor? editor = null) =>
        ItemClientFileValidator.Validate(new[] { item }, ValidationScope.FullScan,
            client, grf ?? new FakeGrf(), accmap ?? new FakeAccmap(), editor ?? new RecordingEditor()).ToList();

    [Fact]
    public void Slot_mismatch_is_flagged_and_its_fix_sets_the_client_slots()
    {
        var client = new FakeClient();
        client.Items[501] = new ClientItemFacts(SlotCount: 0, ClassNum: 0, IdentifiedResourceName: null);
        var editor = new RecordingEditor();

        var issues = Run(Item(501, slots: 2), client, editor: editor);

        var issue = Assert.Single(issues);
        Assert.Equal("XFILE.SLOTCOUNT_MISMATCH", issue.RuleId);
        issue.Fix!.Apply();
        Assert.Contains("SetSlots(501,2)", editor.Calls);
    }

    [Fact]
    public void Matching_item_produces_no_issues()
    {
        var client = new FakeClient();
        client.Items[501] = new ClientItemFacts(SlotCount: 2, ClassNum: 0, IdentifiedResourceName: null);
        Assert.Empty(Run(Item(501, slots: 2), client));
    }

    [Fact]
    public void View_classnum_mismatch_is_ignored_for_non_headgear()
    {
        // The nuance the wiki calls out: View == ClassNum is enforced only for headgear/garment.
        var client = new FakeClient();
        client.Items[501] = new ClientItemFacts(SlotCount: 0, ClassNum: 0, IdentifiedResourceName: null);

        var issues = Run(Item(501, view: 5), client); // View 5 vs ClassNum 0, but no headgear location

        Assert.DoesNotContain(issues, i => i.RuleId == "XFILE.CLASSNUM_MISMATCH");
    }

    [Fact]
    public void Headgear_view_classnum_mismatch_is_flagged_and_fix_sets_classnum()
    {
        var client = new FakeClient();
        client.Items[501] = new ClientItemFacts(SlotCount: 0, ClassNum: 0, IdentifiedResourceName: null);
        var editor = new RecordingEditor();

        var issues = Run(Item(501, view: 5, locations: new[] { "Head_Top" }), client, editor: editor);

        var issue = issues.Single(i => i.RuleId == "XFILE.CLASSNUM_MISMATCH");
        issue.Fix!.Apply();
        Assert.Contains("SetClassNum(501,5)", editor.Calls);
    }

    [Fact]
    public void Missing_client_text_is_flagged_with_create_then_remove_fix()
    {
        var client = new FakeClient(); // item 501 absent
        var editor = new RecordingEditor();

        var issues = Run(Item(501, slots: 1, view: 2), client, editor: editor);

        var issue = issues.Single(i => i.RuleId == "XFILE.ITEM_NO_CLIENTTEXT");
        issue.Fix!.Apply();
        issue.Fix!.Revert!();
        Assert.Equal(new[] { "CreateText(501)", "Remove(501)" }, editor.Calls);
    }
}
