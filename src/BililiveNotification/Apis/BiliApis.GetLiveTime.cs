using Executorlibs.Shared;
using Executorlibs.Shared.Exceptions;
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
            using JsonDocument j = await RequestRoomInitAsync(client, roomId, token);
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
