namespace BililiveNotification.Models
{
    public class UserInfo
    {
        public string UserName { get; }

        public long UserId { get; }

        public string FaceUrl { get; }

        public UserInfo(string userName, long userId, string faceUrl)
        {
            UserName = userName;
            UserId = userId;
            FaceUrl = faceUrl;
        }
    }
}
