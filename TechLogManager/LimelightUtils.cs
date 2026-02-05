using System.Text.Json;
using LimelightClasses;

namespace TechLogManager
{
    public static class LimelightUtils
    {
        private static readonly HttpClient HttpClient = new();

        public static async Task<List<string>> GetVideoListAsync(string llname)
        {
            var response = await HttpClient.GetAsync($"http://{llname}.local:5807/videolist");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var videoInfo = JsonSerializer.Deserialize<List<VideoInfo>>(json)
                            ?? throw new Exception($"limelight {llname} not found");

            return videoInfo.Select(vid => vid.name).ToList();
        }

        /**
         * Returns all the recordings, sorted by oldest to newest
         */
        public static async Task<List<RecordingDetail>> GetRecordingsAsync(string llname) =>
            (await GetVideoListAsync(llname))
            .Select(rec => new RecordingDetail(rec, llname))
            .Reverse()// So its oldest first
            .ToList();

        public static async Task DeleteAllVideosAsync(string llname)
        {
            var response = await HttpClient.DeleteAsync($"http://{llname}.local:5807/delete-videos");
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

    public class RecordingDetail(string name, string limelightName) : IDisposable
    {
        public string video { get; set; } = name + ".avi";
        public string manifest { get; set; } = name + "_manifest.jsonl";
        public string bootlog { get; set; } = name + "_bootlog.txt.gz";
        private readonly string llname = limelightName;
        private readonly HttpClient _client = new();

        public RecordingDetail GetLinks()
        {
            var baseLink = $"http://{llname}.local:5807/recording/";

            video = baseLink + video;
            manifest = baseLink + manifest;
            bootlog = baseLink + bootlog;

            return this;
        }

        public async Task Delete()
        {
            var response = await _client.DeleteAsync($"http://{llname}.local:5807/delete-video?name={video}");
            response.EnsureSuccessStatusCode();
            Dispose();
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
