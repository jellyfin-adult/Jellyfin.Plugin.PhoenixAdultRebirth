using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Newtonsoft.Json.Linq;
using Pronium.Helpers;
using Pronium.Helpers.Utils;

namespace Pronium.Sites
{
    public class PornDbApi : IProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(
            int[] siteNum,
            string searchTitle,
            DateTime? searchDate,
            CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
            {
                return result;
            }

            if (searchDate.HasValue)
            {
                searchTitle += " " + searchDate.Value.ToString("yyyy-MM-dd");
            }

            var url = Helper.GetSearchSearchURL(siteNum) + $"?parse={searchTitle}";
            Logger.Info($"PornDbApi.Search url: {url}");
            var searchResults = await this.GetDataFromApi(url, cancellationToken).ConfigureAwait(false);
            Logger.Info($"PornDbApi.Search searchResults: {searchResults.Count}");
            if (searchResults == null)
            {
                return result;
            }

            foreach (var (idx, searchResult) in searchResults["data"].WithIndex())
            {
                var curID = $"{siteNum[0]}#{siteNum[1]}#{(string)searchResult["_id"]}";
                var sceneName = (string)searchResult["title"];
                var sceneDate = (string)searchResult["date"];
                var scenePoster = (string)searchResult["poster"];

                var res = new RemoteSearchResult
                {
                    ProviderIds = { { Plugin.Instance?.Name ?? "Pronium", curID } }, Name = sceneName, ImageUrl = scenePoster, IndexNumberEnd = idx,
                };

                if (DateTime.TryParseExact(
                        sceneDate,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var sceneDateObj))
                {
                    res.PremiereDate = sceneDateObj;
                }

                result.Add(res);
            }

            return result;
        }

        public async Task<MetadataResult<BaseItem>> Update(int[] siteNum, string[] sceneID, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<BaseItem> { Item = new Movie(), People = new List<PersonInfo>() };

            if (sceneID == null)
            {
                return result;
            }

            var url = Helper.GetSearchSearchURL(siteNum) + $"/{sceneID[0]}";
            var sceneData = await this.GetDataFromApi(url, cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
            {
                return result;
            }

            sceneData = (JObject)sceneData["data"];
            var sceneURL = Helper.GetSearchBaseURL(siteNum) + $"/{sceneID[0]}";

            result.Item.ExternalId = sceneURL;

            result.Item.Name = (string)sceneData["title"];
            result.Item.Overview = (string)sceneData["description"];
            if (sceneData.ContainsKey("site") && sceneData["site"].Type == JTokenType.Object)
            {
                result.Item.AddStudio((string)sceneData["site"]["name"]);

                int? site_id = (int)sceneData["site"]["id"], network_id = (int?)sceneData["site"]["network_id"];

                if (network_id.HasValue && !site_id.Equals(network_id))
                {
                    var sitesApiNum = Helper.GetSiteFromTitle("PornDBSites").siteNum;
                    url = Helper.GetSearchSearchURL(sitesApiNum) + $"/{network_id}";

                    var siteData = await this.GetDataFromApi(url, cancellationToken).ConfigureAwait(false);
                    if (siteData != null)
                    {
                        result.Item.AddStudio((string)siteData["data"]["name"]);
                    }
                }
            }

            var sceneDate = (string)sceneData["date"];
            if (DateTime.TryParseExact(sceneDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
            {
                result.Item.PremiereDate = sceneDateObj;
                var siteName = Regex.Replace(result.Item.Studios.FirstOrDefault().ToLower(), "[^a-z]", string.Empty);
                var prefix = Helper.GetSitePrefixByName(siteName);
                var resultDate = result.Item.PremiereDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                result.Item.OriginalTitle = $"{prefix} - {resultDate} - {result.Item.Name}";
            }

            if (sceneData.ContainsKey("tags"))
            {
                foreach (var genreLink in sceneData["tags"])
                {
                    var genreName = (string)genreLink["name"];

                    result.Item.AddGenre(genreName);
                }
            }

            if (sceneData.ContainsKey("performers"))
            {
                foreach (var actorLink in sceneData["performers"])
                {
                    var imageUrl = actorLink["is_parent"].ToString() == "False" ? actorLink["parent"]["image"].ToString() : actorLink["image"].ToString();
                    var actor = new PersonInfo { Name = (string)actorLink["name"], ImageUrl = imageUrl };

                    result.People.Add(actor);
                }
            }

            return result;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(
            int[] siteNum,
            string[] sceneID,
            BaseItem item,
            CancellationToken cancellationToken)
        {
            var result = new List<RemoteImageInfo>();

            if (sceneID == null)
            {
                return result;
            }

            var url = Helper.GetSearchSearchURL(siteNum) + $"/{sceneID[0]}";
            var sceneData = await this.GetDataFromApi(url, cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
            {
                return result;
            }

            sceneData = (JObject)sceneData["data"];

            if (!((string)sceneData["poster"]).Contains("smart/filters"))
            {
                result.Add(new RemoteImageInfo { Url = (string)sceneData["poster"], Type = ImageType.Primary });
            }

            result.Add(new RemoteImageInfo { Url = (string)sceneData["background"]["full"], Type = ImageType.Primary });
            result.Add(new RemoteImageInfo { Url = (string)sceneData["background"]["full"], Type = ImageType.Backdrop });

            return result;
        }

        private async Task<JObject> GetDataFromApi(string url, CancellationToken cancellationToken)
        {
            JObject json = null;
            var headers = new Dictionary<string, string>();
            var token = Environment.GetEnvironmentVariable("API_TOKEN");

            if (!string.IsNullOrEmpty(token))
            {
                headers.Add("Authorization", $"Bearer {token}");
            }
            else if (!string.IsNullOrEmpty(Plugin.Instance.Configuration.PornDbApiToken))
            {
                headers.Add("Authorization", $"Bearer {Plugin.Instance.Configuration.PornDbApiToken}");
            }

            var http = await HTTP.Request(url, cancellationToken, headers).ConfigureAwait(false);
            if (http.IsOK)
            {
                json = JObject.Parse(http.Content);
            }

            return json;
        }
    }
}
