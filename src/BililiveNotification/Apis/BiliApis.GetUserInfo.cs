using BililiveNotification.Models;
using Executorlibs.Shared.Exceptions;
using Executorlibs.Shared.Extensions;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BililiveNotification.Apis
{
    public static partial class BiliApis
    {
        public static async Task<UserInfo> GetUserInfoAsync(HttpClient client, int userId, CancellationToken token = default)
        {
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, $"https://api.bilibili.com/x/space/acc/info?mid={userId}&jsonp=jsonp");
            req.Headers.Accept.ParseAdd("*/*");
            req.Headers.Add("Origin", "https://live.bilibili.com");
            req.Headers.Referrer = new Uri("https://live.bilibili.com/");
            using JsonDocument j = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, token).GetJsonAsync(token);
            JsonElement root = j.RootElement;
            if (root.GetProperty("code").GetInt32() != 0)
            {
                throw new UnknownResponseException(in root);
            }
            JsonElement data = root.GetProperty("data");
            return new UserInfo(data.GetProperty("name").GetString()!, userId, data.GetProperty("face").GetString()!);
        }
    }
}
