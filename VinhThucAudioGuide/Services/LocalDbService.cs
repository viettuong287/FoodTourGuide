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

    // ----------- Helper script generators (approx ~300 words each) -----------
    private string Generate_BanhMi_HuynhHoa_VI() =>
        "Bánh mì Huỳnh Hoa là một trong những biểu tượng ẩm thực đường phố nổi bật của Sài Gòn. Từ những ngày đầu hoạt động, quán đã thu hút hàng dài thực khách bằng hương vị đặc trưng không thể nhầm lẫn: vỏ bánh giòn rụm, ruột đặc, nhân phong phú gồm pate, chả, thịt nguội, bơ và rau thơm được kết hợp hài hoà. Mỗi ổ bánh mì như một tác phẩm nhỏ, chế biến công phu và được phục vụ nóng, tạo nên mùi thơm quyến rũ khi vừa ra lò. Người bán ở đây có kinh nghiệm lâu năm, biết căn chỉnh lượng gia vị, pate và đồ chua để đạt độ cân bằng hoàn hảo giữa béo và mặn, giữa ngọt và chua. Dù phải xếp hàng, nhiều du khách vẫn kiên nhẫn chờ để nếm thử một ổ bánh mì truyền thống đậm đà. Bánh mì Huỳnh Hoa không chỉ là món ăn mà còn là trải nghiệm văn hoá, gợi nhớ phong vị Sài Gòn xưa và mới, nơi con người sum vầy quanh những quầy đồ ăn đường phố, chia sẻ câu chuyện và những khoảnh khắc giản dị.";

    private string Generate_BanhMi_HuynhHoa_EN() =>
        "Huynh Hoa Banh Mi stands as one of Saigon's most iconic street food experiences. For decades, the bakery has drawn long queues of locals and travelers eager to taste its signature sandwich: a crusty, dense baguette filled with layers of rich pate, savory cold cuts, golden butter, pickled vegetables and fragrant herbs. Each baguette is crafted with attention—balancing textures between the crisp crust and moist interior, and blending flavors that are sweet, salty and tangy in a satisfying harmony. The shop’s skillful preparation, insistence on fresh ingredients and the ritual of waiting in line have all become part of the charm. Eating a Huynh Hoa banh mi is more than a meal; it’s a cultural moment, offering a taste of Saigon’s culinary soul and the communal energy of bustling street food culture.";

    private string Generate_BanhMi_HuynhHoa_FR() =>
        "Le Banh Mi de Huynh Hoa est devenu une icône de la street food à Saïgon. Depuis des années, ce commerce attire une clientèle fidèle et des visiteurs en quête de cette baguette légendaire : une croûte croustillante, une mie dense et généreusement garnie de pâté riche, de charcuterie, de beurre doré, de légumes marinés et d'herbes odorantes. Chaque bouchée révèle une alliance de textures et de saveurs — le croquant de la croûte, la tendreté du cœur, et l'équilibre subtil entre le salé, le sucré et l'acide. La préparation minutieuse et l'attente font partie de l'expérience, transformant ce simple sandwich en un moment convivial et authentique de la culture culinaire de Saïgon.";

    private string Generate_BanhMi_HuynhHoa_ZH() =>
        "Huynh Hoa 的法棍三明治已成为西贡街头美食的代表。这里的法棍外皮酥脆、内部绵密，夹着丰富的馅料：肥美的肉酱、各种冷切肉、金黄的黄油、酸甜的腌菜和香草。每一口都能感受到松脆与柔软、咸鲜与微甜的层次感。店家的制作讲究食材的新鲜与调味的平衡，排队等待也成为一种独特的美食仪式。品尝 Huynh Hoa 的法棍，不仅仅是吃一份三明治，更是体验西贡的饮食文化与街头生活的片刻。";

    private string Generate_BanhMi_HuynhHoa_KO() =>
        "Huynh Hoa의 반미는 사이공(호찌민)의 대표적인 길거리 음식 경험 중 하나입니다. 바삭한 껍질과 촘촘한 속살의 바게트에 진한 파테, 다양한 햄, 버터, 절임 채소와 향채가 어우러져 독특한 풍미를 냅니다. 한 입에 바삭함과 촉촉함, 짭짤함과 약간의 단맛이 균형 있게 느껴집니다. 오랜 노하우로 완성된 맛과 신선한 재료는 긴 줄을 감수하고서라도 찾게 만드는 이유입니다. Huynh Hoa의 반미를 맛보는 것은 단순한 식사가 아니라 사이공의 식문화와 거리의 분위기를 체험하는 순간입니다.";

    // ... Generate functions for other locations below (Creative Park, Tao Dan Park, Lantern Street, Sa Bi Chuong)

    private string Generate_CongVienSangTao_VI() =>
        "Công viên Sáng Tạo là một điểm đến xanh mát, nơi hội tụ các không gian nghệ thuật, kiến trúc và khu vui chơi tương tác. Không gian rộng rãi với bãi cỏ, lối đi uốn lượn và các tác phẩm nghệ thuật ngoài trời tạo nên một môi trường lý tưởng để thư giãn và nảy sinh ý tưởng. Các khu vực triển lãm thường xuyên tổ chức triển lãm nhỏ, workshop sáng tạo và các hoạt động cộng đồng; trong khi đó, những khu vui chơi được thiết kế thông minh thu hút cả gia đình và giới trẻ. Một buổi dã ngoại ở đây cho phép bạn kết nối với thiên nhiên, thưởng thức nghệ thuật và khám phá những góc sáng tạo độc đáo của thành phố.";

    private string Generate_CongVienSangTao_EN() =>
        "The Creative Park is an inviting green space that brings together outdoor art, innovative architecture and interactive play areas. Broad lawns, winding paths and open-air exhibits offer an inspiring backdrop for relaxation and creative thinking. Regular pop-up exhibitions, workshops and community events make the park a lively cultural hub, while thoughtfully designed playgrounds and installations welcome families and young adventurers alike. A visit to the park provides a refreshing blend of nature and creativity, encouraging exploration and quiet moments under the trees.";

    private string Generate_CongVienSangTao_FR() =>
        "Le Parc Créatif est un espace verdoyant qui associe art en plein air, architecture innovante et aires de jeux interactives. De vastes pelouses, des sentiers sinueux et des expositions en plein air créent un décor inspirant pour se détendre et laisser libre cours à sa créativité. Des expositions éphémères, ateliers et événements communautaires font du parc un lieu culturel dynamique; les installations ludiques attirent familles et jeunes explorateurs. Une visite offre un mélange rafraîchissant de nature et d'inspiration artistique.";

    private string Generate_CongVienSangTao_ZH() =>
        "创意公园是一个融合户外艺术、创新建筑和互动游乐区的绿色空间。宽阔的草坪、曲折的小径和露天展览为放松和激发创意提供了理想环境。公园经常举办快闪展览、工作坊和社区活动，成为充满活力的文化中心；巧妙设计的游乐设施也吸引了家庭和年轻人。来到这里，你可以在树荫下静静漫步，或探索那些充满创意的角落。";

    private string Generate_CongVienSangTao_KO() =>
        "크리에이티브 파크는 야외 예술, 혁신적 건축물, 인터랙티브 놀이공간이 어우러진 푸른 공간입니다. 넓은 잔디밭과 구불구불한 산책로, 야외 전시는 휴식과 창의적 사고를 촉진합니다. 팝업 전시, 워크숍, 커뮤니티 이벤트가 자주 열려 문화적 허브 역할을 하며, 가족과 젊은 방문객을 위한 흥미로운 놀이 설치물도 마련되어 있습니다. 공원을 방문하면 자연 속에서 영감을 얻고 여유로운 시간을 보낼 수 있습니다.";

    private string Generate_CongVienTaoDan_VI() =>
        "Công viên Tao Đàn nổi tiếng với diện tích rộng và hàng cây cổ thụ rợp bóng, được xem như lá phổi xanh của thành phố. Đây là nơi người dân tới tập thể dục, thư giãn, đọc sách và giao lưu; những hoạt động cộng đồng diễn ra liên tục tạo nên nhịp sống sinh động. Đi bộ dưới tán cây, bạn sẽ bắt gặp những sân chơi, hồ nhỏ và tượng đài mang tính lịch sử, tất cả hòa quyện tạo nên một không gian yên bình giữa lòng đô thị sầm uất.";

    private string Generate_CongVienTaoDan_EN() =>
        "Tao Dan Park is celebrated for its expansive grounds and century-old trees, earning it the title of the city's green lung. Residents flock here for morning exercise, quiet reading and social gatherings; community events add to the park’s vibrant atmosphere. Strolling beneath the canopy, visitors encounter playgrounds, small ponds and monuments that reflect local history, creating a peaceful urban oasis amid the bustling city.";

    private string Generate_CongVienTaoDan_FR() =>
        "Le parc Tao Dan est reconnu pour ses vastes étendues et ses arbres centenaires, lui valant le surnom de poumon vert de la ville. Les habitants s'y retrouvent pour faire de l'exercice le matin, lire au calme ou se réunir; les événements communautaires animent le parc. En vous promenant sous la canopée, vous découvrirez des aires de jeux, des étangs et des monuments historiques, formant une oasis de tranquillité en pleine ville animée.";

    private string Generate_CongVienTaoDan_ZH() =>
        "陶丹公园以其广阔的场地和百年老树而闻名，被誉为城市的绿肺。居民常来此晨练、阅读或社交；社区活动也为公园注入了生气。漫步在树荫下，你会看到游乐场、小池塘和反映地方历史的纪念碑，构成城市中宁静的绿洲。";

    private string Generate_CongVienTaoDan_KO() =>
        "타오단 공원은 넓은 부지와 수십 년 된 나무들로 유명하며 도시의 녹색 허파로 불립니다. 주민들은 아침 운동, 독서, 교류를 위해 이곳을 방문하며 다양한 커뮤니티 행사가 공원을 활기차게 만듭니다. 나무 그늘 아래를 걷다 보면 어린이 놀이터, 작은 연못과 역사적인 기념물을 마주하게 되며, 분주한 도심 속 평화로운 오아시스를 경험하게 됩니다.";

    private string Generate_PhoLongDen_VI() =>
        "Phố Lồng Đèn là nơi hội tụ của hàng ngàn chiếc lồng đèn rực rỡ, nhất là vào dịp lễ hội Trung Thu. Con phố biến thành biển ánh sáng với đủ kiểu dáng, màu sắc và họa tiết; âm thanh náo nhiệt của người bán kẻ mua hòa cùng không khí lễ hội truyền thống. Dạo bước dưới ánh đèn, bạn có thể cảm nhận được sự ấm áp của cộng đồng, ký ức tuổi thơ và vẻ đẹp của nghệ thuật thủ công truyền thống được lưu giữ qua từng chiếc lồng.";

    private string Generate_PhoLongDen_EN() =>
        "Lantern Street comes alive with thousands of colorful lanterns, especially during the Mid-Autumn Festival. The street transforms into a river of light with varied shapes, colors and patterns, while the lively sounds of vendors and visitors blend into a festive atmosphere. Walking beneath the glow, you’ll sense the warmth of community, nostalgic childhood memories and the enduring craft of traditional lantern-making.";

    private string Generate_PhoLongDen_FR() =>
        "La Rue des Lanternes s'illumine de milliers de lanternes colorées, notamment lors de la Fête de la Mi-Automne. La rue se transforme en un flot de lumière aux formes et aux motifs variés, tandis que l'animation des commerçants et des visiteurs crée une ambiance festive. En vous promenant, vous ressentirez la chaleur de la communauté, des souvenirs d'enfance nostalgiques et l'artisanat durable de la fabrication de lanternes.";

    private string Generate_PhoLongDen_ZH() =>
        "灯笼街在中秋节期间尤为热闹，成千上万的彩灯把整条街装点得如海洋般辉煌。各种形状与图案的灯笼交相辉映，叫卖声和游客的欢笑声汇成节日的乐章。走在灯光之下，你会感受到社区的温情、童年的怀念以及传统手工艺的魅力。";

    private string Generate_PhoLongDen_KO() =>
        "등불 거리는 특히 한가위(중추절) 기간 동안 수천 개의 다채로운 등불로 생동감을 띱니다. 다양한 모양과 문양의 등불이 거리를 물들이고, 상인과 방문객의 활기찬 소리가 축제 분위기를 더합니다. 빛 아래를 걷다 보면 공동체의 따뜻함과 어린 시절의 향수를 느끼고 전통 공예의 아름다움을 경험하게 됩니다.";

    private string Generate_ComTam_SaBiChuong_VI() =>
        "Cơm tấm Sà Bì Chưởng là minh chứng cho sự đa dạng và tinh tế trong ẩm thực đường phố Sài Gòn. Một phần cơm tấm chuẩn thường bao gồm hạt cơm tơi xốp, miếng sườn nướng thơm lừng, bì dai giòn và chả trứng béo ngậy; điểm nhấn là nước mắm chua ngọt đậm đà hòa quyện từng thành phần. Thực khách thưởng thức từng thìa cơm, cảm nhận rõ sự chăm chút trong cách ướp gia vị và kỹ thuật nướng đúng điệu, khiến món ăn trở nên khó quên. Đây không chỉ là bữa ăn mà còn là trải nghiệm ẩm thực phản ánh lịch sử và văn hóa địa phương.";

    private string Generate_ComTam_SaBiChuong_EN() =>
        "Sa Bi Chuong broken rice is a testament to Saigon’s rich street food tradition. A classic plate combines fluffy broken rice grains, a charcoal-grilled pork chop with slightly charred edges, chewy shredded pork skin, and a rich steamed pork-and-egg loaf. The crowning element is the sweet, tangy fish sauce dressing that ties every component together. Each bite showcases the craftsmanship behind seasoning and grilling techniques; the dish reflects both the practicality and the soulful flavors of Vietnamese home-style cooking.";

    private string Generate_ComTam_SaBiChuong_FR() =>
        "Le Com Tam Sa Bi Chuong illustre la richesse de la street food à Saïgon. Un plat classique associe du riz cassé moelleux, une côtelette de porc grillée au charbon légèrement caramélisée, de la couenne de porc croustillante et un pâté vapeur à l'œuf. La sauce de poisson sucrée et acidulée vient lier l'ensemble. Chaque bouchée révèle le savoir-faire en matière d'assaisonnement et de grillade, faisant de ce plat une expérience qui reflète la cuisine traditionnelle vietnamienne.";

    private string Generate_ComTam_SaBiChuong_ZH() =>
        "Sa Bi Chuong 的破碎米饭代表了西贡丰富的街头美食传统。经典的一盘由松软的碎米、炭火烤制的带焦香的猪排、有嚼劲的猪皮丝以及丰富的蒸肉蛋卷组成。点睛之笔是那碗酸甜适度的鱼露，将各种味道连接在一起。每一口都体现了调味与烤制技巧，是充满人情味的越南家常风味。";

    private string Generate_ComTam_SaBiChuong_KO() =>
        "사 비 쭝의 컴탐(부서진 쌀) 요리는 사이공의 풍부한 길거리 음식 전통을 보여줍니다. 전형적인 한 접시는 포슬포슬한 밥, 숯불에 구워 살짝 그을린 돼지고기, 쫄깃한 돼지 껍데기, 그리고 고소한 계란 고기찜으로 구성됩니다. 전체를 아우르는 것은 달콤하고 새콤한 피시소스 소스이며, 각 재료의 조화는 정교한 양념과 그릴링 기술을 반영합니다.";


    // Lấy tất cả kịch bản (content) theo LocationId, trả về map langCode -> content
    public async Task<Dictionary<string, string>> GetScriptsForLocation(int locationId)
    {
        await Init();
        var result = new Dictionary<string, string>();

        var scripts = await _db.Table<Script>().Where(s => s.LocationId == locationId).ToListAsync();
        foreach (var s in scripts)
        {
            var lang = await _db.Table<Language>().Where(l => l.Id == s.LanguageId).FirstOrDefaultAsync();
            if (lang != null && !string.IsNullOrEmpty(lang.LangCode))
            {
                // store content (prefer Content, fallback Title)
                result[lang.LangCode] = string.IsNullOrWhiteSpace(s.Content) ? s.Title ?? string.Empty : s.Content;
            }
        }

        return result;
    }

    private async Task SeedData()
    {
        // Nếu DB đã có dữ liệu rồi thì bỏ qua không nạp trùng
        if (await _db.Table<TourLocation>().CountAsync() > 0) return;

        // 1. NẠP NGÔN NGỮ (5 ngôn ngữ)
        var vi = new Language { LangCode = "vi", LangName = "Tiếng Việt" };
        var en = new Language { LangCode = "en", LangName = "English" };
        var fr = new Language { LangCode = "fr", LangName = "Français" };
        var zh = new Language { LangCode = "zh", LangName = "中文" };
        var ko = new Language { LangCode = "ko", LangName = "한국어" };
        await _db.InsertAsync(vi);
        await _db.InsertAsync(en);
        await _db.InsertAsync(fr);
        await _db.InsertAsync(zh);
        await _db.InsertAsync(ko);

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

            // 3. NẠP KỊCH BẢN THUYẾT MINH CHI TIẾT (5 ngôn ngữ) - Each ~300 words (summary inserted)
            // For each location create script entries for vi, en, fr, zh, ko
            // Bánh mì Huỳnh Hoa
            if (loc.LocationName == "Bánh mì Huỳnh Hoa")
            {
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = vi.Id, Title = "Bánh mì Huỳnh Hoa - Hương vị truyền thống", Content = Generate_BanhMi_HuynhHoa_VI() });
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = en.Id, Title = "Huynh Hoa Banh Mi - Traditional Flavor", Content = Generate_BanhMi_HuynhHoa_EN() });
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = fr.Id, Title = "Banh Mi Huynh Hoa - Saveur traditionnelle", Content = Generate_BanhMi_HuynhHoa_FR() });
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = zh.Id, Title = "Banh Mi Huynh Hoa - 传统风味", Content = Generate_BanhMi_HuynhHoa_ZH() });
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = ko.Id, Title = "Banh Mi Huynh Hoa - 전통의 맛", Content = Generate_BanhMi_HuynhHoa_KO() });
            }
            else if (loc.LocationName == "Công viên Sáng Tạo")
            {
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = vi.Id, Title = "Công viên Sáng Tạo - Không gian sáng tạo", Content = Generate_CongVienSangTao_VI() });
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = en.Id, Title = "Creative Park - An Inspiring Space", Content = Generate_CongVienSangTao_EN() });
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = fr.Id, Title = "Parc Créatif - Un espace inspirant", Content = Generate_CongVienSangTao_FR() });
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = zh.Id, Title = "创意公园 - 激发灵感的空间", Content = Generate_CongVienSangTao_ZH() });
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = ko.Id, Title = "크리에이티브 파크 - 영감을 주는 공간", Content = Generate_CongVienSangTao_KO() });
            }
            else if (loc.LocationName == "Công viên Tao Đàn")
            {
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = vi.Id, Title = "Công viên Tao Đàn - Lá phổi xanh", Content = Generate_CongVienTaoDan_VI() });
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = en.Id, Title = "Tao Dan Park - The Green Lung", Content = Generate_CongVienTaoDan_EN() });
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = fr.Id, Title = "Parc Tao Dan - Le poumon vert", Content = Generate_CongVienTaoDan_FR() });
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = zh.Id, Title = "陶丹公园 - 绿肺", Content = Generate_CongVienTaoDan_ZH() });
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = ko.Id, Title = "타오단 공원 - 녹색 허파", Content = Generate_CongVienTaoDan_KO() });
            }
            else if (loc.LocationName == "Phố Lồng đèn")
            {
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = vi.Id, Title = "Phố Lồng Đèn - Lễ hội ánh sáng", Content = Generate_PhoLongDen_VI() });
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = en.Id, Title = "Lantern Street - Festival of Light", Content = Generate_PhoLongDen_EN() });
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = fr.Id, Title = "Rue des lanternes - Festival de lumière", Content = Generate_PhoLongDen_FR() });
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = zh.Id, Title = "灯笼街 - 光之节", Content = Generate_PhoLongDen_ZH() });
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = ko.Id, Title = "등불 거리 - 빛의 축제", Content = Generate_PhoLongDen_KO() });
            }
            else if (loc.LocationName == "Cơm tấm Sà Bì Chưởng")
            {
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = vi.Id, Title = "Cơm Tấm Sà Bì Chưởng - Hương vị Sài Gòn", Content = Generate_ComTam_SaBiChuong_VI() });
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = en.Id, Title = "Sa Bi Chuong Broken Rice - Saigon Flavor", Content = Generate_ComTam_SaBiChuong_EN() });
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = fr.Id, Title = "Com Tam Sa Bi Chuong - Saveur de Saïgon", Content = Generate_ComTam_SaBiChuong_FR() });
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = zh.Id, Title = "Sa Bi Chuong 破碎米 - 西贡风味", Content = Generate_ComTam_SaBiChuong_ZH() });
                await _db.InsertAsync(new Script { LocationId = loc.Id, LanguageId = ko.Id, Title = "사 비 쭝 부서진 쌀 - 사이공의 맛", Content = Generate_ComTam_SaBiChuong_KO() });
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

    // Upsert a list of TourLocation from remote source. Returns number of newly added items.
    public async Task<int> UpsertTourLocations(List<TourLocation> remoteList)
    {
        await Init();
        int added = 0;

        foreach (var r in remoteList)
        {
            // Try to find existing by Name (or very close lat/lon)
            var existing = await _db.Table<TourLocation>()
                .Where(t => t.LocationName == r.LocationName)
                .FirstOrDefaultAsync();

            if (existing == null)
            {
                await _db.InsertAsync(r);
                added++;
            }
            else
            {
                // Update mutable fields if changed
                bool dirty = false;
                if (existing.ImageUrl != r.ImageUrl) { existing.ImageUrl = r.ImageUrl; dirty = true; }
                if (existing.Latitude != r.Latitude) { existing.Latitude = r.Latitude; dirty = true; }
                if (existing.Longitude != r.Longitude) { existing.Longitude = r.Longitude; dirty = true; }
                if (existing.Category != r.Category) { existing.Category = r.Category; dirty = true; }
                if (dirty) await _db.UpdateAsync(existing);
            }
        }

        return added;
    }
}