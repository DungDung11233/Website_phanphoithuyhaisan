using System.ComponentModel.DataAnnotations;

namespace DoAnCoSo.Models
{
    public class NguoiDung
    {
        [Key]
        public int MaNguoiDung { get; set; }
        public string TenNguoiDung { get; set; }
        public string SoDienThoai { get; set; }
        public string Email { get; set; }
        public string DiaChi { get; set; }
        public DateTime NgaySinh { get; set; }

        public string? UserID { get; set; }
        public ApplicationUser? User { get; set; }

        public ICollection<DonHang>? DonHangs { get; set; }
        public ICollection<DanhGia>? DanhGias { get; set; }
        public ICollection<HoaDon>? HoaDons { get; set; }
    }


}
