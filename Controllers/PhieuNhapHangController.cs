using DoAnCoSo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DoAnCoSo.Controllers
{
    [Authorize(Roles = "Nhà cung cấp")]
    public class PhieuNhapHangController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SeafoodDbContext _context;

        public PhieuNhapHangController(UserManager<ApplicationUser> userManager, SeafoodDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: PhieuNhapHang
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var nhaCungCap = await _context.NhaCungCaps.FirstOrDefaultAsync(n => n.UserID == user.Id);
            if (nhaCungCap == null)
            {
                return NotFound();
            }

            var phieuNhapHangs = await _context.PhieuNhapHangs
                .Include(p => p.ChiTietPhieuNhaps)
                .ThenInclude(c => c.SanPham)
                .Where(p => p.MaNhaCungCapID == nhaCungCap.MaNCC)
                .ToListAsync();

            return View(phieuNhapHangs);
        }

        // GET: PhieuNhapHang/Create
        public async Task<IActionResult> Create()
        {
            // Load danh mục và loại sản phẩm để nhà cung cấp chọn
            ViewBag.DanhMucs = new SelectList(await _context.DanhMucs.ToListAsync(), "MaDanhMuc", "TenDanhMuc");
            ViewBag.LoaiSanPhams = new SelectList(await _context.LoaiSanPhams.ToListAsync(), "MaLoai", "TenLoai");
            return View(new PhieuNhapHang());
        }

        // POST: PhieuNhapHang/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PhieuNhapHang phieuNhapHang, List<SanPham> sanPhams, List<int> soLuongSanPhams, List<decimal> giaNhapHangs)
        {
            try
            {
                if (!ModelState.IsValid || sanPhams == null || !sanPhams.Any() || soLuongSanPhams == null || giaNhapHangs == null)
                {
                    ViewBag.DanhMucs = new SelectList(await _context.DanhMucs.ToListAsync(), "MaDanhMuc", "TenDanhMuc");
                    ViewBag.LoaiSanPhams = new SelectList(await _context.LoaiSanPhams.ToListAsync(), "MaLoai", "TenLoai");
                    ModelState.AddModelError("", "Vui lòng nhập ít nhất một sản phẩm và thông tin liên quan.");
                    return View(phieuNhapHang);
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }

                var nhaCungCap = await _context.NhaCungCaps.FirstOrDefaultAsync(n => n.UserID == user.Id);
                if (nhaCungCap == null)
                {
                    return NotFound();
                }

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Tạo phiếu nhập hàng
                        phieuNhapHang.MaNhaCungCapID = nhaCungCap.MaNCC;
                        _context.PhieuNhapHangs.Add(phieuNhapHang);
                        await _context.SaveChangesAsync();

                        decimal tongTien = 0;
                        int tongSoLuong = 0;

                        // Thêm từng sản phẩm và chi tiết phiếu nhập
                        for (int i = 0; i < sanPhams.Count; i++)
                        {
                            var sanPham = sanPhams[i];
                            
                            // Đảm bảo các trường bắt buộc không null
                            sanPham.TenSanPham = sanPham.TenSanPham ?? "Sản phẩm mới";
                            sanPham.LoaiBaoQuan = sanPham.LoaiBaoQuan ?? "Tiêu chuẩn";
                            sanPham.TinhTrang = sanPham.TinhTrang ?? "Còn hàng";
                            sanPham.NguonGoc = sanPham.NguonGoc ?? "Nội địa";
                            sanPham.MoTa = sanPham.MoTa ?? "Chưa có mô tả";
                            
                            sanPham.SoLuong = soLuongSanPhams[i];
                            sanPham.GiaTheoKG = giaNhapHangs[i] * 1.2m; // Giá bán = giá nhập * 1.2

                            _context.SanPhams.Add(sanPham);
                            await _context.SaveChangesAsync();

                            // Tạo chi tiết phiếu nhập
                            var chiTiet = new ChiTietPhieuNhap
                            {
                                MaPhieuNhap = phieuNhapHang.MaPhieuNhap,
                                MaSanPham = sanPham.MaSanPham,
                                SoLuongSanPham = soLuongSanPhams[i],
                                GiaNhapHang = giaNhapHangs[i]
                            };
                            _context.ChiTietPhieuNhaps.Add(chiTiet);

                            tongTien += chiTiet.SoLuongSanPham * chiTiet.GiaNhapHang;
                            tongSoLuong += chiTiet.SoLuongSanPham;
                        }

                        // Cập nhật tổng tiền và tổng số lượng
                        phieuNhapHang.TongTien = tongTien;
                        phieuNhapHang.TongSoLuong = tongSoLuong;
                        _context.Update(phieuNhapHang);

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        TempData["SuccessMessage"] = "Tạo phiếu nhập hàng thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Có lỗi xảy ra: {ex.Message}");
                ViewBag.DanhMucs = new SelectList(await _context.DanhMucs.ToListAsync(), "MaDanhMuc", "TenDanhMuc");
                ViewBag.LoaiSanPhams = new SelectList(await _context.LoaiSanPhams.ToListAsync(), "MaLoai", "TenLoai");
                return View(phieuNhapHang);
            }
        }

        // GET: PhieuNhapHang/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phieuNhapHang = await _context.PhieuNhapHangs
                .Include(p => p.NhaCungCap)
                .Include(p => p.ChiTietPhieuNhaps)
                .ThenInclude(c => c.SanPham)
                .FirstOrDefaultAsync(m => m.MaPhieuNhap == id);

            if (phieuNhapHang == null)
            {
                return NotFound();
            }

            return View(phieuNhapHang);
        }
    }
}