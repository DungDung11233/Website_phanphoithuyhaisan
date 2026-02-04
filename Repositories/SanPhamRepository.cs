using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using DoAnCoSo.Models;

namespace DoAnCoSo.Repositories
{
    public class SanPhamRepository : ISanPhamRepository
    {
        private readonly SeafoodDbContext _context;

        public SanPhamRepository(SeafoodDbContext context)
        {
            _context = context;
        }
        public async Task<List<SanPham>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await _context.SanPhams.ToListAsync();
            }
            return await _context.SanPhams
                .Where(sp => sp.TenSanPham.ToLower().Contains(searchTerm) || 
                    sp.LoaiBaoQuan.ToLower().Contains(searchTerm) || 
                    (sp.TinhTrang != null && sp.TinhTrang.ToLower().Contains(searchTerm)))
                .ToListAsync();
        }

        public async Task<IEnumerable<SanPham>> GetAllAsync()
        {
            return await _context.SanPhams
                                 .Include(s => s.ChiTietPhieuNhaps)
                                 .Include(s => s.ChiTietDonHangs)
                                 .Include(s => s.ChiTietKhoHangs)                            
                                 .Include(s => s.DanhGias)
                                 .Include(s => s.HinhAnhSanPhams)
                                 .Include(s => s.ChiTietMuas)
                                 .Include(s => s.DanhMuc)
                                 .Include(s => s.LoaiSanPham)
                                 .ToListAsync();
        }

        public async Task<SanPham> GetByIdAsync(int maSanPham)
        {
            return await _context.SanPhams
                                 .Include(s => s.ChiTietPhieuNhaps)
                                 .Include(s => s.ChiTietDonHangs)
                                 .Include(s => s.ChiTietKhoHangs)                            
                                 .Include(s => s.DanhGias)
                                 .Include(s => s.HinhAnhSanPhams)
                                 .Include(s => s.ChiTietMuas)
                                 .Include(s => s.DanhMuc)
                                 .Include(s => s.LoaiSanPham)
                                 .FirstOrDefaultAsync(s => s.MaSanPham == maSanPham);
        }

        public async Task AddAsync(SanPham sanPham)
        {
            _context.SanPhams.AddAsync(sanPham);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(SanPham sanPham)
        {
            _context.SanPhams.Update(sanPham);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int maSanPham)
        {
            var sanPham = await _context.SanPhams.FindAsync(maSanPham);
            if (sanPham != null)
            {
                _context.SanPhams.Remove(sanPham);
                await _context.SaveChangesAsync();
            }
        }

    }
}
