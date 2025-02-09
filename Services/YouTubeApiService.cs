using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using YouTubeApiProject.Models;

namespace YouTubeApiProject.Services
{
    public class YouTubeApiService
    {
        private readonly string _apiKey;

        public YouTubeApiService(IConfiguration configuration)
        {
            _apiKey = configuration["YouTubeApiKey"];
        }

        public async Task<(List<YouTubeVideoModel>, string, string)> SearchVideosAsync(string query, string durationFilter = "", string pageToken = "")
        {
            try
            {
                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    ApiKey = _apiKey,
                    ApplicationName = "YoutubeProject"
                });

                var searchRequest = youtubeService.Search.List("snippet");
                searchRequest.Q = query;
                searchRequest.MaxResults = 9;
                searchRequest.Type = "video";
                searchRequest.PageToken = pageToken; // Apply pagination token

                // Apply duration filter
                if (!string.IsNullOrEmpty(durationFilter))
                {
                    try
                    {
                        searchRequest.VideoDuration = durationFilter.ToLower() switch
                        {
                            "short" => SearchResource.ListRequest.VideoDurationEnum.Short__,
                            "medium" => SearchResource.ListRequest.VideoDurationEnum.Medium,
                            "long" => SearchResource.ListRequest.VideoDurationEnum.Long__,
                            _ => throw new ArgumentException($"Invalid duration filter: {durationFilter}")
                        };
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine($"[WARNING] {ex.Message}");
                        searchRequest.VideoDuration = null; // Ignore filter if invalid
                    }
                }

                var searchResponse = await searchRequest.ExecuteAsync();

                if (searchResponse?.Items == null || !searchResponse.Items.Any())
                {
                    Console.WriteLine("[INFO] No videos found for the given search.");
                    return (new List<YouTubeVideoModel>(), null, null);
                }

                var videoIds = string.Join(",", searchResponse.Items.Select(i => i.Id.VideoId));

                if (string.IsNullOrEmpty(videoIds))
                {
                    Console.WriteLine("[INFO] No video IDs found in search response.");
                    return (new List<YouTubeVideoModel>(), null, null);
                }

                // Retrieve video details (duration, thumbnail, title, etc.)
                var videoRequest = youtubeService.Videos.List("contentDetails");
                videoRequest.Id = videoIds;

                var videoResponse = await videoRequest.ExecuteAsync();

                if (videoResponse?.Items == null || !videoResponse.Items.Any())
                {
                    Console.WriteLine("[INFO] No additional video details found.");
                    return (new List<YouTubeVideoModel>(), null, null);
                }

                var videos = searchResponse.Items.Select(item =>
                {
                    var videoDetails = videoResponse.Items.FirstOrDefault(v => v.Id == item.Id.VideoId);
                    string duration = videoDetails?.ContentDetails?.Duration ?? "Unknown";

                    return new YouTubeVideoModel
                    {
                        Title = item.Snippet.Title,
                        Description = item.Snippet.Description,
                        ThumbnailUrl = item.Snippet.Thumbnails?.Medium?.Url ?? "No thumbnail",
                        VideoUrl = "https://www.youtube.com/watch?v=" + item.Id.VideoId,
                        Duration = duration
                    };
                }).ToList();

                return (videos, searchResponse.NextPageToken, searchResponse.PrevPageToken);
            }
            catch (Google.GoogleApiException ex)
            {
                Console.WriteLine($"[ERROR] YouTube API Error: {ex.Message}");
                return (new List<YouTubeVideoModel>(), null, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Unexpected Error: {ex.Message}");
                return (new List<YouTubeVideoModel>(), null, null);
            }
        }
    }
}

