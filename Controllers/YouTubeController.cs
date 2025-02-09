using Microsoft.AspNetCore.Mvc;
using YouTubeApiProject.Services;
using YouTubeApiProject.Models;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace YouTubeApiProject.Controllers
{
    public class YouTubeController : Controller
    {
        private readonly YouTubeApiService _youtubeService;

        public YouTubeController(YouTubeApiService youtubeService)
        {
            _youtubeService = youtubeService;
        }

        // Display Search Page
        public IActionResult Index()
        {
            return View(new List<YouTubeVideoModel>()); // Pass an empty list initially
        }

        // Handle the search query
        [HttpPost]
        public async Task<IActionResult> Search(string query, string duration, string pageToken = "")
        {
            try
            {
                Console.WriteLine($"[DEBUG] Search called with Query='{query}', Duration='{duration}', PageToken='{pageToken}'");

                if (string.IsNullOrWhiteSpace(query))
                {
                    ModelState.AddModelError("query", "Search query cannot be empty.");
                    return View("Index", new List<YouTubeVideoModel>());
                }

                var (videos, nextPageToken, prevPageToken) = await _youtubeService.SearchVideosAsync(query, duration, pageToken);

                ViewBag.Query = query;
                ViewBag.Duration = duration;
                ViewBag.NextPageToken = nextPageToken;
                ViewBag.PrevPageToken = prevPageToken;

                if (!videos.Any())
                {
                    ViewBag.Message = "No videos found.";
                }

                return View("Index", videos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception in Search: {ex.Message}");
                ModelState.AddModelError(string.Empty, "An error occurred while searching.");
                return View("Index", new List<YouTubeVideoModel>());
            }
        }
    }
}





