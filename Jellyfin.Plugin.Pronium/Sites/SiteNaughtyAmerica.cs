using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Pronium.Helpers;
using Pronium.Helpers.Utils;

namespace Pronium.Sites
{
    public class SiteNaughtyAmerica : IProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(
            int[] siteNum,
            string searchTitle,
            DateTime? searchDate,
            CancellationToken cancellationToken)
        {
            var results = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
            {
                return results;
            }

            var url = Helper.GetSearchSearchURL(siteNum) + searchTitle;
            Logger.Info($"SiteNaughtyAmerica.Search url: {url}");
            var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var searchResults = data.SelectNodesSafe("//div[@class='scene-grid-item']");
            Logger.Info($"SiteNaughtyAmerica.Search searchResults: {searchResults.Count}");

            foreach (var searchResult in searchResults)
            {
                var sceneUrl = new Uri(searchResult.SelectSingleNode("./a[@class='contain-img']").Attributes["href"].Value);
                var uniqueId = Helper.Encode(sceneUrl.AbsoluteUri);
                var scenePoster = searchResult.SelectSingleText(".//img[contains(@class, 'main-scene-img')]/@data-srcset");
                var res = new RemoteSearchResult { Name = string.Empty, ImageUrl = $"https://{scenePoster}" };

                Logger.Info($"SiteNaughtyAmerica.Search sceneURL: {sceneUrl}, ID: {uniqueId}, scenePoster: {scenePoster}");

                res.ProviderIds.Add(Plugin.Instance?.Name ?? "Pronium", uniqueId);

                results.Add(res);
            }

            return results;
        }

        /// <inheritdoc />
        public async Task<MetadataResult<BaseItem>> Update(int[] siteNum, string[] sceneId, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<BaseItem> { Item = new Movie(), People = new List<PersonInfo>() };

            if (sceneId == null)
            {
                return result;
            }

            var sceneUrl = Helper.Decode(sceneId[0]);

            Logger.Info($"SiteNaughtyAmerica.Update sceneID: {sceneId[0]}, sceneURL: {sceneUrl}");

            var sceneData = await HTML.ElementFromURL(sceneUrl, cancellationToken).ConfigureAwait(false);
            var name = HttpUtility.HtmlDecode(sceneData.SelectSingleText("//h1"));
            var overview = sceneData.SelectSingleText("//div[contains(@class, 'synopsis')]").Replace("Synopsis", string.Empty);

            Logger.Info($"SiteNaughtyAmerica.Update name: {name}");
            result.Item.ExternalId = sceneUrl;

            result.Item.Name = name;
            result.Item.Overview = overview;
            result.Item.AddStudio("Naughty America");
            var studio = sceneData.SelectSingleText("//a[contains(@class, 'site-title')]");
            if (!string.IsNullOrWhiteSpace(studio))
            {
                result.Item.AddStudio(studio);
            }

            var date = sceneData.SelectSingleText("//span[contains(@class, 'entry-date')]");
            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(
                    date,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var sceneDateObj))
            {
                result.Item.PremiereDate = sceneDateObj;
                result.Item.OriginalTitle =
                    $"{Helper.GetSitePrefix(siteNum)} - {result.Item.PremiereDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} - {result.Item.Name}";
            }

            var genreNode = sceneData.SelectNodesSafe("//div[contains(@class, 'categories')]/a");
            foreach (var genreLink in genreNode)
            {
                var genreName = genreLink.InnerText;

                result.Item.AddGenre(genreName);
            }

            var actorsNode = sceneData.SelectNodesSafe("//div[@class='performer-list']//a");
            foreach (var actorLink in actorsNode)
            {
                var actorUrl = actorLink.Attributes["href"].Value;
                var actorData = await HTML.ElementFromURL(actorUrl, cancellationToken).ConfigureAwait(false);
                var actor = new PersonInfo { Name = actorLink.InnerText };
                var actorImage = actorData.SelectSingleNode("//img[contains(@class, 'performer-pic')]");
                if (actorImage != null)
                {
                    var actorImageUrl = actorImage.Attributes["data-src"].Value ?? string.Empty;
                    actor.ImageUrl = $"https:{actorImageUrl}";
                }

                result.People.Add(actor);
            }

            Logger.Info($"SiteNaughtyAmerica.Update genres: {result.Item.Genres.Length}, actors: {result.People.Count}");

            return result;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(
            int[] siteNum,
            string[] sceneId,
            BaseItem item,
            CancellationToken cancellationToken)
        {
            var result = new List<RemoteImageInfo>();

            if (sceneId == null)
            {
                return result;
            }

            var sceneUrl = Helper.Decode(sceneId[0]);
            var sceneData = await HTML.ElementFromURL(sceneUrl, cancellationToken).ConfigureAwait(false);
            var primaryImage = sceneData.SelectSingleText(".//img[contains(@class, 'start-card')]/@data-srcset");

            if (!string.IsNullOrEmpty(primaryImage))
            {
                result.Add(new RemoteImageInfo { Url = $"https:{primaryImage}", Type = ImageType.Primary });
                result.Add(new RemoteImageInfo { Url = $"https:{primaryImage}", Type = ImageType.Backdrop });
            }

            var additionalImages = sceneData.SelectNodesSafe("//a[contains(@class, 'fancybox')]");

            foreach (var additionalImage in additionalImages)
            {
                var img = HttpUtility.HtmlDecode(additionalImage.Attributes["href"].Value);

                result.Add(new RemoteImageInfo { Url = img, Type = ImageType.Primary });
                result.Add(new RemoteImageInfo { Url = img, Type = ImageType.Backdrop });
            }

            return result;
        }
    }
}
