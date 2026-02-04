using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using DoAnCoSo.Models;

namespace DoAnCoSo.Repositories
{
    public class NguoiDungRepository : INguoiDungRepository
    {
        private readonly SeafoodDbContext _context;

        public NguoiDungRepository(SeafoodDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<NguoiDung>> GetAllAsync()
        {
            return await _context.NguoiDungs
                                 .Include(n => n.DonHangs)
                                 .Include(n => n.DanhGias)
                                 .ToListAsync();
        }

        public async Task<NguoiDung> GetByIdAsync(int maNguoiDung)
        {
            return await _context.NguoiDungs
                                 .Include(n => n.DonHangs)
                                 .Include(n => n.DanhGias)
                                 .FirstOrDefaultAsync(n => n.MaNguoiDung == maNguoiDung);
        }

        public async Task AddAsync(NguoiDung nguoiDung)
        {
            await _context.NguoiDungs.AddAsync(nguoiDung);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(NguoiDung nguoiDung)
        {
            _context.NguoiDungs.Update(nguoiDung);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int maNguoiDung)
        {
            var nguoiDung = await _context.NguoiDungs.FindAsync(maNguoiDung);
            if (nguoiDung != null)
            {
                _context.NguoiDungs.Remove(nguoiDung);
                await _context.SaveChangesAsync();
            }
        }
    }
}
