using Pronium.Helpers;
using Pronium.Helpers.Utils;
using Pronium.Sites;

namespace Pronium.UnitTests;

[TestFixture]
public class SiteData18Tests
{
    [SetUp]
    public void Setup()
    {
        Database.LoadAll();
    }

    private readonly SiteData18 _site = new();
    private readonly string _testSceneUrl = "https://www.data18.com/scenes/144118";

    [Test]
    [TestCase(TestName = "{c}.{m}")]
    public async Task SearchIsWorking()
    {
        var result = await _site.Search(new[] { 51, 0 }, "Elite Dyke Society", null, new CancellationToken());
        Assert.That(result.Count, Is.GreaterThan(0));
        var id = result[0].ProviderIds.Values.FirstOrDefault();
        Assert.That(id, Is.Not.Empty);
        Assert.That(Helper.Decode(id?.Split('#')[0]), Is.EqualTo(_testSceneUrl));
    }

    [Test]
    [TestCase(TestName = "{c}.{m}")]
    public async Task UpdateIsWorking()
    {
        var result = await _site.Update(new[] { 51, 0 }, new[] { Helper.Encode(_testSceneUrl), "2010-09-14" }, new CancellationToken());
        Assert.That(result.Item.Name, Is.EqualTo("Elite Dyke Society"));
        Assert.That(result.Item.OriginalTitle, Is.EqualTo("data18 - 2010-09-14 - Elite Dyke Society"));
        Assert.That(result.Item.Overview, Is.Not.Empty);
        Assert.That(result.Item.Studios.Length, Is.EqualTo(3));
        Assert.That(result.Item.Genres.Length, Is.EqualTo(6));
        Assert.That(result.People.Count, Is.EqualTo(2));
        Assert.That(result.People[0].ImageUrl, Does.Contain("8b1888d6-4d61-4a60-9bec-d9bf3d80c785"));
        Assert.That(result.People[1].ImageUrl, Does.Contain("7e059593-78ff-40d3-840e-1f5190791c67"));
    }

    [Test]
    [TestCase(TestName = "{c}.{m}")]
    public async Task UpdateIsWorkingBoyGirl()
    {
        var result = await _site.Update(new[] { 51, 0 }, new[] { Helper.Encode("https://www.data18.com/scenes/144862"), "2010-10-18" }, new CancellationToken());
        Assert.That(result.Item.Name, Is.EqualTo("Block Out Your Husband ... Now Fuck Me"));
        Assert.That(result.Item.OriginalTitle, Is.EqualTo("data18 - 2010-10-18 - Block Out Your Husband ... Now Fuck Me"));
        Assert.That(result.Item.Overview, Is.Not.Empty);
        Assert.That(result.Item.Studios.Length, Is.EqualTo(3));
        Assert.That(result.Item.Genres.Length, Is.EqualTo(4));
        Assert.That(result.People.Count, Is.EqualTo(2));
        Assert.That(result.People[0].ImageUrl, Does.Contain("Sophie-Dee"));
        Assert.That(result.People[1].ImageUrl, Does.Contain("Scott-Nails"));
    }

    [Test]
    [TestCase(TestName = "{c}.{m}")]
    public async Task GetImagesIsWorking()
    {
        var result = (await _site.GetImages(
            new[] { 51, 0 },
            new[] { Helper.Encode(_testSceneUrl), "2010-09-14" },
            null,
            new CancellationToken())).ToList();

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(
            result.First().Url,
            Is.EqualTo("https://cdn.dt18.com/media/t/1/scenes/1/2/44118-kelly-divine-sara-jaymes-hot-and-mean.jpg"));
    }
}
