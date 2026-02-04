using System.Collections.Generic;
using System.Threading.Tasks;
using DoAnCoSo.Models;

namespace DoAnCoSo.Repositories
{
    public interface INguoiDungRepository
    {
        Task<IEnumerable<NguoiDung>> GetAllAsync();
        Task<NguoiDung> GetByIdAsync(int maNguoiDung);
        Task AddAsync(NguoiDung nguoiDung);
        Task UpdateAsync(NguoiDung nguoiDung);
        Task DeleteAsync(int maNguoiDung);
    }
}
