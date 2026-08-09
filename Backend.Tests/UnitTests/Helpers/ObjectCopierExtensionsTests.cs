using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class ObjectCopierExtensionsTests
{
    private sealed class SourceDto
    {
        public bool? UseOnlineVoting { get; set; }
        public bool? OnlineCloseIsEstimate { get; set; }
        public string? Name { get; set; }
    }

    private sealed class TargetEntity
    {
        public bool UseOnlineVoting { get; set; } = true;
        public bool OnlineCloseIsEstimate { get; set; } = true;
        public string Name { get; set; } = "old";
    }

    [Fact]
    public void CopyMatchingPropertiesTo_NullableBoolFalse_OverwritesNonNullableBoolTrue()
    {
        var source = new SourceDto
        {
            UseOnlineVoting = false,
            OnlineCloseIsEstimate = false,
            Name = "new",
        };
        var target = new TargetEntity();

        source.CopyMatchingPropertiesTo(target, ignoreNulls: true);

        Assert.False(target.UseOnlineVoting);
        Assert.False(target.OnlineCloseIsEstimate);
        Assert.Equal("new", target.Name);
    }

    [Fact]
    public void CopyMatchingPropertiesTo_IgnoreNulls_SkipsNullNullableBool()
    {
        var source = new SourceDto
        {
            UseOnlineVoting = null,
            Name = "kept-name-update",
        };
        var target = new TargetEntity
        {
            UseOnlineVoting = true,
            Name = "old",
        };

        source.CopyMatchingPropertiesTo(target, ignoreNulls: true);

        Assert.True(target.UseOnlineVoting);
        Assert.Equal("kept-name-update", target.Name);
    }
}
