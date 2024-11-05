using Pronium.Helpers;
using Pronium.Helpers.Utils;
using Pronium.Sites;
using MediaBrowser.Model.Providers;

namespace Pronium.UnitTests;

[TestFixture]
public class SiteFreeUseFantasyTests
{
    [SetUp]
    public void Setup()
    {
        Database.LoadAll();
    }

    private readonly NetworkMylf _site = new();
    private readonly string _testSceneUrl = "https://www.teamskeet.com/movies/freeused-freeloader";

    [Test, Retry(3)]
    [TestCase(TestName = "{c}.{m}")]
    public async Task SearchIsWorking()
    {
        var result = new List<RemoteSearchResult>();
        try {
            result = await _site.Search(new[] { 24, 58 }, "Freeused Freeloader", null, new CancellationToken());
        } catch (Exception e) {
            Console.WriteLine(e.Message);
        }

        Assert.That(result.Count, Is.GreaterThan(1));
        var id = result.FirstOrDefault()?.ProviderIds.Values.FirstOrDefault();
        Assert.That(id, Is.Not.Empty);
        Assert.That(id != null ? Helper.Decode(id.Split('#')[0]) : "", Is.EqualTo(_testSceneUrl));
        Assert.That(result.FirstOrDefault()?.ImageUrl ?? "", Does.Contain("breezy_bri_and_penelope_woods"));
    }

    [Test, Retry(3)]
    [TestCase(TestName = "{c}.{m}")]
    public async Task UpdateIsWorking()
    {
        var result = await _site.Update(new[] { 24, 58 }, new[] { Helper.Encode(_testSceneUrl), "2024-08-10" }, new CancellationToken());

        Assert.That(result.Item.Name, Is.EqualTo("Freeused Freeloader"));
        Assert.That(result.Item.OriginalTitle, Is.EqualTo("freeusefantasy - 2024-08-10 - Freeused Freeloader"));
        Assert.That(result.Item.Overview, Does.StartWith("Tyler is pissed at Penelope. She’s not contributing to rent"));
        Assert.That(result.Item.Studios.Length, Is.EqualTo(2));
        Assert.That(result.Item.Genres.Length, Is.EqualTo(86));
        Assert.That(result.Item.Genres, Does.Contain("Belly Button Piercings"));
        Assert.That(result.People.Count, Is.EqualTo(2));
        Assert.That(result.People.Select(t => t.Name), Does.Contain("Penelope Woods"));
        Assert.That(result.People.Select(t => t.Name), Does.Contain("Breezy Bri"));
#pragma warning disable NUnit2021 // Incompatible types for EqualTo constraint
        Assert.That(result.Item.PremiereDate, Is.EqualTo(new DateTime(2024, 08, 10)).Within(24).Hours);
#pragma warning restore NUnit2021 // Incompatible types for EqualTo constraint
    }

    [Test, Retry(3)]
    [TestCase(TestName = "{c}.{m}")]
    public async Task GetImagesIsWorking()
    {
        var result = (await _site.GetImages(
            new[] { 24, 58 },
            new[] { Helper.Encode(_testSceneUrl), "2024-08-10" },
            null,
            new CancellationToken())).ToList();

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.FirstOrDefault()?.Url ?? "", Does.Contain("breezy_bri_and_penelope_woods"));
    }
}
