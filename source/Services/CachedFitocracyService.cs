using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace FitBot.Services
{
    public class CachedFitocracyService : FitocracyService
    {
        private readonly string _cacheDir = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "FitocracyServiceCache");

        public CachedFitocracyService(IScrapingService scraper)
            : base(scraper)
        {
            Directory.CreateDirectory(_cacheDir);
        }

        protected override async Task<Stream> Get(string endpoint, object queryArgs = null)
        {
            var cacheFileName = Path.Combine(_cacheDir, string.Concat((endpoint + "_" + queryArgs).Replace(" ", "").Split(Path.GetInvalidFileNameChars())));
            if (endpoint == "accounts/login")
            {
                if (File.Exists(cacheFileName))
                {
                    using (var stream = File.OpenRead(cacheFileName))
                    {
                        Cookies = (CookieContainer) new BinaryFormatter().Deserialize(stream);
                    }
                    return Stream.Null;
                }
                return await base.Get(endpoint, queryArgs);
            }
            if (!File.Exists(cacheFileName))
            {
                using (var source = await base.Get(endpoint, queryArgs))
                using (var destination = File.OpenWrite(cacheFileName))
                {
                    source.CopyTo(destination);
                }
            }
            return File.OpenRead(Path.Combine(_cacheDir, cacheFileName));
        }

        protected override async Task Post(string endpoint, object formData = null)
        {
            if (endpoint == "accounts/login")
            {
                var cacheFileName = Path.Combine(_cacheDir, "accountslogin_");
                if (!File.Exists(cacheFileName))
                {
                    await base.Post(endpoint, formData);
                    using (var stream = File.OpenWrite(cacheFileName))
                    {
                        new BinaryFormatter().Serialize(stream, Cookies);
                    }
                }
            }
            else
            {
                await base.Post(endpoint, formData);
            }
        }
    }
}