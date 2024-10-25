using Pronium.Helpers.Utils;
using Pronium.Sites;

namespace Pronium.UnitTests;

[TestFixture]
public class SiteGloryholeTests
{
    [SetUp]
    public void Setup()
    {
        Database.LoadAll();
    }

    private readonly NetworkGammaEnt _site = new();

    [Test]
    [TestCase(TestName = "{c}.{m}")]
    public async Task SearchIsWorking()
    {
        var result = await _site.Search(new[] { 19, 3 }, "Alena Croft", DateTime.Parse("2015-09-28"), new CancellationToken());
        Assert.That(result.Count, Is.EqualTo(2));
        var id = result[0].ProviderIds.Values.FirstOrDefault();
        Assert.That(id, Is.Not.Empty);
        Assert.That(id?.Split('#')[0], Is.EqualTo("221961"));
    }

    [Test]
    [TestCase(TestName = "{c}.{m}")]
    public async Task UpdateIsWorking()
    {
        var result = await _site.Update(new[] { 19, 3 }, new[] { "221961", "scenes", "2015-09-28" }, new CancellationToken());
        Assert.That(result.Item.Name, Is.EqualTo("Alena Croft"));
        Assert.That(result.Item.OriginalTitle, Is.EqualTo("gloryhole - 2015-09-28 - Alena Croft"));
        Assert.That(result.Item.Overview, Is.Not.Empty);
        Assert.That(result.Item.Studios.Length, Is.EqualTo(2));
        Assert.That(result.Item.Genres.Length, Is.EqualTo(7));
        Assert.That(result.People.Count, Is.EqualTo(1));
    }

    [Test]
    [TestCase(TestName = "{c}.{m}")]
    public async Task GetImagesIsWorking()
    {
        var result = (await _site.GetImages(new[] { 19, 3 }, new[] { "221961", "scenes", "2015-09-28" }, null, new CancellationToken()))
            .ToList();

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(
            result.First().Url,
            Is.EqualTo("https://images-fame.gammacdn.com/movies/104458/104458_01/previews/2/685/top_1_resized/104458_01_01.jpg"));
    }
}
