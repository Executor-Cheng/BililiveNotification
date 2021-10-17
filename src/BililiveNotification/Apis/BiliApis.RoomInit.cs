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
        /// <summary>
        /// 检查接口返回是否正常
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="TimeoutException"/>
        /// <exception cref="UnknownResponseException"/>
        private static void CheckRoomInitResp(in JsonElement root)
        {
            int code = root.GetProperty("code").GetInt32();
            switch (code)
            {
                case 0:
                    {
                        return;
                    }
                case 1024:
                    {
                        throw new TimeoutException("服务器超时");
                    }
                case 60004:
                    {
                        throw new InvalidOperationException("房间号不存在");
                    }
                default:
                    {
                        throw new UnknownResponseException(in root);
                    }
            }
        }

        private static void ParseRoomInitResp(in JsonElement root, out int shortRoomId, out int originRoomId)
        {
            CheckRoomInitResp(in root);
            JsonElement data = root.GetProperty("data");
            shortRoomId = data.GetProperty("short_id").GetInt32();
            originRoomId = data.GetProperty("room_id").GetInt32();
        }

        private static void ProcessCache(int shortRoomId, int originRoomId)
        {
            if (shortRoomId > 0)
            {
                if (!ShortRoomCache.ContainsKey(shortRoomId))
                {
                    ShortRoomCache[shortRoomId] = shortRoomId;
                }
                if (!ShortRoomCache.ContainsKey(originRoomId))
                {
                    ShortRoomCache[originRoomId] = shortRoomId;
                }
                if (!OriginRoomCache.ContainsKey(shortRoomId))
                {
                    OriginRoomCache[shortRoomId] = originRoomId;
                }
            }
            else if (!ShortRoomCache.ContainsKey(originRoomId))
            {
                ShortRoomCache[originRoomId] = originRoomId;
            }
            if (!OriginRoomCache.ContainsKey(originRoomId))
            {
                OriginRoomCache[originRoomId] = originRoomId;
            }
        }

        /// <summary>
        /// 使用给定的房间号异步请求 /room/v1/Room/room_init 接口
        /// </summary>
        /// <param name="roomId">房间号</param>
        private static Task<JsonDocument> RequestRoomInitAsync(HttpClient client, int roomId, CancellationToken token = default)
        {
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, $"https://api.live.bilibili.com/room/v1/Room/room_init?id={roomId}");
            req.Headers.Add("Origin", "https://live.bilibili.com");
            req.Headers.Referrer = new Uri($"https://live.bilibili.com/{roomId}");
            return client.SendAsync(req, token).GetJsonAsync(token);
        }
    }
}
