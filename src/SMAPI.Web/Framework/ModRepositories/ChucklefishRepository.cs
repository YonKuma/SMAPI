using System;
using System.Net;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Pathoschild.Http.Client;
using StardewModdingAPI.Common.Models;

namespace StardewModdingAPI.Web.Framework.ModRepositories
{
    /// <summary>An HTTP client for fetching mod metadata from the Chucklefish mod site.</summary>
    internal class ChucklefishRepository : RepositoryBase
    {
        /*********
        ** Properties
        *********/
        /// <summary>The base URL for the Chucklefish mod site.</summary>
        private readonly string BaseUrl;

        /// <summary>The URL for a mod page excluding the base URL, where {0} is the mod ID.</summary>
        private readonly string ModPageUrlFormat;

        /// <summary>The underlying HTTP client.</summary>
        private readonly IClient Client;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="vendorKey">The unique key for this vendor.</param>
        /// <param name="userAgent">The user agent for the API client.</param>
        /// <param name="baseUrl">The base URL for the Chucklefish mod site.</param>
        /// <param name="modPageUrlFormat">The URL for a mod page excluding the <paramref name="baseUrl"/>, where {0} is the mod ID.</param>
        public ChucklefishRepository(string vendorKey, string userAgent, string baseUrl, string modPageUrlFormat)
            : base(vendorKey)
        {
            this.BaseUrl = baseUrl;
            this.ModPageUrlFormat = modPageUrlFormat;
            this.Client = new FluentClient(baseUrl).SetUserAgent(userAgent);
        }

        /// <summary>Get metadata about a mod in the repository.</summary>
        /// <param name="id">The mod ID in this repository.</param>
        public override async Task<ModInfoModel> GetModInfoAsync(string id)
        {
            // validate ID format
            if (!uint.TryParse(id, out uint _))
                return new ModInfoModel($"The value '{id}' isn't a valid Chucklefish mod ID, must be an integer ID.");

            // fetch info
            try
            {
                // fetch HTML
                string html;
                try
                {
                    html = await this.Client
                        .GetAsync(string.Format(this.ModPageUrlFormat, id))
                        .AsString();
                }
                catch (ApiException ex) when (ex.Status == HttpStatusCode.NotFound)
                {
                    return new ModInfoModel("Found no mod with this ID.");
                }

                // parse HTML
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // extract mod info
                string url = new UriBuilder(new Uri(this.BaseUrl)) { Path = string.Format(this.ModPageUrlFormat, id) }.Uri.ToString();
                string name = doc.DocumentNode.SelectSingleNode("//meta[@name='twitter:title']").Attributes["content"].Value;
                if (name.StartsWith("[SMAPI] "))
                    name = name.Substring("[SMAPI] ".Length);
                string version = doc.DocumentNode.SelectSingleNode("//h1/span").InnerText;

                // create model
                return new ModInfoModel(name, this.NormaliseVersion(version), url);
            }
            catch (Exception ex)
            {
                return new ModInfoModel(ex.ToString());
            }
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public override void Dispose()
        {
            this.Client.Dispose();
        }
    }
}
