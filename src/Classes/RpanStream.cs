namespace RpanList.Classes
{
    public class RpanStream
    {
        public string stream_id;
        public string hls_url;
        public long? publish_at;
        public long? hls_exists_at;
        public string thumbnail;
        public int? width;
        public int? height;
        public long? publish_done_at;
        public long? killed_at;
        public long? purged_at;
        public long? update_at;
        public long? ended_at;
        public string ended_reason;
        public string state;
        public int? duration_limit;
    }
}