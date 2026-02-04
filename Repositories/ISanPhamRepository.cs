using System.Collections.Generic;
using System.Threading.Tasks;
using DoAnCoSo.Models;

namespace DoAnCoSo.Repositories
{
    public interface ISanPhamRepository
    {
        Task<IEnumerable<SanPham>> GetAllAsync();
        Task<SanPham> GetByIdAsync(int maSanPham);
        Task<List<SanPham>> SearchAsync(string searchTerm); 
        Task AddAsync(SanPham sanPham);
        Task UpdateAsync(SanPham sanPham);
        Task DeleteAsync(int maSanPham);
    }
}
