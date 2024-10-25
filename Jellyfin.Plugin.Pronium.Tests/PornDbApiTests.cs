using Pronium.Helpers.Utils;
using Pronium.Sites;

namespace Pronium.UnitTests;

[TestFixture]
public class PornDbApiTests
{
    [SetUp]
    public void Setup()
    {
        Database.LoadAll();
    }

    private readonly PornDbApi _site = new();

    [Test]
    [TestCase(TestName = "{c}.{m}")]
    public async Task SearchIsWorking()
    {
        var result = await _site.Search(new[] { 48, 0 }, "Anal Maid Service", null, new CancellationToken());
        Assert.That(result.Count, Is.GreaterThan(0));
        var id = result[0].ProviderIds.Values.FirstOrDefault();
        Assert.That(id, Is.Not.Empty);
        Assert.That(id, Is.EqualTo("48#0#2751895"));
    }

    [Test]
    [TestCase(TestName = "{c}.{m}")]
    public async Task SearchForUniqueMovie()
    {
        var result = await _site.Search(new[] { 48, 1 }, "Casino - Endgame", null, new CancellationToken());
        Assert.That(result.Count, Is.GreaterThan(0));
        var id = result[0].ProviderIds.Values.FirstOrDefault();
        Assert.That(id, Is.Not.Empty);
        Assert.That(id, Is.EqualTo("48#1#5159484"));
    }

    [Test]
    [TestCase(TestName = "{c}.{m}")]
    public async Task SearchForMoviesIsWorking()
    {
        var result = await _site.Search(new[] { 48, 1 }, "Dark Woods", null, new CancellationToken());
        Assert.That(result.Count, Is.GreaterThan(0));
        var id = result[0].ProviderIds.Values.FirstOrDefault();
        Assert.That(id, Is.Not.Empty);
        Assert.That(id, Is.EqualTo("48#1#5273528"));
    }

    [Test]
    [TestCase(TestName = "{c}.{m}")]
    public async Task UpdateIsWorking()
    {
        var result = await _site.Update(new[] { 48, 0 }, new[] { "2751895" }, new CancellationToken());
        Assert.That(result.Item.Name, Is.EqualTo("Anal Maid Service"));
        Assert.That(result.Item.OriginalTitle, Is.EqualTo("mydirtymaid - 2023-02-23 - Anal Maid Service"));
        Assert.That(result.Item.Overview, Is.Not.Empty);
        Assert.That(result.Item.Studios.Length, Is.EqualTo(2));
        Assert.That(result.Item.Genres.Length, Is.EqualTo(15));
        Assert.That(result.People.Count, Is.EqualTo(2));
    }

    [Test]
    [TestCase(TestName = "{c}.{m}")]
    public async Task AbbreviationAreUsed()
    {
        var result = await _site.Update(new[] { 48, 0 }, new[] { "802284" }, new CancellationToken());
        Assert.That(result.Item.OriginalTitle, Is.EqualTo("rws - 2012-10-30 - Bride Of Frankendick"));
    }

    [Test]
    [TestCase(TestName = "{c}.{m}")]
    public async Task AbbreviationAreUsedWithSpecialChars()
    {
        var result = await _site.Update(new[] { 48, 0 }, new[] { "1978334" }, new CancellationToken());
        Assert.That(result.Item.OriginalTitle, Is.EqualTo("mshf - 2022-07-09 - Busty Redhead, Harper Red, Is So Wet and Horny That She Must Get Dick in the Cafe This Instant"));
    }

    [Test]
    [TestCase(TestName = "{c}.{m}")]
    public async Task UpdateForMoviesIsWorking()
    {
        var result = await _site.Update(new[] { 48, 1 }, new[] { "4697054" }, new CancellationToken());
        Assert.That(result.Item.Name, Is.EqualTo("Dark Woods"));
        Assert.That(result.Item.OriginalTitle, Is.EqualTo("dpg - 2023-04-11 - Dark Woods"));
        Assert.That(result.Item.Overview, Is.Not.Empty);
        Assert.That(result.Item.Studios.Length, Is.EqualTo(1));
        Assert.That(result.Item.Genres.Length, Is.EqualTo(6));
        Assert.That(result.People.Count, Is.EqualTo(5));
    }

    [Test]
    [TestCase(TestName = "{c}.{m}")]
    public async Task GetImagesIsWorking()
    {
        var result = (await _site.GetImages(new[] { 48, 0 }, new[] { "2751895" }, null, new CancellationToken())).ToList();

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.First().Url, Does.Contain("my-dirty-maid-anal-maid"));
    }
}
