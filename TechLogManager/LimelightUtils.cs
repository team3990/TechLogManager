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
        public static async Task<List<RecordingDetail>> GetRecordingsAsync(string llname)
        {
            return (await GetVideoListAsync(llname))
                .Select(rec => new RecordingDetail(rec, llname))
                .Reverse() // So its oldest first
                .ToList();
        }

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
        public string name { get; set; } = "";
        public long size { get; set; } = 0;
    }

    public class RecordingDetail(string name, string limelightName) : IDisposable
    {
        private readonly HttpClient _client = new();
        public string Video { get; private set; } = name + ".avi";
        public string Manifest { get; private set; } = name + "_manifest.jsonl";
        public string Bootlog { get; private set; } = name + "_bootlog.txt.gz";
        public readonly string Name = limelightName + "_" + name;

        public void Dispose()
        {
            _client.Dispose();
        }

        public RecordingDetail GetLinks()
        {
            var baseLink = $"http://{limelightName}.local:5807/recording/";

            Video = baseLink + Video;
            Manifest = baseLink + Manifest;
            Bootlog = baseLink + Bootlog;

            return this;
        }

        public async Task Delete()
        {
            var response = await _client.DeleteAsync($"http://{limelightName}.local:5807/delete-video?name={Video}");
            response.EnsureSuccessStatusCode();
            Dispose();
        }
    }
}
