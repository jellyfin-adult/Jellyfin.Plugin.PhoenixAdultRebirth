using Pronium.Helpers.Utils;
using Pronium.Providers;

namespace Pronium.UnitTests;

[TestFixture]
public class ActorImageProviderTests
{
    [SetUp]
    public void Setup()
    {
        Database.LoadAll();
    }

    private ActorImageProvider _provider = new();

    [Test, Explicit]
    [TestCase(TestName = "{c}.{m}")]
    public async Task GetActorPhotosIsWorking()
    {
        var result = await this._provider.GetActorPhotos("Scarlet Skies", new CancellationToken());

        Assert.That(result.Count, Is.GreaterThanOrEqualTo(5));
    }
}
