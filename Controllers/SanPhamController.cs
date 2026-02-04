using DoAnCoSo.Models;
using DoAnCoSo.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System;
using System.Linq;

namespace DoAnCoSo.Controllers
{
    public class SanPhamController : Controller
    {
        private readonly ISanPhamRepository _sanPhamRepository;
        private readonly IDanhMucRepository _danhMucRepository;
        private readonly ILoaiSanPhamRepository _loaisanphamRepository;
        private readonly SeafoodDbContext _context;

        public SanPhamController(
            ISanPhamRepository sanPhamRepository,
            IDanhMucRepository danhMucRepository,
            ILoaiSanPhamRepository loaisanphamRepository,
            SeafoodDbContext context)
        {
            _sanPhamRepository = sanPhamRepository;
            _danhMucRepository = danhMucRepository;
            _loaisanphamRepository = loaisanphamRepository;
            _context = context;
        }

        // Các hành động hiện có (Index, Add, Display, Update, Delete, SanPhamTheoDanhMuc, SanPhamTheoLoai)

        public async Task<IActionResult> Index()
        {
            var danhSach = await _sanPhamRepository.GetAllAsync();
            foreach (var sanPham in danhSach)
            {
                // Tính tổng số lượng từ ChiTietKhoHang (nếu cần đồng bộ)
                sanPham.SoLuong = await _context.ChiTietKhoHangs
                    .Where(ct => ct.MaSanPhamID == sanPham.MaSanPham)
                    .SumAsync(ct => (int?)ct.SoLuong) ?? 0;
            }
            return View(danhSach);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add()
        {
            var DanhMucs = await _danhMucRepository.GetAllAsync();
            ViewBag.DanhMucs = new SelectList(DanhMucs, "MaDanhMuc", "TenDanhMuc");
            var LoaiSanPhams = await _loaisanphamRepository.GetAllAsync();
            ViewBag.LoaiSanPhams = new SelectList(LoaiSanPhams, "MaLoai", "TenLoai");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add(SanPham sanPham, IFormFile imageUrl)
        {
            if (!ModelState.IsValid)
            {
                var DanhMucs = await _danhMucRepository.GetAllAsync();
                ViewBag.DanhMucs = new SelectList(DanhMucs, "MaDanhMuc", "TenDanhMuc");
                var LoaiSanPhams = await _loaisanphamRepository.GetAllAsync();
                ViewBag.LoaiSanPhams = new SelectList(LoaiSanPhams, "MaLoai", "TenLoai");
                return View(sanPham);
            }

            if (imageUrl != null && imageUrl.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageUrl.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageUrl.CopyToAsync(fileStream);
                }

                sanPham.ImageUrl = "/images/" + fileName;
            }

            sanPham.SoLuong = 0; // Khởi tạo số lượng ban đầu là 0
            await _sanPhamRepository.AddAsync(sanPham);
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Display(int id)
        {
            var sanPham = await _context.SanPhams
                .Include(s => s.DanhGias)
                    .ThenInclude(d => d.NguoiDung)
                .FirstOrDefaultAsync(s => s.MaSanPham == id);

            if (sanPham == null)
            {
                return NotFound();
            }

            // Tính lại số lượng từ chi tiết kho
            sanPham.SoLuong = await _context.ChiTietKhoHangs
                .Where(ct => ct.MaSanPhamID == sanPham.MaSanPham)
                .SumAsync(ct => (int?)ct.SoLuong) ?? 0;

            // Tính trung bình đánh giá
            if (sanPham.DanhGias != null && sanPham.DanhGias.Any())
            {
                ViewBag.AverageRating = sanPham.DanhGias.Average(d => d.XepHang);
                ViewBag.TotalReviews = sanPham.DanhGias.Count();
            }
            else
            {
                ViewBag.AverageRating = 0;
                ViewBag.TotalReviews = 0;
            }

            return View(sanPham);
        }

        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var sanPham = await _sanPhamRepository.GetByIdAsync(id);
            if (sanPham == null)
                return NotFound();

            // Đồng bộ số lượng từ kho
            sanPham.SoLuong = await _context.ChiTietKhoHangs
                .Where(ct => ct.MaSanPhamID == sanPham.MaSanPham)
                .SumAsync(ct => (int?)ct.SoLuong) ?? 0;

            return View(sanPham);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, SanPham sanPham, IFormFile? imageUrl)
        {
            if (id != sanPham.MaSanPham)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var DanhMucs = await _danhMucRepository.GetAllAsync();
                ViewBag.DanhMucs = new SelectList(DanhMucs, "MaDanhMuc", "TenDanhMuc");
                var LoaiSanPhams = await _loaisanphamRepository.GetAllAsync();
                ViewBag.LoaiSanPhams = new SelectList(LoaiSanPhams, "MaLoai", "TenLoai");
                return View(sanPham);
            }

            try
            {
                var existingSanPham = await _sanPhamRepository.GetByIdAsync(id);
                if (existingSanPham == null)
                {
                    return NotFound();
                }

                existingSanPham.TenSanPham = sanPham.TenSanPham;
                existingSanPham.NguonGoc = sanPham.NguonGoc;
                existingSanPham.NgayThuHoach = sanPham.NgayThuHoach;
                existingSanPham.LoaiBaoQuan = sanPham.LoaiBaoQuan;
                existingSanPham.GiaTheoKG = sanPham.GiaTheoKG;
                existingSanPham.TinhTrang = sanPham.TinhTrang;
                existingSanPham.DanhMucID = sanPham.DanhMucID;
                existingSanPham.MaLoaiID = sanPham.MaLoaiID;

                if (imageUrl != null && imageUrl.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageUrl.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageUrl.CopyToAsync(fileStream);
                    }

                    if (!string.IsNullOrEmpty(existingSanPham.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingSanPham.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    existingSanPham.ImageUrl = "/images/" + fileName;
                }

                // Đồng bộ số lượng (nếu có thay đổi trực tiếp từ form)
                if (sanPham.SoLuong.HasValue)
                {
                    var currentTotal = await _context.ChiTietKhoHangs
                        .Where(ct => ct.MaSanPhamID == id)
                        .SumAsync(ct => (int?)ct.SoLuong) ?? 0;
                    var difference = sanPham.SoLuong.Value - currentTotal;
                    existingSanPham.SoLuong = sanPham.SoLuong.Value;

                    // Cập nhật số lượng trong kho nếu cần (tùy chọn)
                    if (difference != 0)
                    {
                        // Logic điều chỉnh số lượng trong kho (có thể cần giao dịch)
                    }
                }

                await _sanPhamRepository.UpdateAsync(existingSanPham);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật sản phẩm: " + ex.Message);
                return View(sanPham);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            var sanPham = await _sanPhamRepository.GetByIdAsync(id);
            if (sanPham == null)
            {
                return NotFound();
            }
            return View(sanPham);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var sanPham = await _sanPhamRepository.GetByIdAsync(id);
                if (sanPham == null)
                {
                    TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
                    return RedirectToAction(nameof(Index));
                }

                var chiTietKhoHangs = await _context.ChiTietKhoHangs
                    .Where(ct => ct.MaSanPhamID == id)
                    .ToListAsync();
                if (chiTietKhoHangs.Any())
                {
                    TempData["ErrorMessage"] = "Không thể xóa sản phẩm vì vẫn còn trong kho.";
                    return RedirectToAction(nameof(Index));
                }

                sanPham.SoLuong = 0;
                await _sanPhamRepository.UpdateAsync(sanPham);
                await _sanPhamRepository.DeleteAsync(id);
                TempData["SuccessMessage"] = "Xóa sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra khi xóa sản phẩm: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // Kiểm tra MaSanPham và chuyển hướng đến ManageHinhAnh
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CheckMaSanPham(int maSanPham)
        {
            var sanPham = await _context.SanPhams.FindAsync(maSanPham);
            if (sanPham == null)
            {
                TempData["ErrorMessage"] = $"Mã sản phẩm {maSanPham} không tồn tại.";
                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("ManageHinhAnh", new { maSanPham });
        }

        // Quản lý HinhAnhSanPham
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageHinhAnh(int maSanPham)
        {
            var sanPham = await _context.SanPhams
                .Include(sp => sp.HinhAnhSanPhams)
                .FirstOrDefaultAsync(sp => sp.MaSanPham == maSanPham);
            if (sanPham == null)
            {
                TempData["ErrorMessage"] = $"Mã sản phẩm {maSanPham} không tồn tại.";
                return RedirectToAction("Index", "Home");
            }
            ViewBag.MaSanPham = maSanPham;
            return View(sanPham);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddHinhAnh(int maSanPham, IFormFile imageFile)
        {
            var sanPham = await _context.SanPhams.FindAsync(maSanPham);
            if (sanPham == null || imageFile == null || imageFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi thêm ảnh phụ.";
                return RedirectToAction("ManageHinhAnh", new { maSanPham });
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            var hinhAnh = new HinhAnhSanPham
            {
                LoaiHinhAnh = "/images/" + fileName,
                MaSanPhamID = maSanPham
            };
            _context.HinhAnhSanPhams.Add(hinhAnh);
            await _context.SaveChangesAsync();

            return RedirectToAction("ManageHinhAnh", new { maSanPham });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditHinhAnh(int maHinhAnh, IFormFile imageFile)
        {
            var hinhAnh = await _context.HinhAnhSanPhams.FindAsync(maHinhAnh);
            if (hinhAnh == null)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi sửa ảnh phụ.";
                return RedirectToAction("Index", "Home");
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
            if (imageFile != null && imageFile.Length > 0)
            {
                // Xóa ảnh cũ
                var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", hinhAnh.LoaiHinhAnh.TrimStart('/'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }

                // Tải lên ảnh mới
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                hinhAnh.LoaiHinhAnh = "/images/" + fileName;
            }

            _context.HinhAnhSanPhams.Update(hinhAnh);
            await _context.SaveChangesAsync();

            return RedirectToAction("ManageHinhAnh", new { maSanPham = hinhAnh.MaSanPhamID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteHinhAnh(int maHinhAnh)
        {
            var hinhAnh = await _context.HinhAnhSanPhams.FindAsync(maHinhAnh);
            if (hinhAnh == null)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa ảnh phụ.";
                return RedirectToAction("Index", "Home");
            }

            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", hinhAnh.LoaiHinhAnh.TrimStart('/'));
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }

            _context.HinhAnhSanPhams.Remove(hinhAnh);
            await _context.SaveChangesAsync();

            return RedirectToAction("ManageHinhAnh", new { maSanPham = hinhAnh.MaSanPhamID });
        }
        public async Task<IActionResult> SanPhamTheoDanhMuc(int id)
        {
            var sanPhams = await _sanPhamRepository.GetAllAsync();
            var filteredSanPhams = sanPhams.Where(sp => sp.DanhMucID == id).ToList();

            // Lấy tên danh mục để hiển thị
            var danhMuc = await _danhMucRepository.GetByIdAsync(id);
            ViewBag.TenDanhMuc = danhMuc?.TenDanhMuc ?? "Danh mục không xác định";

            return View(filteredSanPhams);
        }
        private string GetTinhTrang(int soLuong)
        {
            if (soLuong > 10) return "Còn Hàng";
            if (soLuong > 0) return "Sắp Hết";
            return "Hết Hàng";
        }

        [HttpPost]
        [Authorize] // Yêu cầu đăng nhập để đánh giá
        public async Task<IActionResult> AddReview(int maSanPham, int xepHang, string binhLuan)
        {
            if (xepHang < 1 || xepHang > 5)
            {
                return BadRequest("Xếp hạng phải từ 1 đến 5 sao");
            }

            var sanPham = await _sanPhamRepository.GetByIdAsync(maSanPham);
            if (sanPham == null)
            {
                return NotFound("Không tìm thấy sản phẩm");
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var nguoiDung = await _context.NguoiDungs.FirstOrDefaultAsync(n => n.UserID == userId);

            if (nguoiDung == null)
            {
                return BadRequest("Không tìm thấy thông tin người dùng");
            }

            var danhGia = new DanhGia
            {
                MaSanPhamID = maSanPham,
                MaNguoiDungID = nguoiDung.MaNguoiDung,
                XepHang = xepHang,
                BinhLuan = binhLuan,
                NgayDanhGia = DateTime.Now
            };

            await _context.DanhGias.AddAsync(danhGia);
            await _context.SaveChangesAsync();

            return RedirectToAction("Display", new { id = maSanPham });
        }
    }
}