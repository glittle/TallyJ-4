using Backend.Localization;

namespace Backend.Tests.UnitTests;

public class LocalizationPathResolverTests
{
    [Fact]
    public void Resolve_RelativePath_UsesContentRootWhenTheDirectoryExists()
    {
        var root = Path.Combine(Path.GetTempPath(), "tallyj-locales-" + Guid.NewGuid().ToString("N"));
        var locales = Path.Combine(root, "frontend", "src", "locales");
        Directory.CreateDirectory(locales);

        try
        {
            var resolved = LocalizationPathResolver.Resolve("../frontend/src/locales", Path.Combine(root, "backend"));

            Assert.Equal(Path.GetFullPath(locales), Path.GetFullPath(resolved));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Resolve_MissingRelativePath_ReturnsContentRootInterpretation()
    {
        var root = Path.Combine(Path.GetTempPath(), "tallyj-missing-locales-" + Guid.NewGuid().ToString("N"));
        var contentRoot = Path.Combine(root, "publish");
        Directory.CreateDirectory(contentRoot);

        try
        {
            var resolved = LocalizationPathResolver.Resolve("../frontend/src/locales", contentRoot);

            Assert.Equal(
                Path.GetFullPath(Path.Combine(contentRoot, "../frontend/src/locales")),
                Path.GetFullPath(resolved));
            Assert.False(Directory.Exists(resolved));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Resolve_PublishedLocalesFolder_IsUsedWhenConfiguredPathIsMissing()
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), "tallyj-published-" + Guid.NewGuid().ToString("N"));
        var published = Path.Combine(contentRoot, "locales");
        Directory.CreateDirectory(published);

        try
        {
            var resolved = LocalizationPathResolver.Resolve("../frontend/src/locales", contentRoot);

            Assert.Equal(Path.GetFullPath(published), Path.GetFullPath(resolved));
        }
        finally
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }
}
