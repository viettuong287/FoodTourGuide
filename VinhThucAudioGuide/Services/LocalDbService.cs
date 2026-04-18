using SQLite;
using System.IO;
using System.Threading.Tasks;
using VinhThucAudioGuide.Models;

namespace VinhThucAudioGuide.Services;

public class LocalDbService
{
    private SQLiteAsyncConnection _db;

    public async Task Init()
    {
        if (_db != null) return;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "VinhThucData.db");
        _db = new SQLiteAsyncConnection(dbPath);

        // ĐÃ SỬA THÀNH TourLocation
        await _db.CreateTableAsync<TourLocation>();
        await _db.CreateTableAsync<Language>();
        await _db.CreateTableAsync<Script>();
        await _db.CreateTableAsync<QRCodeData>();
        await _db.CreateTableAsync<UserDevice>();

        await SeedData();
    }

    private async Task SeedData()
    {
        // Nếu DB đã có dữ liệu rồi thì bỏ qua không nạp trùng
        if (await _db.Table<TourLocation>().CountAsync() > 0) return;

        // 1. NẠP NGÔN NGỮ
        var vi = new Language { LangCode = "vi", LangName = "Tiếng Việt" };
        var en = new Language { LangCode = "en", LangName = "English" };
        await _db.InsertAsync(vi);
        await _db.InsertAsync(en);

        // 2. NẠP DANH SÁCH 5 ĐỊA ĐIỂM
        var locations = new List<TourLocation>
        {
            new TourLocation { LocationName = "Bánh mì Huỳnh Hoa", Category = "Thức ăn", ImageUrl = "banhmihuynhhoa.jpg", Latitude = 10.771606907737766, Longitude = 106.69238606657234 },
            new TourLocation { LocationName = "Công viên Sáng Tạo", Category = "Vui chơi", ImageUrl = "congviensangtao.jpg", Latitude = 10.780051989368951, Longitude = 106.7126109954083 },
            new TourLocation { LocationName = "Công viên Tao Đàn", Category = "Vui chơi", ImageUrl = "congvientaodan.jpg", Latitude = 10.775579589373159, Longitude = 106.69206896657238 },
            new TourLocation { LocationName = "Phố Lồng đèn", Category = "Lễ hội", ImageUrl = "pholongden.jpg", Latitude = 10.753367070257587, Longitude = 106.66003093773622 },
            new TourLocation { LocationName = "Cơm tấm Sà Bì Chưởng", Category = "Thức ăn", ImageUrl = "comtamsabichuong.jpg", Latitude = 10.758594123027729, Longitude = 106.68044906916442 }
        };

        // Dùng vòng lặp nạp từng địa điểm, tạo mã QR và kịch bản tương ứng
        foreach (var loc in locations)
        {
            await _db.InsertAsync(loc);

            // Tự động tạo mã QR (VD: QR_BANHMIHUYNHHOA)
            string qrCode = "QR_" + loc.LocationName.Replace(" ", "").ToUpper();
            await _db.InsertAsync(new QRCodeData { CodeValue = qrCode, LocationId = loc.Id });

            // 3. NẠP KỊCH BẢN THUYẾT MINH CHI TIẾT
            if (loc.LocationName == "Bánh mì Huỳnh Hoa")
            {
                await _db.InsertAsync(new Script
                {
                    LocationId = loc.Id,
                    LanguageId = vi.Id,
                    Title = "Bánh mì Huỳnh Hoa - Vua Bánh Mì Sài Gòn",
                    Content = "Bánh mì Huỳnh Hoa từ lâu đã trở thành một biểu tượng ẩm thực đường phố không thể bỏ qua khi đến Sài Gòn. Nổi tiếng với ổ bánh mì đặc ruột, nặng trĩu tay, bên trong là lớp pate béo ngậy công thức gia truyền, bơ vàng óng ánh, cùng hàng chục loại chả lụa, dăm bông, thịt nguội xắt lát dày dặn. Cắn một miếng, bạn sẽ cảm nhận được sự hòa quyện tuyệt vời giữa độ giòn rụm của vỏ bánh và vị đậm đà, thơm lừng của nhân thịt. Dù lúc nào cũng phải xếp hàng chờ đợi, nhưng trải nghiệm thưởng thức ổ bánh mì trứ danh này chắc chắn sẽ làm hài lòng mọi thực khách đam mê ẩm thực."
                });
                await _db.InsertAsync(new Script
                {
                    LocationId = loc.Id,
                    LanguageId = en.Id,
                    Title = "Huynh Hoa Bakery",
                    Content = "Huynh Hoa Bakery has long been an unmissable street food icon when visiting Saigon. Famous for its dense, heavy baguettes, the inside is filled with a rich, secret-recipe pate, golden butter, and dozens of types of Vietnamese sausage, ham, and thick cold cuts. With one bite, you will feel the perfect harmony between the crispy crust and the savory, aromatic meat filling. Although you almost always have to wait in line, the experience of enjoying this renowned banh mi will surely satisfy any food-loving traveler."
                });
            }
            else if (loc.LocationName == "Công viên Sáng Tạo")
            {
                await _db.InsertAsync(new Script
                {
                    LocationId = loc.Id,
                    LanguageId = vi.Id,
                    Title = "Công viên Sáng Tạo",
                    Content = "Công viên Sáng Tạo là một không gian xanh hiện đại, được thiết kế đặc biệt để khơi nguồn cảm hứng và thúc đẩy sự đổi mới. Nơi đây không chỉ có những bãi cỏ rộng lớn, cây cối rợp bóng mát mà còn được tích hợp nhiều khu vực triển lãm nghệ thuật ngoài trời, các mô hình kiến trúc độc đáo và khu vui chơi tương tác cho giới trẻ. Đây là điểm đến lý tưởng cho những buổi dã ngoại cuối tuần, nơi các nhóm bạn có thể cùng nhau thảo luận ý tưởng, đọc sách hoặc đơn giản là thư giãn sau những giờ học tập và làm việc căng thẳng."
                });
                await _db.InsertAsync(new Script
                {
                    LocationId = loc.Id,
                    LanguageId = en.Id,
                    Title = "Creative Park",
                    Content = "The Creative Park is a modern green space specifically designed to inspire and foster innovation. It not only features expansive lawns and shaded trees but also integrates numerous outdoor art exhibition areas, unique architectural models, and interactive playgrounds for the youth. This is an ideal destination for weekend picnics, where groups of friends can gather to brainstorm ideas, read books, or simply relax after stressful hours of studying and working."
                });
            }
            else if (loc.LocationName == "Công viên Tao Đàn")
            {
                await _db.InsertAsync(new Script
                {
                    LocationId = loc.Id,
                    LanguageId = vi.Id,
                    Title = "Công viên Tao Đàn",
                    Content = "Được mệnh danh là 'lá phổi xanh' giữa lòng Sài Gòn nhộn nhịp, Công viên Tao Đàn sở hữu hơn 10 hecta diện tích với hàng ngàn cây cổ thụ rợp bóng mát. Điểm nhấn của công viên là khu đền tưởng niệm Vua Hùng và mô hình tháp Chàm thu nhỏ tuyệt đẹp. Vào mỗi buổi sáng sớm, bạn sẽ dễ dàng bắt gặp hình ảnh người dân tập thể dục, múa võ hay những hội nhóm chơi chim cảnh tụ tập thưởng thức tiếng chim hót. Đây thực sự là một ốc đảo bình yên giúp bạn tạm lánh khỏi sự ồn ào, náo nhiệt của phố thị."
                });
                await _db.InsertAsync(new Script
                {
                    LocationId = loc.Id,
                    LanguageId = en.Id,
                    Title = "Tao Dan Park",
                    Content = "Dubbed the 'green lung' in the heart of bustling Saigon, Tao Dan Park boasts over 10 hectares of area with thousands of ancient trees providing ample shade. The highlights of the park include the Hung King Memorial Temple and a beautiful miniature Cham tower model. Early every morning, you can easily catch sight of locals exercising, practicing martial arts, or bird enthusiast clubs gathering to enjoy the birdsong. This is truly a peaceful oasis that helps you escape the noise and hustle of the city."
                });
            }
            else if (loc.LocationName == "Phố Lồng đèn")
            {
                await _db.InsertAsync(new Script
                {
                    LocationId = loc.Id,
                    LanguageId = vi.Id,
                    Title = "Phố Lồng Đèn Lương Nhữ Học",
                    Content = "Nằm trên con đường Lương Nhữ Học, Phố Lồng đèn là điểm đến rực rỡ và mang đậm màu sắc văn hóa truyền thống nhất tại khu vực Chợ Lớn. Mỗi dịp Trung Thu về, cả con phố như bừng sáng bởi hàng ngàn chiếc lồng đèn đủ hình dáng, kích cỡ và màu sắc – từ lồng đèn giấy kính truyền thống hình ngôi sao, cá chép đến lồng đèn điện tử hiện đại. Bước đi dưới ánh sáng huyền ảo của những chiếc lồng đèn, lắng nghe âm thanh nhộn nhịp của người mua kẻ bán, bạn sẽ được đắm chìm trọn vẹn trong không khí lễ hội tuổi thơ đầy hoài niệm."
                });
                await _db.InsertAsync(new Script
                {
                    LocationId = loc.Id,
                    LanguageId = en.Id,
                    Title = "Lantern Street",
                    Content = "Located on Luong Nhu Hoc street, Lantern Street is the most vibrant and culturally rich destination in the Cho Lon area. Every Mid-Autumn Festival, the entire street lights up with thousands of lanterns of all shapes, sizes, and colors – from traditional cellophane lanterns shaped like stars and carp to modern electronic ones. Walking under the magical glow of the lanterns, listening to the bustling sounds of buyers and sellers, you will be fully immersed in the nostalgic, childhood festival atmosphere."
                });
            }
            else if (loc.LocationName == "Cơm tấm Sà Bì Chưởng")
            {
                await _db.InsertAsync(new Script
                {
                    LocationId = loc.Id,
                    LanguageId = vi.Id,
                    Title = "Cơm Tấm Sà Bì Chưởng",
                    Content = "Cơm tấm Sà Bì Chưởng không chỉ thu hút bởi cái tên độc đáo mà còn bởi hương vị chuẩn mực của đĩa cơm tấm Sài Gòn. Một phần ăn hoàn hảo bao gồm hạt cơm tấm tơi xốp, miếng sườn cốt lết nướng than hoa cháy xém cạnh, thơm lức mũi, quyện cùng lớp bì dai giòn và chả trứng hấp béo ngậy. Linh hồn của món ăn chính là chén nước mắm kẹo ngọt chua cay, rưới đều lên đĩa cơm kèm theo chút mỡ hành và tóp mỡ giòn tan. Đây là minh chứng rõ nét cho sự tinh túy của nền ẩm thực đường phố Việt Nam."
                });
                await _db.InsertAsync(new Script
                {
                    LocationId = loc.Id,
                    LanguageId = en.Id,
                    Title = "Sa Bi Chuong Broken Rice",
                    Content = "Sa Bi Chuong Broken Rice attracts diners not only with its unique name but also with the authentic flavor of standard Saigon broken rice. A perfect portion includes fluffy broken rice grains, a charcoal-grilled pork chop with slightly charred edges and an irresistible aroma, combined with crunchy shredded pork skin and a rich steamed egg meatloaf. The soul of the dish is the sweet, sour, and spicy thickened fish sauce, drizzled over the rice along with scallion oil and crispy pork cracklings. This is a clear testament to the essence of Vietnamese street food culinary culture."
                });
            }
        }
    }

    public async Task<Script> GetScriptByQRAndLanguage(string scannedCode, string langCode)
    {
        await Init();

        var qr = await _db.Table<QRCodeData>().Where(q => q.CodeValue == scannedCode).FirstOrDefaultAsync();
        if (qr == null) return null;

        var lang = await _db.Table<Language>().Where(l => l.LangCode == langCode).FirstOrDefaultAsync();
        if (lang == null) return null;

        var script = await _db.Table<Script>()
            .Where(s => s.LocationId == qr.LocationId && s.LanguageId == lang.Id)
            .FirstOrDefaultAsync();

        return script;
    }
    // HÀM LẤY TOÀN BỘ DANH SÁCH ĐỊA ĐIỂM TỪ DATABASE LÊN BẢN ĐỒ
    public async Task<List<TourLocation>> GetAllTourLocations()
    {
        await Init();
        // Lấy tất cả các dòng trong bảng TourLocation
        return await _db.Table<TourLocation>().ToListAsync();
    }
}