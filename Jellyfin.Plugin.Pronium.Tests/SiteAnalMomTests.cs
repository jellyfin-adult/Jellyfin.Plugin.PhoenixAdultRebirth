using Pronium.Helpers;
using Pronium.Helpers.Utils;
using Pronium.Sites;

namespace Pronium.UnitTests;

[TestFixture]
public class SiteAnalMomTests
{
    [SetUp]
    public void Setup()
    {
        Database.LoadAll();
    }

    private readonly NetworkMylf _site = new();
    private readonly string _testSceneUrl = "https://mylf.com/movies/a-hole-lot-of-help-from-my-tutor";

    [Test]
    [TestCase(TestName = "{c}.{m}")]
    public async Task SearchIsWorking()
    {
        var result = await _site.Search(new[] { 23, 26 }, "A Hole Lot of Help From My Tutor", null, new CancellationToken());
        Assert.That(result.Count, Is.GreaterThan(10));
        var id = result[0].ProviderIds.Values.FirstOrDefault();
        Assert.That(id, Is.Not.Empty);
        Assert.That(Helper.Decode(id?.Split('#')[0]), Is.EqualTo(_testSceneUrl));
        Assert.That(result.First().ImageUrl, Does.Contain("christy_love2"));
    }

    [Test]
    [TestCase(TestName = "{c}.{m}")]
    public async Task UpdateIsWorking()
    {
        var result = await _site.Update(new[] { 23, 26 }, new[] { Helper.Encode(_testSceneUrl), "2024-02-17" }, new CancellationToken());
        Assert.That(result.Item.Name, Is.EqualTo("A Hole Lot of Help From My Tutor"));
        Assert.That(result.Item.OriginalTitle, Is.EqualTo("analmom - 2024-02-17 - A Hole Lot of Help From My Tutor"));
        Assert.That(result.Item.Overview, Does.StartWith("Max and Nade aren’t the sharpest tools"));
        Assert.That(result.Item.Studios.Length, Is.EqualTo(2));
        Assert.That(result.Item.Genres.Length, Is.EqualTo(70));
        Assert.That(result.Item.Genres, Does.Contain("Casual Wear"));
        Assert.That(result.People.Count, Is.EqualTo(1));
        Assert.That(result.People.Select(t => t.Name), Does.Contain("Christy Love"));
#pragma warning disable NUnit2021 // Incompatible types for EqualTo constraint
        Assert.That(result.Item.PremiereDate, Is.EqualTo(new DateTime(2024, 02, 17)).Within(24).Hours);
#pragma warning restore NUnit2021 // Incompatible types for EqualTo constraint
    }

    [Test]
    [TestCase(TestName = "{c}.{m}")]
    public async Task GetImagesIsWorking()
    {
        var result = (await _site.GetImages(
            new[] { 23, 26 },
            new[] { Helper.Encode(_testSceneUrl), "2024-02-17" },
            null,
            new CancellationToken())).ToList();

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.First().Url, Does.Contain("christy_love2"));
    }
}
