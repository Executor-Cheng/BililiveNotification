using System;
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
        /// 短房间号缓存字典
        /// </summary>
        public static IDictionary<int, int> ShortRoomCache { get; } = new ConcurrentDictionary<int, int>();

        /// <summary>
        /// 异步获取短房间号
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="NotImplementedException"/>
        /// <exception cref="TimeoutException"/>
        /// <param name="roomId">将要查询的房间号</param>
        /// <returns>短房间号,如果没有,则返回原房间号</returns>
        public static ValueTask<int> GetShortRoomIdAsync(int roomId, CancellationToken token = default)
            => GetShortRoomIdAsync(Client, roomId, token);

        /// <summary>
        /// 异步获取短房间号
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="NotImplementedException"/>
        /// <exception cref="TimeoutException"/>
        /// <param name="roomId">将要查询的房间号</param>
        /// <returns>短房间号,如果没有,则返回原房间号</returns>
        public static ValueTask<int> GetShortRoomIdAsync(HttpClient client, int roomId, CancellationToken token = default)
        {
            if (ShortRoomCache.TryGetValue(roomId, out int result))
            {
                return new ValueTask<int>(result);
            }
            static async ValueTask<int> InternalGetShortRoomIdAsync(HttpClient client, int roomId, CancellationToken token = default)
            {
                using JsonDocument j = await RequestRoomInitAsync(client, roomId, token);
                JsonElement root = j.RootElement;
                ParseRoomInitResp(root, out int shortRoomId, out int originRoomId);
                ProcessCache(shortRoomId, originRoomId);
                return shortRoomId;
            }
            return InternalGetShortRoomIdAsync(client, roomId, token);
        }
    }
}
