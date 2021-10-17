using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BililiveNotification.Apis
{
    public static partial class BiliApis
    {
        /// <summary>
        /// 原房间号缓存字典
        /// </summary>
        public static IDictionary<int, int> OriginRoomCache { get; } = new ConcurrentDictionary<int, int>();

        /// <summary>
        /// 异步获取原房间号
        /// </summary>
        /// <param name="roomId">将要查询的房间号</param>
        /// <returns>原房间号</returns>
        public static ValueTask<int> GetRealRoomIdAsync(int roomId, CancellationToken token = default)
            => GetRealRoomIdAsync(Client, roomId, token);

        /// <summary>
        /// 异步获取原房间号
        /// </summary>
        /// <param name="roomId">将要查询的房间号</param>
        /// <returns>原房间号</returns>
        public static ValueTask<int> GetRealRoomIdAsync(HttpClient client, int roomId, CancellationToken token = default)
        {
            if (OriginRoomCache.TryGetValue(roomId, out int result))
            {
                return new ValueTask<int>(result);
            }
            static async ValueTask<int> InternalGetRealRoomIdAsync(HttpClient client, int roomId, CancellationToken token = default)
            {
                using JsonDocument j = await RequestRoomInitAsync(client, roomId, token);
                JsonElement root = j.RootElement;
                ParseRoomInitResp(root, out int shortRoomId, out int originRoomId);
                ProcessCache(shortRoomId, originRoomId);
                return originRoomId;
            }
            return InternalGetRealRoomIdAsync(client, roomId, token);
        }
    }
}
