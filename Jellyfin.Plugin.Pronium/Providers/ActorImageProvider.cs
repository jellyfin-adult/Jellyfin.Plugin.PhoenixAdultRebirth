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
using Pronium.Configuration;
using Pronium.Helpers;
using Pronium.Helpers.Utils;

namespace Pronium.Providers
{
    public class ActorImageProvider : IRemoteImageProvider
    {
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
            var tasks = new Dictionary<string, Task<IEnumerable<string>>>();
            var imageList = new List<RemoteImageInfo>();

            if (string.IsNullOrEmpty(name))
            {
                return imageList;
            }

            Logger.Info($"Searching actor images for \"{name}\"");

            tasks.Add("AdultDVDEmpire", GetFromAdultDvdEmpire(name, cancellationToken));
            tasks.Add("Boobpedia", GetFromBoobpedia(name, cancellationToken));
            tasks.Add("Babepedia", GetFromBabepedia(name, cancellationToken));
            tasks.Add("IAFD", GetFromIafd(name, cancellationToken));

            var splitedName = name.Split();
            switch (Plugin.Instance?.Configuration.JAVActorNamingStyle)
            {
                case JAVActorNamingStyle.JapaneseStyle:
                    if (splitedName.Length > 1)
                    {
                        var nameReversed = string.Join(" ", splitedName.Reverse());

                        tasks.Add("Boobpedia JAV", GetFromBoobpedia(nameReversed, cancellationToken));
                        tasks.Add("Babepedia JAV", GetFromBabepedia(nameReversed, cancellationToken));
                        tasks.Add("IAFD JAV", GetFromIafd(nameReversed, cancellationToken));
                    }

                    break;
            }

            try
            {
                await Task.WhenAll(tasks.Values).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error($"GetActorPhotos error: \"{e}\"");

                await Analytics.Send(new AnalyticsExeption { Request = name, Exception = e }, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                foreach (var image in tasks)
                {
                    var results = image.Value.Result;

                    foreach (var result in results)
                    {
                        imageList.Add(new RemoteImageInfo { ProviderName = image.Key, Url = result });
                    }
                }
            }

            return imageList;
        }

        private static async Task<IEnumerable<string>> GetFromAdultDvdEmpire(string name, CancellationToken cancellationToken)
        {
            var images = new List<string>();

            if (string.IsNullOrEmpty(name))
            {
                return images;
            }

            string encodedName = HttpUtility.UrlEncode(name), url = $"https://www.adultdvdempire.com/performer/search?q={encodedName}";

            var actorData = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var actorPageUrl = actorData.SelectSingleText("//div[@id='performerlist']/div//a/@href");
            if (!string.IsNullOrEmpty(actorPageUrl))
            {
                if (!actorPageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    actorPageUrl = "https://www.adultdvdempire.com" + actorPageUrl;
                }

                var actorPage = await HTML.ElementFromURL(actorPageUrl, cancellationToken).ConfigureAwait(false);

                var img = actorPage.SelectSingleText("//div[contains(@class, 'performer-image-container')]/a/@href");
                if (!string.IsNullOrEmpty(img))
                {
                    images.Add(img);
                }
            }

            return images;
        }

        private static async Task<IEnumerable<string>> GetFromBoobpedia(string name, CancellationToken cancellationToken)
        {
            var images = new List<string>();

            if (string.IsNullOrEmpty(name))
            {
                return images;
            }

            string encodedName = HttpUtility.UrlEncode(name), url = $"https://www.boobpedia.com/wiki/index.php?search={encodedName}";

            var actorData = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var img = actorData.SelectSingleText("//table[@class='infobox']//a[@class='image']//img/@src");
            if (!string.IsNullOrEmpty(img) && !img.Contains("NoImage", StringComparison.OrdinalIgnoreCase))
            {
                if (!img.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    img = "https://www.boobpedia.com" + img;
                }

                images.Add(img);
            }

            return images;
        }

        private static async Task<IEnumerable<string>> GetFromBabepedia(string name, CancellationToken cancellationToken)
        {
            var images = new List<string>();

            if (string.IsNullOrEmpty(name))
            {
                return images;
            }

            string encodedName = name.Replace(" ", "_", StringComparison.OrdinalIgnoreCase),
                url = $"https://www.babepedia.com/babe/{encodedName}";

            var actorData = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var img = actorData.SelectSingleText("//div[@id='profimg']/a/@href");
            if (!string.IsNullOrEmpty(img) && !img.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
            {
                if (!img.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    img = "https://www.babepedia.com" + img;
                }

                images.Add(img);
            }

            var profSelectedImages = actorData.SelectNodesSafe("//div[@id='profselect']//a");
            foreach (var image in profSelectedImages)
            {
                var imageUrl = image.Attributes["href"].Value;
                if (!imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    imageUrl = "https://www.babepedia.com" + imageUrl;
                }

                images.Add(imageUrl);
            }

            return images;
        }

        private static async Task<IEnumerable<string>> GetFromIafd(string name, CancellationToken cancellationToken)
        {
            var image = new List<string>();

            if (string.IsNullOrEmpty(name))
            {
                return image;
            }

            string encodedName = HttpUtility.UrlEncode(name),
                url = $"https://www.iafd.com/results.asp?searchtype=comprehensive&searchstring={encodedName}";

            var actorData = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var actorPageURL = actorData.SelectSingleText("//table[@id='tblFem']//tbody//a/@href");
            if (!string.IsNullOrEmpty(actorPageURL))
            {
                if (!actorPageURL.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    actorPageURL = "https://www.iafd.com" + actorPageURL;
                }

                var actorPage = await HTML.ElementFromURL(actorPageURL, cancellationToken).ConfigureAwait(false);

                var actorImage = actorPage.SelectSingleText("//div[@id='headshot']//img/@src");
                if (!actorImage.Contains("nophoto", StringComparison.OrdinalIgnoreCase))
                {
                    image.Add(actorImage);
                }
            }

            return image;
        }
    }
}
