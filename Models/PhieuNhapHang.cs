using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    public class PhieuNhapHang
    {
        [Key]
        public int MaPhieuNhap { get; set; }

        public decimal TongTien { get; set; }
        public int TongSoLuong { get; set; }

        public int? MaNhaCungCapID { get; set; }
        public NhaCungCap? NhaCungCap { get; set; }

        public ICollection<ChiTietPhieuNhap>? ChiTietPhieuNhaps { get; set; }
    }
}
