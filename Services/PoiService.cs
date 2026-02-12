using System.Collections;
using food_market_narrator.Models;
using Microsoft.Maui.Devices.Sensors;


namespace food_market_narrator.Services;

public class POIService
{
    private readonly List<POI> _pois;

    // Danh sach cac POI
    public POIService()
    {
        _pois = new List<POI>
        {
            new POI
            {
                Id = "oc-phat-1",
                Name = "Ốc Phát",
                Description = "Quán ốc hải sản tươi ngon, giá bình dân. Nổi bật với các món ốc len xào dừa, ốc hương rang muối, sò điệp nướng mỡ hành. Không gian thoải mái, phục vụ nhanh chóng.",
                Latitude = 10.761972909157093,
                Longitude = 106.70209728596608,
                Category = "Ốc - Hải Sản",
                Radius = 500,
                PriceRange = "30.000 – 120.000 VNĐ/món",
                OpeningHours = "16:00 – 23:00",
                Phone = "(028) 1234 5678",
                Address = "TP.HCM"
            },
            new POI
            {
                Id = "oc-thao-2",
                Name = "Quán Ốc Thảo",
                Description = "Thiên đường ốc hải sản ở Quận 4. Thực đơn phong phú, nguyên liệu tươi, nước sốt đậm vị. Không gian bình dân, gần gũi, luôn đông khách buổi tối.",
                Latitude = 10.761732598326033,
                Longitude =  106.70237164471263,
                Category = "Ốc - Hải Sản",
                Radius = 500,
                PriceRange = "40.000 – 150.000 VNĐ/món",
                OpeningHours = "15:00 – 23:30",
                Phone = "(028) 1234 5679",
                Address = "Quận 4, TP.HCM"
            },
            new POI
            {
                Id = "lang-restaurant-3",
                Name = "Làng Restaurant",
                Description = "Nhà hàng ẩm thực hiện đại, kết hợp truyền thống và trình bày tinh tế. Phù hợp tiếp khách, gặp đối tác hoặc hẹn hò.",
                Latitude = 10.761126129011814,
                Longitude = 106.70541654744625,
                Category = "Nhà Hàng Sang Trọng",
                Radius = 500,
                PriceRange = "150.000 – 400.000 VNĐ/món",
                OpeningHours = "10:00 – 22:00",
                Phone = "(028) 1234 5680",
                Address = "TP.HCM"
            },
            new POI
            {
                Id = "oc-vu-4",
                Name = "Quán Ốc Vũ",
                Description = "Ốc ngon chuẩn vị, phục vụ nhanh. Món lên đều tay, giá hợp lý. Ốc xào và nướng sa tế rất được yêu thích.",
                Latitude = 10.761407697463692,
                Longitude = 106.7027003633483,
                Category = "Ốc - Hải Sản",
                Radius = 500,
                PriceRange = "30.000 – 120.000 VNĐ/món",
                OpeningHours = "16:00 – 23:00",
                Phone = "(028) 1234 5681",
                Address = "TP.HCM"
            },
            new POI
            {
                Id = "be-oc-5",
                Name = "Quán Bò Ốc",
                Description = "Quán ốc bình dân, giá sinh viên. Hải sản tươi, chế biến đơn giản nhưng đậm đà.",
                Latitude = 10.763402443407372,
                Longitude = 106.70205076750194,
                Category = "Ốc - Hải Sản",
                Radius = 500,
                PriceRange = "25.000 – 100.000 VNĐ/món",
                OpeningHours = "15:30 – 22:30",
                Phone = "(028) 1234 5682",
                Address = "TP.HCM"
            },
            new POI
            {
                Id = "oc-loan-6",
                Name = "Ốc Loan",
                Description = "Quán ốc đậm vị, ăn là ghiền. Nổi tiếng với ốc xào cay, nướng sa tế và nước chấm đặc trưng.",
                Latitude = 10.761241606318567,
                Longitude = 106.7026195976886,
                Category = "Ốc - Hải Sản",
                Radius = 500,
                PriceRange = "35.000 – 130.000 VNĐ/món",
                OpeningHours = "16:00 – 23:30",
                Phone = "(028) 1234 5683",
                Address = "TP.HCM"
            },
            new POI
            {
                Id = "oc-hong-nhung-7",
                Name = "Ốc Hồng Nhung",
                Description = "Hải sản tươi sống, menu đa dạng. Phù hợp gia đình và nhóm bạn.",
                Latitude = 10.761167859974787,
                Longitude = 106.70305121075414,
                Category = "Ốc - Hải Sản",
                Radius = 500,
                PriceRange = "40.000 – 180.000 VNĐ/món",
                OpeningHours = "15:00 – 23:00",
                Phone = "(028) 1234 5684",
                Address = "TP.HCM"
            },
            new POI
            {
                Id = "lau-nuong-thuan-viet-8",
                Name = "Lẩu Nướng Thuần Việt",
                Description = "Lẩu & nướng cho nhóm đông, không gian rộng rãi, phù hợp liên hoan và sinh nhật.",
                Latitude = 10.760978824959505,
                Longitude = 106.70302754201865,
                Category = "Lẩu - Nướng",
                Radius = 500,
                PriceRange = "120.000 – 250.000 VNĐ/người",
                OpeningHours = "16:00 – 22:30",
                Phone = "(028) 1234 5685",
                Address = "TP.HCM"
            },
            new POI
            {
                Id = "oc-hoa-kieu-9",
                Name = "Ốc Hoa Kiều",
                Description = "Hương vị riêng, phong cách chế biến đặc trưng, mang trải nghiệm ẩm thực mới lạ.",
                Latitude = 10.760935570260248,
                Longitude = 106.70334311748142,
                Category = "Ốc - Hải Sản",
                Radius = 500,
                PriceRange = "40.000 – 150.000 VNĐ/món",
                OpeningHours = "16:00 – 23:00",
                Phone = "(028) 1234 5686",
                Address = "TP.HCM"
            },
            new POI
            {
                Id = "oc-oanh-10",
                Name = "Ốc Oanh",
                Description = "Quán ốc quen thuộc, chất lượng ổn định. Được nhiều khách quen tin tưởng nhờ món ăn đều tay và phục vụ nhanh chóng.",
                Latitude = 10.760761560031622,
                Longitude = 106.70330524631726,
                Category = "Ốc - Hải Sản",
                Radius = 500,
                PriceRange = "35.000 – 140.000 VNĐ/món",
                OpeningHours = "16:00 – 23:00",
                Phone = "(028) 1234 5687",
                Address = "TP.HCM"
            },
            new POI
            {
                Id = "chilli-bbq-hotpot-11",
                Name = "Chilli BBQ Hotpot Restaurant",
                Description = "Lẩu & nướng cay nóng theo phong cách hiện đại. Phù hợp với giới trẻ thích vị đậm và không khí sôi động.",
                Latitude = 10.760704088063799,
                Longitude = 106.70363437959259,
                Category = "Lẩu - Nướng",
                Radius = 500,
                PriceRange = "150.000 – 300.000 VNĐ/người",
                OpeningHours = "11:00 – 22:00",
                Phone = "(028) 1234 5688",
                Address = "TP.HCM"
            },
            new POI
            {
                Id = "the-gioi-bo-12",
                Name = "Thế Giới Bò",
                Description = "Chuỗi quán chuyên bò nướng sốt đậm đà. Menu đa dạng, thịt mềm, giá hợp lý.",
                Latitude = 10.764057056507824,
                Longitude = 106.70127906132028,
                Category = "Nướng - Bò",
                Radius = 500,
                PriceRange = "120.000 – 250.000 VNĐ/người",
                OpeningHours = "11:00 – 22:00",
                Phone = "(028) 1234 5689",
                Address = "TP.HCM"
            },
            new POI
            {
                Id = "alo-quan-13",
                Name = "Sốt & Lẩu – Alo Quán",
                Description = "Beer & Seafood. Địa điểm lý tưởng để vừa ăn hải sản vừa nhâm nhi bia trong không gian sôi động.",
                Latitude = 10.761121393586846,
                Longitude = 106.7047304273393,
                Category = "Hải Sản - Bia",
                Radius = 500,
                PriceRange = "130.000 – 300.000 VNĐ/người",
                OpeningHours = "16:00 – 23:30",
                Phone = "(028) 1234 5690",
                Address = "TP.HCM"
            },
            new POI
            {
                Id = "lau-met-nuong-79k-14",
                Name = "Lẩu Mệt Nướng 79K",
                Description = "Ngon – rẻ – phù hợp sinh viên. Nổi bật với mức giá tiết kiệm chỉ từ 79K.",
                Latitude = 10.760797616074754,
                Longitude = 106.70432167038692,
                Category = "Lẩu - Nướng",
                Radius = 500,
                PriceRange = "Từ 79.000 VNĐ",
                OpeningHours = "16:00 – 22:30",
                Phone = "(028) 1234 5691",
                Address = "TP.HCM"
            },
            new POI
            {
                Id = "toan-phuong-quan-15",
                Name = "Toàn Phương Quán",
                Description = "Quán ăn lâu năm tại Quận 4. Cái tên quen thuộc khi nhắc đến con đường ẩm thực Vĩnh Khánh.",
                Latitude = 10.761376029856681,
                Longitude = 106.70251563883349,
                Category = "Ốc - Hải Sản",
                Radius = 500,
                PriceRange = "40.000 – 180.000 VNĐ/món",
                OpeningHours = "15:00 – 23:30",
                Phone = "(028) 1234 5692",
                Address = "Vĩnh Khánh, Quận 4, TP.HCM"
            },
            new POI
            {
                Id = "them-nuong-yakiniku-16",
                Name = "Thèm Nướng – Yakiniku",
                Description = "Phong cách nướng Nhật Bản với nguyên liệu chất lượng và không gian ấm cúng.",
                Latitude = 10.760800296651315,
                Longitude = 106.7047493782212,
                Category = "Nướng - Nhật Bản",
                Radius = 500,
                PriceRange = "180.000 – 350.000 VNĐ/người",
                OpeningHours = "11:00 – 22:00",
                Phone = "(028) 1234 5693",
                Address = "TP.HCM"
            }
        };
    }

    // Lấy tất cả các POIs
    public List<POI> GetAllPOIs()
    {
        return _pois;
    }

    // Lấy tất cả các POIs đồng bộ
    public async Task<List<POI>> GetAllPOIsAsync()
    {
        return await Task.FromResult(_pois);
    }





}