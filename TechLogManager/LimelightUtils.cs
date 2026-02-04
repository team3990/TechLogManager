using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
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

            return (from vid in videoInfo select vid.name).ToList();
        }

        public static async Task<List<RecordingDetail>> GetRecordingsAsync(string llname)
        {
            var response = await HttpClient.GetAsync($"http://{llname}.local:5807/recording-list");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var recs = JsonSerializer.Deserialize<RecordingListResponse>(json)?.recordings ?? [];
            recs.Reverse();// so its older first

            return recs;
        }

        public static List<RecordingDetail> GetRecordingLinks(string llname)
        {
            var recordings = GetRecordingsAsync(llname).Result;
            return (from rec in recordings select new RecordingDetail(llname, rec)).ToList();
        }

        public static async Task<List<RecordingDetail>> GetRecordingLinksAsync(string llname)
        {
            var recordings = await GetRecordingsAsync(llname);
            return (from rec in recordings select new RecordingDetail(llname, rec)).ToList();
        }

        public static void DeleteAllVideos(string llname)
        {
            DeleteAllVideosAsync(llname).Wait();
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
