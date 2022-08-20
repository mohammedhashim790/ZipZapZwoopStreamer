namespace Streamer.AWS
{
    public class Development : Environment
    {
        public string SessionTableName { get; set; }
        public string StorageURL { get; set; }
        public string Environment { get; set ; }

        public Development()
        {
            this.SessionTableName = "Session-2adlfeoqwvgqhh3rrhfbn6shbq-dev";
            this.StorageURL = "https://zipzapzwoop-storage115812-dev.s3.ap-south-1.amazonaws.com/public/res";
            this.Environment = "Development";
        }

    }
}
