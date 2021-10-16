using Executorlibs.Shared;
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
        public static async Task<DateTime?> GetLiveTimeAsync(HttpClient client, int roomId, CancellationToken token = default)
        {
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, $"https://api.live.bilibili.com/room/v1/Room/room_init?id={roomId}");
            req.Headers.Add("Origin", "https://live.bilibili.com");
            req.Headers.Referrer = new Uri($"https://live.bilibili.com/");
            using JsonDocument j = await client.SendAsync(req, token).GetJsonAsync(token);
            JsonElement root = j.RootElement;
            if (root.GetProperty("code").GetInt32() == 0)
            {
                JsonElement data = root.GetProperty("data");
                return data.GetProperty("live_status").GetInt32() == 1 ? Utils.UnixTime2DateTime(data.GetProperty("live_time").GetInt32()) : null;
            }
            throw new UnknownResponseException(in root);
        }
    }
}
