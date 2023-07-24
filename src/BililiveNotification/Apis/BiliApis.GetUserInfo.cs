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
        public static async Task<UserInfo> GetUserInfoAsync(HttpClient client, long userId, CancellationToken token = default)
        {
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, $"https://api.bilibili.com/x/web-interface/card?mid={userId}");
            req.Headers.Accept.ParseAdd("*/*");
            req.Headers.Add("Origin", "https://www.bilibili.com");
            req.Headers.Referrer = new Uri("https://www.bilibili.com/");
            using JsonDocument j = await client.SendAsync(req, token).GetJsonAsync(token);
            JsonElement root = j.RootElement;
            if (root.GetProperty("code").GetInt32() != 0)
            {
                throw new UnknownResponseException(in root);
            }
            JsonElement card = root.GetProperty("data").GetProperty("card");
            return new UserInfo(card.GetProperty("name").GetString()!, userId, card.GetProperty("face").GetString()!);
        }
    }
}
