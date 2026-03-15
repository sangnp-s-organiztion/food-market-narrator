using admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace admin.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Analytics));
        }

        public IActionResult Analytics()
        {
            ViewData["Title"] = "Analytics";
            ViewData["TopbarTitle"] = "Analytics Overview";
            ViewData["TopbarSubtitle"] = "Real-time performance and user behavior metrics for Vinh Khanh street.";
            ViewData["PageKey"] = "analytics";
            return View();
        }

        public IActionResult PoiManagement()
        {
            ViewData["Title"] = "POI Management";
            ViewData["TopbarTitle"] = "POI Management";
            ViewData["TopbarSubtitle"] = "Review, update, and maintain all points of interest on the street.";
            ViewData["PageKey"] = "poi-management";
            return View();
        }

        public IActionResult PoiEditor()
        {
            ViewData["Title"] = "POI Editor";
            ViewData["TopbarTitle"] = "POI Editor";
            ViewData["TopbarSubtitle"] = "Edit detailed POI content including media and narrations.";
            ViewData["PageKey"] = "poi-editor";
            return View();
        }

        public IActionResult ApprovalQueue()
        {
            ViewData["Title"] = "Approval Queue";
            ViewData["TopbarTitle"] = "Approval Queue";
            ViewData["TopbarSubtitle"] = "Moderate pending POI submissions from food street sellers.";
            ViewData["PageKey"] = "approval-queue";
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [AllowAnonymous]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
