namespace BililiveNotification.Models
{
    public class UserInfo
    {
        public string UserName { get; }

        public int UserId { get; }

        public string FaceUrl { get; }

        public UserInfo(string userName, int userId, string faceUrl)
        {
            UserName = userName;
            UserId = userId;
            FaceUrl = faceUrl;
        }
    }
}
