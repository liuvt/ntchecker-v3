using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace TaxiNT.Controllers;

[Route("sitemap.xml")]
[ApiController]
public class SitemapController : ControllerBase
{
    //Get API Server
    private readonly ILogger<SitemapController> logger;
    public SitemapController(ILogger<SitemapController> _logger)
    {
        this.logger = _logger;
    }
    [HttpGet]
    [ResponseCache(Duration = 86400)] // Cache 1 ngày cho Google bot
    public IActionResult Index()
    {
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var staticUrls = new[]
        {
            new { loc = "https://taxinamthang.com/", priority = "1.0", changefreq = "daily" },
            //new { loc = "https://taxinamthang.com/gioi-thieu", priority = "0.8", changefreq = "monthly" },
            //new { loc = "https://taxinamthang.com/lien-he", priority = "0.6", changefreq = "yearly" },
        };

        var sb = new StringBuilder();
        sb.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");
        sb.AppendLine(@"<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">");

        foreach (var url in staticUrls)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{url.loc}</loc>");
            sb.AppendLine($"    <lastmod>{now}</lastmod>");
            sb.AppendLine($"    <changefreq>{url.changefreq}</changefreq>");
            sb.AppendLine($"    <priority>{url.priority}</priority>");
            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");

        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }
}