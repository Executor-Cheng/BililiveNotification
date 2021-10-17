using Executorlibs.Shared.Extensions;
using System.Net.Http;

namespace BililiveNotification.Apis
{
    public static partial class BiliApis
    {
        public static HttpClient Client { get; } = new HttpClient();

        static BiliApis()
        {
            Client.DefaultRequestHeaders.SetUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36 Edg/87.0.664.66");
            Client.DefaultRequestHeaders.SetAccept("*/*");
            Client.DefaultRequestHeaders.SetAcceptLanguage("zh-CN,zh;q=0.9");
        }
    }
}
