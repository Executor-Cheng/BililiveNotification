namespace BililiveNotification.Models
{
    public class RoomInfo
    {
        public string UserName { get; }

        public long UserId { get; }

        public string FaceUrl { get; }

        public string? CoverUrl { get; }

        public RoomInfo(string userName, long userId, string faceUrl, string? coverUrl)
        {
            UserName = userName;
            UserId = userId;
            FaceUrl = faceUrl;
            CoverUrl = coverUrl;
        }
    }
}
