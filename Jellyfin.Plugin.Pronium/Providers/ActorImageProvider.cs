#if __EMBY__
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Configuration;
#else
using System.Net.Http;
#endif
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Newtonsoft.Json.Linq;
using Pronium.Configuration;
using Pronium.Helpers;
using Pronium.Helpers.Utils;

namespace Pronium.Providers
{
    public class ActorImageProvider : IRemoteImageProvider
    {
        private readonly string pornDbApiUrl = "https://api.theporndb.net/performers?q=";

        public string Name => Plugin.Instance.Name;

        public bool Supports(BaseItem item)
        {
            return item is Person;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new List<ImageType> { ImageType.Primary };
        }

#if __EMBY__
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(
            BaseItem item,
            LibraryOptions libraryOptions,
            CancellationToken cancellationToken)
#else
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
#endif
        {
            var images = new List<RemoteImageInfo>();

            if (item == null)
            {
                return images;
            }

            images = await this.GetActorPhotos(item.Name, cancellationToken).ConfigureAwait(false);

            if (item.ProviderIds.TryGetValue(this.Name, out var externalId))
            {
                var actorId = externalId.Split('#');
                if (actorId.Length > 2)
                {
                    var siteNum = new int[2]
                    {
                        int.Parse(actorId[0], CultureInfo.InvariantCulture), int.Parse(actorId[1], CultureInfo.InvariantCulture),
                    };
                    var sceneId = item.ProviderIds;

                    if (sceneId.ContainsKey(this.Name))
                    {
                        var provider = Helper.GetProviderBySiteId(siteNum[0]);
                        if (provider != null)
                        {
                            IEnumerable<RemoteImageInfo> remoteImageInfos = new List<RemoteImageInfo>();
                            try
                            {
                                remoteImageInfos = await provider.GetImages(siteNum, actorId.Skip(2).ToArray(), item, cancellationToken)
                                    .ConfigureAwait(false);
                            }
                            catch (Exception e)
                            {
                                Logger.Error($"GetImages error: \"{e}\"");

                                await Analytics.Send(
                                    new AnalyticsExeption { Request = string.Join("#", actorId.Skip(2)), SiteNum = siteNum, Exception = e },
                                    cancellationToken).ConfigureAwait(false);
                            }
                            finally
                            {
                                images.AddRange(remoteImageInfos);
                            }
                        }
                    }
                }
            }

            images = await ImageHelper.GetImagesSizeAndValidate(images, cancellationToken).ConfigureAwait(false);

            if (images.Any())
            {
                foreach (var img in images)
                {
                    if (string.IsNullOrEmpty(img.ProviderName))
                    {
                        img.ProviderName = this.Name;
                    }
                }

                images = images.OrderByDescending(o => o.Height).ToList();
            }

            return images;
        }

#if __EMBY__
        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
#else
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
#endif
        {
            return Helper.GetImageResponse(url, cancellationToken);
        }

        public async Task<List<RemoteImageInfo>> GetActorPhotos(string name, CancellationToken cancellationToken)
        {
            var imageList = new List<RemoteImageInfo>();

            if (string.IsNullOrEmpty(name))
            {
                return imageList;
            }

            Logger.Info($"Searching actor images for \"{name}\"");

            var searchName = Plugin.Instance?.Configuration.JAVActorNamingStyle == JAVActorNamingStyle.JapaneseStyle
                ? string.Join(" ", name.Split())
                : name;

            try
            {
                var result = await this.GetDataFromApi(this.pornDbApiUrl + searchName, cancellationToken);
                var data = result["data"];

                if (data.Count() == 0)
                {
                    throw new Exception($"No results found for \"{searchName}\"");
                }

                var posters = data[0]["posters"];

                foreach (var poster in posters)
                {
                    imageList.Add(new RemoteImageInfo
                    {
                        ProviderName = "PornDB",
                        ThumbnailUrl = poster["url"].ToString(),
                    });
                }
            }
            catch (Exception e)
            {
                Logger.Error($"GetActorPhotos error: \"{e}\"");

                await Analytics.Send(new AnalyticsExeption { Request = name, Exception = e }, cancellationToken).ConfigureAwait(false);
            }

            return imageList;
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
