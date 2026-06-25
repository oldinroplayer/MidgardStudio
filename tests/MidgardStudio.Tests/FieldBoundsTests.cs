using System.Linq;
using MidgardStudio.Core.Schema;
using MidgardStudio.Core.Schemas;
using Xunit;

namespace MidgardStudio.Tests;

public class FieldBoundsTests
{
    [Fact]
    public void Clamp_RespectsMinMax()
    {
        var f = new FieldSchema { Name = "Rate", Label = "Rate", Kind = FieldKind.Int, Min = 0, Max = 1_000_000 };
        Assert.Equal(1_000_000, f.Clamp(2_000_000));
        Assert.Equal(0, f.Clamp(-5));
        Assert.Equal(500, f.Clamp(500));
    }

    [Fact]
    public void Clamp_UnboundedIsNoOp()
    {
        var f = new FieldSchema { Name = "X", Label = "X", Kind = FieldKind.Int };
        Assert.Equal(999_999_999, f.Clamp(999_999_999));
        Assert.Equal(-12_345, f.Clamp(-12_345));
    }

    [Fact]
    public void SummonRate_IsBounded_0_to_1000000()
    {
        var rate = NestedField(MobSummonSchema.Instance, "Summon", "Rate");
        Assert.Equal(0, rate.Min);
        Assert.Equal(1_000_000, rate.Max);
    }

    [Fact]
    public void AbraProbability_IsBounded_0_to_10000()
    {
        var prob = NestedField(AbraDbSchema.Instance, "Probability", "Probability");
        Assert.Equal(0, prob.Min);
        Assert.Equal(10_000, prob.Max);
    }

    private static FieldSchema NestedField(DbSchema schema, string listField, string fieldName)
    {
        var list = schema.Fields.First(f => f.Name == listField);
        return list.ObjectSchema!.Fields.First(f => f.Name == fieldName);
    }
}
