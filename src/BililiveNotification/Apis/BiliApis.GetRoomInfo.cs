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
        public static async Task<RoomInfo> GetRoomInfoAsync(HttpClient client, int roomId, CancellationToken token = default)
        {
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, $"https://api.live.bilibili.com/room/v1/Room/get_info?room_id={roomId}&from=room");
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
            long userId = data.GetProperty("uid").GetInt64();
            UserInfo userInfo = await GetUserInfoAsync(client, userId, token);
            string? cover = data.GetProperty("user_cover").GetString();
            if (cover == "")
            {
                cover = null;
            }
            return new RoomInfo(userInfo.UserName, userId, userInfo.FaceUrl, cover);
        }
    }
}
