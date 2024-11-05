using Pronium.Helpers;
using Pronium.Helpers.Utils;
using Pronium.Sites;

namespace Pronium.UnitTests;

[TestFixture]
public class SiteNaughtyAmericaTests
{
    [SetUp]
    public void Setup()
    {
        Database.LoadAll();
    }

    private readonly SiteNaughtyAmerica _site = new();
    private readonly string _testSceneUrl = "https://www.naughtyamerica.com/scene/mgbf-alisondanny-31736";

    [Test, Retry(3)]
    [TestCase(TestName = "{c}.{m}")]
    public async Task SearchIsWorking()
    {
        var result = await _site.Search(
            new[] { 10, 0 },
            "Busty brunette Alison Rey takes friend's boyfriend's dick for a ride",
            null,
            new CancellationToken());
        Assert.That(result.Count, Is.GreaterThan(0));
        var id = result.FirstOrDefault()?.ProviderIds.Values.FirstOrDefault();
        Assert.That(id, Is.Not.Empty);
        var url = !string.IsNullOrWhiteSpace(id) ? Helper.Decode(id?.Split('#')[0]) : string.Empty;
        Assert.That(url, Is.EqualTo(_testSceneUrl));
    }

    [Test, Retry(3)]
    [TestCase(TestName = "{c}.{m}")]
    public async Task UpdateIsWorking()
    {
        var result = await _site.Update(new[] { 10, 0 }, new[] { Helper.Encode(_testSceneUrl), "2023-03-02" }, new CancellationToken());
        Assert.That(result.Item.Name, Is.EqualTo("Busty brunette Alison Rey takes friend's boyfriend's dick for a ride"));
        Assert.That(result.Item.OriginalTitle, Is.EqualTo("nam - 2023-03-02 - Busty brunette Alison Rey takes friend's boyfriend's dick for a ride"));
        Assert.That(result.Item.Overview, Does.StartWith("Alison Rey has her friend"));
        Assert.That(result.Item.Studios.Length, Is.EqualTo(2));
        Assert.That(result.Item.Genres.Length, Is.EqualTo(15));
        Assert.That(result.People.Count, Is.EqualTo(2));
    }

    [Test, Retry(3)]
    [TestCase(TestName = "{c}.{m}")]
    public async Task GetImagesIsWorking()
    {
        var result = (await _site.GetImages(
            new[] { 51, 0 },
            new[] { Helper.Encode(_testSceneUrl), "2023-03-02" },
            null,
            new CancellationToken())).ToList();

        Assert.That(result, Has.Count.EqualTo(18));
        Assert.That(
            result.FirstOrDefault()?.Url,
            Is.EqualTo("https://images1.naughtycdn.com/cms/nacmscontent/v1/scenes/mgbf/alisondanny/scene/horizontal/1279x852c.webp"));
    }
}
