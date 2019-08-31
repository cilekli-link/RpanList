namespace RpanList.Classes
{
    public class RpanData
    {
        public int total_streams;
        public int rank;
        public int upvotes;
        public int downvotes;
        public int unique_watchers;
        public int continuous_watchers;
        public string updates_websocket;
        public bool chat_disabled;
        public bool is_first_broadcast;
        public string broadcast_time;
        public RedditPost post;
        public string share_link;
        public RpanStream stream;
    }
}