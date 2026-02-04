using System.Diagnostics;
using DoAnCoSo.Models;
using DoAnCoSo.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DoAnCoSo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ISanPhamRepository _sanPhamRepository;
        private readonly ILogger<HomeController> _logger;
        private readonly SeafoodDbContext _context;
        private readonly IDanhMucRepository _danhMucRepository; // Thêm repository cho DanhMuc

        public HomeController(ILogger<HomeController> logger, SeafoodDbContext context , ISanPhamRepository sanPhamRepository, IDanhMucRepository danhMucRepository)
        {
            _sanPhamRepository = sanPhamRepository;
            _logger = logger;
            _context = context;
            _danhMucRepository = danhMucRepository;
        }


        public async Task<IActionResult> Index(string searchTerm)
        {
            var sanPhams = string.IsNullOrEmpty(searchTerm)
                ? await _sanPhamRepository.GetAllAsync()
                : await _sanPhamRepository.SearchAsync(searchTerm);

            var danhMucs = await _danhMucRepository.GetAllAsync();
            ViewBag.DanhMucs = danhMucs;
            ViewBag.SearchTerm = searchTerm; // Truy?n chu?i tìm ki?m vào view ?? hi?n th?
            return View(sanPhams);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
