using DoAnCoSo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo.Controllers
{
    [Authorize(Roles = "Admin")]
    public class NhaKhoController : Controller
    {
        private readonly SeafoodDbContext _context;

        public NhaKhoController(SeafoodDbContext context)
        {
            _context = context;
        }

        // Hiển thị danh sách nhà kho
        public async Task<IActionResult> Index()
        {
            var nhaKhos = await _context.NhaKhos.ToListAsync();
            return View(nhaKhos);
        }

        // Chi tiết nhà kho
        public async Task<IActionResult> Details(int id)
        {
            var nhaKho = await _context.NhaKhos
                .Include(nk => nk.ChiTietKhoHangs)
                .ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(nk => nk.MaNhaKho == id);

            if (nhaKho == null)
            {
                return NotFound();
            }
            return View(nhaKho);
        }

        // Thêm nhà kho
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(NhaKho nhaKho)
        {
            if (ModelState.IsValid)
            {
                _context.NhaKhos.Add(nhaKho);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(nhaKho);
        }

        // Sửa nhà kho
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var nhaKho = await _context.NhaKhos.FindAsync(id);
            if (nhaKho == null)
            {
                return NotFound();
            }
            return View(nhaKho);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(NhaKho nhaKho)
        {
            if (ModelState.IsValid)
            {
                _context.Update(nhaKho);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(nhaKho);
        }

        // Xóa nhà kho
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var nhaKho = await _context.NhaKhos
                .Include(nk => nk.ChiTietKhoHangs)
                .FirstOrDefaultAsync(nk => nk.MaNhaKho == id);

            if (nhaKho == null)
            {
                return NotFound();
            }
            if (nhaKho.ChiTietKhoHangs.Any())
            {
                TempData["Error"] = "Không thể xóa nhà kho vì còn sản phẩm trong kho.";
                return RedirectToAction("Index");
            }
            _context.NhaKhos.Remove(nhaKho);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // Thêm sản phẩm vào kho
        [HttpGet]
        public IActionResult AddProductToWarehouse(int id)
        {
            ViewBag.SanPhams = new SelectList(_context.SanPhams, "MaSanPham", "TenSanPham");
            return View(new ChiTietKhoHang { MaNhaKhoID = id });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddProductToWarehouse(ChiTietKhoHang chiTietKhoHang)
        {
            if (chiTietKhoHang.SoLuong <= 0)
            {
                ModelState.AddModelError("SoLuong", "Số lượng phải lớn hơn 0.");
            }
            var existingChiTiet = await _context.ChiTietKhoHangs
                .FirstOrDefaultAsync(ct => ct.MaNhaKhoID == chiTietKhoHang.MaNhaKhoID && ct.MaSanPhamID == chiTietKhoHang.MaSanPhamID);
            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var sanPham = await _context.SanPhams.FindAsync(chiTietKhoHang.MaSanPhamID);
                    if (sanPham == null)
                    {
                        return NotFound();
                    }

                    if (existingChiTiet != null)
                    {
                        existingChiTiet.SoLuong += chiTietKhoHang.SoLuong;
                        _context.Update(existingChiTiet);
                    }
                    else
                    {
                        _context.ChiTietKhoHangs.Add(chiTietKhoHang);
                    }
                    sanPham.SoLuong += chiTietKhoHang.SoLuong; // Đồng bộ tổng số lượng
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return RedirectToAction("Details", new { id = chiTietKhoHang.MaNhaKhoID });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            ViewBag.SanPhams = new SelectList(_context.SanPhams, "MaSanPham", "TenSanPham");
            return View(chiTietKhoHang);
        }

        // Sửa sản phẩm trong kho
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditProductInWarehouse(int id, int maSanPham)
        {
            var chiTiet = await _context.ChiTietKhoHangs
                .FirstOrDefaultAsync(ct => ct.MaNhaKhoID == id && ct.MaSanPhamID == maSanPham);

            if (chiTiet == null)
            {
                return NotFound();
            }

            ViewBag.SanPhams = new SelectList(_context.SanPhams, "MaSanPham", "TenSanPham", chiTiet.MaSanPhamID);
            return View(chiTiet);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProductInWarehouse(ChiTietKhoHang chiTietKhoHang)
        {
            if (chiTietKhoHang.SoLuong <= 0)
            {
                ModelState.AddModelError("SoLuong", "Số lượng phải lớn hơn 0.");
            }

            var existingChiTiet = await _context.ChiTietKhoHangs
                .FirstOrDefaultAsync(ct => ct.MaNhaKhoID == chiTietKhoHang.MaNhaKhoID && ct.MaSanPhamID == chiTietKhoHang.MaSanPhamID);

            if (existingChiTiet != null && ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var sanPham = await _context.SanPhams.FindAsync(chiTietKhoHang.MaSanPhamID);
                    if (sanPham != null)
                    {
                        sanPham.SoLuong += (chiTietKhoHang.SoLuong - existingChiTiet.SoLuong); // Cập nhật số lượng tổng
                        existingChiTiet.SoLuong = chiTietKhoHang.SoLuong;
                        existingChiTiet.NgayNhap = chiTietKhoHang.NgayNhap;

                        _context.Update(existingChiTiet);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }

                    return RedirectToAction("Details", new { id = chiTietKhoHang.MaNhaKhoID });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            ViewBag.SanPhams = new SelectList(_context.SanPhams, "MaSanPham", "TenSanPham", chiTietKhoHang.MaSanPhamID);
            return View(chiTietKhoHang);
        }

        // Xóa sản phẩm khỏi kho
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProductFromWarehouse(int id, int maSanPham)
        {
            var chiTiet = await _context.ChiTietKhoHangs
                .FirstOrDefaultAsync(ct => ct.MaNhaKhoID == id && ct.MaSanPhamID == maSanPham);
            if (chiTiet != null)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var sanPham = await _context.SanPhams.FindAsync(chiTiet.MaSanPhamID);
                    if (sanPham != null)
                    {
                        sanPham.SoLuong -= chiTiet.SoLuong; // Giảm tổng số lượng
                        _context.ChiTietKhoHangs.Remove(chiTiet);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            return RedirectToAction("Details", new { id });
        }
    }
}