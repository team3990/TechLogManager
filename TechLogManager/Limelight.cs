using System.Text.Json;
using LimelightClasses;

namespace TechLogManager
{
    public static class Limelight
    {
        private static readonly HttpClient HttpClient = new();

        public static List<string> GetVideoList(string llname)
        {
            var response = HttpClient.GetAsync($"http://{llname}.local:5807/videolist").Result;
            response.EnsureSuccessStatusCode();

            var json = response.Content.ReadAsStringAsync().Result;
            var videoInfo = JsonSerializer.Deserialize<List<VideoInfo>>(json)
                            ?? throw new Exception($"limelight {llname} not found");

            return (from vid in videoInfo select vid.name).ToList();
        }

        public static List<RecordingDetail> GetRecordings(string llname)
        {
            var response = HttpClient.GetAsync($"http://{llname}.local:5807/recording-list").Result;
            response.EnsureSuccessStatusCode();

            var json = response.Content.ReadAsStringAsync().Result;
            var recs = JsonSerializer.Deserialize<RecordingListResponse>(json).recordings;
            recs.Reverse();

            return recs;
        }

        public static List<RecordingDetail> GetRecordingLinks(string llname)
        {
            return (from rec in GetRecordings(llname) select new RecordingDetail(llname, rec)).ToList();
        }

        public static void DeleteAllVideos(string llname)
        {
            var response = HttpClient.DeleteAsync($"http://{llname}.local:5807/delete-videos").Result;
            response.EnsureSuccessStatusCode();
        }
    }
}

namespace LimelightClasses
{
    public class VideoInfo
    {
        public string name { get; set; }
        public long size { get; set; }
    }

    public class RecordingListResponse
    {
        public List<RecordingDetail> recordings { get; set; }
    }

    public class RecordingDetail
    {
        public RecordingDetail()
        {
        }

        public RecordingDetail(string llname, RecordingDetail rec)
        {
            var baseLink = $"http://{llname}.local:5807/recording/";

            video = baseLink + rec.video;
            manifest = baseLink + rec.manifest;
            bootlog = baseLink + rec.bootlog;
        }

        public string video { get; set; }
        public string manifest { get; set; }
        public string bootlog { get; set; }
        public long size { get; set; }
    }
}
