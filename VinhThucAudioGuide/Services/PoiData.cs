using System.Collections.Generic;

namespace VinhThucAudioGuide.Services;

public static class PoiData
{
    public static List<(string Name, string Cat, string Img, double Lat, double Lon)> GetNewLocations()
    {
        return new List<(string, string, string, double, double)>
        {
            // 5 ĐỊA ĐIỂM CŨ - Giữ nguyên tên file ảnh local để đảm bảo load 100%
            ("Bánh mì Huỳnh Hoa", "Thức ăn", "banhmihuynhhoa.jpg", 10.7716, 106.6924),
            ("Công viên Sáng Tạo", "Vui chơi", "congviensangtao.jpg", 10.7801, 106.7126),
            ("Công viên Tao Đàn", "Vui chơi", "congvientaodan.jpg", 10.7756, 106.6921),
            ("Phố Lồng đèn", "Lễ hội", "pholongden.jpg", 10.7534, 106.6600),
            ("Cơm tấm Sà Bì Chưởng", "Thức ăn", "comtamsabichuong.jpg", 10.7586, 106.6804),

            // 14 ĐỊA ĐIỂM MỚI - Tên file ảnh local (sẽ được tải tự động về máy nếu chưa có)
            ("Chợ Bến Thành", "Thức ăn", "chobenthanh.jpg", 10.7726, 106.6981),
            ("Dinh Độc Lập", "Vui chơi", "dinhdoclap.jpg", 10.7769, 106.6955),
            ("Nhà thờ Đức Bà", "Vui chơi", "nhathoducba.jpg", 10.7798, 106.6991),
            ("Địa đạo Củ Chi", "Vui chơi", "diadaocuchi.jpg", 11.0548, 106.4933),
            ("Phố đi bộ Nguyễn Huệ", "Vui chơi", "pdbnguyenhue.jpg", 10.7735, 106.7032),
            ("Thảo Cầm Viên", "Vui chơi", "thaocamvien.jpg", 10.7874, 106.7057),
            ("Tòa nhà Bitexco", "Vui chơi", "bitexco.jpg", 10.7720, 106.7046),
            ("Công viên nước Đầm Sen", "Vui chơi", "cvndamsen.jpg", 10.7675, 106.6352),
            ("Hủ tiếu mực Ông Già Cali", "Thức ăn", "htmonggiacali.jpg", 10.7762, 106.7008),
            ("Bánh mì chảo Hòa Mã", "Thức ăn", "bmchoama.jpg", 10.7809, 106.6893),
            ("Bún riêu Yến", "Thức ăn", "bunrieuyen.jpg", 10.7623, 106.6673),
            ("Cơm tấm Ba Ghiền", "Thức ăn", "comtambaghien.jpg", 10.7980, 106.6852),
            ("Lễ giỗ Đức Thánh Trần Hưng Đạo", "Lễ hội", "legiothd.jpg", 10.7834, 106.6965),
            ("Lễ giỗ tổ nghề Kim Hoàn", "Lễ hội", "legiokh.jpg", 10.7756, 106.7000)
        };
    }

    public static Dictionary<string, (string vi, string en, string fr, string zh, string ko)> GetScripts()
    {
        return new Dictionary<string, (string, string, string, string, string)>
        {
            // KỊCH BẢN 5 ĐỊA ĐIỂM CŨ
            ["Bánh mì Huỳnh Hoa"] = (
                "Bánh mì Huỳnh Hoa là biểu tượng ẩm thực đường phố nổi bật của Sài Gòn. Vỏ bánh giòn rụm, nhân phong phú gồm pate, chả, thịt nguội tạo nên một trải nghiệm văn hoá, gợi nhớ phong vị Sài Gòn.",
                "Huynh Hoa Banh Mi stands as one of Saigon's iconic street food experiences. A crusty baguette filled with rich pate and savory cold cuts offers a taste of Saigon's culinary soul.",
                "Le Banh Mi de Huynh Hoa est une icône de la street food à Saïgon. Une baguette croustillante généreusement garnie de pâté et de charcuterie.",
                "Huynh Hoa法棍三明治是西贡街头美食的代表。酥脆的面包夹着肉酱和冷切肉，体现了西贡的饮食文化。",
                "Huynh Hoa 반미는 사이공의 대표적인 길거리 음식입니다. 바삭한 바게트와 진한 파테, 햄이 어우러져 독특한 풍미를 냅니다."
            ),
            ["Công viên Sáng Tạo"] = (
                "Công viên Sáng Tạo là điểm đến xanh mát hội tụ nghệ thuật và kiến trúc. Các khu vực triển lãm, workshop sáng tạo thu hút gia đình và giới trẻ thư giãn giữa thiên nhiên.",
                "The Creative Park brings together outdoor art and interactive play areas. Open-air exhibits offer an inspiring backdrop for relaxation and creative thinking.",
                "Le Parc Créatif associe art en plein air et aires de jeux interactives. Il offre un décor inspirant pour se détendre.",
                "创意公园是一个融合户外艺术和互动游乐区的绿色空间。露天展览为放松提供了理想环境。",
                "크리에이티브 파크는 야외 예술과 놀이공간이 어우러진 푸른 공간입니다. 휴식과 창의적 사고를 촉진합니다."
            ),
            ["Công viên Tao Đàn"] = (
                "Công viên Tao Đàn được xem như lá phổi xanh của thành phố. Người dân tới tập thể dục, thư giãn, tạo nên một không gian yên bình giữa lòng đô thị sầm uất.",
                "Tao Dan Park is celebrated as the city's green lung. Residents flock here for morning exercise, creating a peaceful urban oasis.",
                "Le parc Tao Dan est le poumon vert de la ville. Les habitants s'y retrouvent pour faire de l'exercice au calme.",
                "陶丹公园被誉为城市的绿肺。居民常来此晨练，构成城市中宁静的绿洲。",
                "타오단 공원은 도시의 녹색 허파로 불립니다. 주민들은 아침 운동을 위해 이곳을 방문합니다."
            ),
            ["Phố Lồng đèn"] = (
                "Phố Lồng Đèn hội tụ hàng ngàn chiếc lồng đèn rực rỡ dịp Trung Thu. Dạo bước dưới ánh đèn, bạn cảm nhận được vẻ đẹp của nghệ thuật thủ công truyền thống.",
                "Lantern Street comes alive with colorful lanterns during Mid-Autumn Festival. Walking beneath the glow, you sense the enduring craft of lantern-making.",
                "La Rue des Lanternes s'illumine de lanternes colorées lors de la Fête de la Mi-Automne. Vous ressentirez l'artisanat de la fabrication de lanternes.",
                "灯笼街在中秋节期间极为热闹。走在灯光下，你会感受到传统手工艺的魅力。",
                "등불 거리는 한가위 기간 동안 다채로운 등불로 생동감을 띱니다. 전통 공예의 아름다움을 경험하게 됩니다."
            ),
            ["Cơm tấm Sà Bì Chưởng"] = (
                "Cơm tấm Sà Bì Chưởng là minh chứng cho sự tinh tế trong ẩm thực Sài Gòn. Sườn nướng thơm lừng, bì dai giòn hòa quyện nước mắm đậm đà khiến món ăn khó quên.",
                "Sa Bi Chuong broken rice is a testament to Saigon's rich street food tradition. Charcoal-grilled pork chop and chewy shredded pork skin create a soulful flavor.",
                "Le Com Tam Sa Bi Chuong illustre la richesse de la street food à Saïgon. Côtelette de porc grillée et couenne de porc créent une expérience inoubliable.",
                "Sa Bi Chuong 破碎米饭代表了西贡的美食传统。炭火烤猪排和猪皮丝让你回味无穷。",
                "사 비 쭝의 컴탐 요리는 사이공의 길거리 음식 전통을 보여줍니다. 숯불 돼지고기와 쫄깃한 껍데기가 일품입니다."
            ),

            // KỊCH BẢN 14 ĐỊA ĐIỂM MỚI
            ["Chợ Bến Thành"] = (
                "Chợ Bến Thành là biểu tượng lâu đời của Sài Gòn, nơi hội tụ hàng ngàn gian hàng ẩm thực và thủ công mỹ nghệ. Thưởng thức đồ ăn ở đây mang lại cảm giác đậm đà bản sắc địa phương.",
                "Ben Thanh Market is an enduring symbol of Saigon, gathering thousands of stalls. Eating here provides a bustling, authentic local experience.",
                "Le marché Ben Thanh est un symbole durable de Saigon. Manger ici offre une expérience locale authentique et animée.",
                "滨城市场是西贡的悠久象征，聚集了成千上万的摊位。在这里用餐可以体验到地道的当地风情。",
                "벤탄 시장은 사이공의 오랜 상징입니다. 이곳에서 식사하면 활기차고 진정한 현지의 맛을 느낄 수 있습니다."
            ),
            ["Dinh Độc Lập"] = (
                "Dinh Độc Lập là di tích lịch sử quan trọng, chứng kiến nhiều thăng trầm của lịch sử. Đây là nơi bạn có thể tìm hiểu về lịch sử và tận hưởng không gian yên bình.",
                "Independence Palace is an important historical monument. It's a place to learn about history while enjoying a peaceful oasis.",
                "Le Palais de l'Indépendance est un monument historique important. Il offre une expérience fascinante pour découvrir l'histoire.",
                "独立宫是重要的历史遗迹。在这里您可以了解历史，同时享受宁静的绿洲。",
                "독립궁은 중요한 역사적 기념물입니다. 평화로운 오아시스를 즐기며 역사를 배울 수 있는 곳입니다."
            ),
            ["Nhà thờ Đức Bà"] = (
                "Nhà thờ Đức Bà Sài Gòn mang đậm dấu ấn kiến trúc Pháp cổ kính. Hai tháp chuông cao vút và gạch ngói đỏ đặc trưng là điểm check-in không thể bỏ lỡ.",
                "Notre Dame Cathedral of Saigon features classic French architecture. Its towering twin bell towers and red bricks are a must-visit landmark.",
                "La basilique Notre-Dame de Saïgon présente une architecture française classique. Ses briques rouges distinctives sont un monument incontournable.",
                "西贡圣母大教堂拥有经典的法国建筑风格。其高耸的双钟楼是不可错过的地标。",
                "사이공 노트르담 대성당은 고전적인 프랑스 건축 양식을 자랑합니다. 붉은 벽돌이 특징인 필수 방문 명소입니다."
            ),
            ["Địa đạo Củ Chi"] = (
                "Địa đạo Củ Chi là một hệ thống đường hầm dưới lòng đất, minh chứng cho sự kiên cường của người dân. Du khách sẽ được trải nghiệm chui hầm và thưởng thức khoai mì luộc.",
                "The Cu Chi Tunnels are an extensive underground network showcasing the resilience of the people. Visitors can crawl through tunnels and taste boiled tapioca.",
                "Les tunnels de Cu Chi sont un vaste réseau souterrain illustrant la résilience du peuple. Les visiteurs peuvent ramper dans les tunnels.",
                "古芝地道是一个庞大的地下网络，展示了人民的韧性。游客可以体验在隧道中爬行。",
                "구찌 터널은 베트남 사람들의 회복력을 보여주는 광범위한 지하 네트워크입니다. 터널을 기어 다닐 수 있습니다."
            ),
            ["Phố đi bộ Nguyễn Huệ"] = (
                "Phố đi bộ Nguyễn Huệ là trung tâm vui chơi giải trí sầm uất. Đây là nơi diễn ra các sự kiện âm nhạc và nghệ thuật đường phố náo nhiệt.",
                "Nguyen Hue Walking Street is a bustling entertainment hub. It frequently hosts vibrant music events and street art performances.",
                "La rue piétonne Nguyen Hue est un centre de divertissement animé. Elle accueille des événements musicaux et des arts de rue.",
                "阮惠步行街是繁华的娱乐中心。这里经常举办音乐活动和街头艺术表演。",
                "응우옌 후에 보행자 거리는 활기찬 엔터테인먼트 중심지입니다. 음악 행사와 거리 예술 공연이 열립니다."
            ),
            ["Thảo Cầm Viên"] = (
                "Thảo Cầm Viên là vườn thú lâu đời bảo tồn ngàn loài động thực vật. Không gian xanh giúp gia đình thư giãn, hòa mình vào thiên nhiên.",
                "The Botanical Garden preserves thousands of flora and fauna species. Its lush space offers a relaxing getaway in nature.",
                "Le jardin botanique préserve des milliers d'espèces. Son espace luxuriant offre une escapade relaxante en pleine nature.",
                "草禽园保护着数千种动植物。其郁郁葱葱的空间为家庭提供了放松的度假胜地。",
                "동식물원은 수천 종의 동식물을 보호합니다. 도심 한가운데서 자연에 몰입하며 편안한 휴식을 제공합니다."
            ),
            ["Tòa nhà Bitexco"] = (
                "Tòa nhà Bitexco mang hình dáng búp sen là niềm tự hào kiến trúc hiện đại. Tại đài quan sát Saigon Skydeck, bạn ngắm toàn cảnh thành phố lung linh.",
                "Bitexco Financial Tower, shaped like a lotus bud, is a pride of modern architecture. From the Skydeck, you enjoy a panoramic view.",
                "La tour Bitexco, en forme de bouton de lotus, est une fierté. Depuis le Skydeck, vous profitez d'une vue panoramique.",
                "金融塔形状犹如向天空伸展的莲花苞。在观景台上，您可以全景俯瞰城市。",
                "비텍스코 타워는 현대 건축의 자랑입니다. 스카이덱에서는 도시 전경을 감상할 수 있습니다."
            ),
            ["Công viên nước Đầm Sen"] = (
                "Công viên nước Đầm Sen mang lại giây phút giải trí tuyệt vời với các trò chơi dưới nước. Đây là điểm lý tưởng để giải nhiệt mùa hè.",
                "Dam Sen Water Park offers fantastic entertainment with water rides. It’s an ideal destination to cool off and have fun.",
                "Le parc aquatique Dam Sen offre d'excellents divertissements. C'est l'endroit idéal pour se rafraîchir en famille.",
                "莲水上乐园提供各种刺激的水上游乐设施。这是与家人一起消暑的理想之地。",
                "담센 워터파크는 환상적인 물놀이 시설을 제공합니다. 가족과 함께 더위를 식히기에 이상적인 곳입니다."
            ),
            ["Hủ tiếu mực Ông Già Cali"] = (
                "Hủ tiếu mực Ông Già Cali nổi tiếng với hương vị hải sản. Mực giòn ngọt cùng nước lèo đậm đà tạo nên món ăn khó quên.",
                "Ông Già Cali Squid Noodle is famous for its fresh seafood flavor. Crunchy squid in a rich broth creates an irresistible dish.",
                "La soupe de nouilles aux calmars est célèbre pour sa saveur. Des calmars croquants créent un plat irrésistible.",
                "Ông Già Cali 墨鱼粿条以海鲜风味闻名。爽脆的墨鱼搭配浓郁的汤头让人回味无穷。",
                "Ông Già Cali 오징어 쌀국수는 신선한 해산물 맛으로 유명합니다. 맑은 국물이 일품입니다."
            ),
            ["Bánh mì chảo Hòa Mã"] = (
                "Bánh mì chảo Hòa Mã là một phần ký ức ẩm thực. Chảo gang phục vụ trứng, pate, xúc xích luôn làm nức lòng thực khách.",
                "Hoa Ma Pan Bread is a part of culinary memory. A hot cast-iron pan serving eggs and pate delights diners.",
                "Le pain à la poêle Hoa Ma est une mémoire culinaire. Une poêle chaude servant des œufs et du pâté ravit toujours.",
                "Hoa Ma 铁板面包是美食记忆。热腾腾的铁板上煎着鸡蛋和肉酱，让人大快朵颐。",
                "호아마 철판 반미는 미식 기억입니다. 뜨거운 철판에 계란과 파테가 제공됩니다."
            ),
            ["Bún riêu Yến"] = (
                "Bún riêu Yến hút khách bởi tô bún đầy, nước dùng thanh ngọt vị cua. Riêu cua béo ngậy tạo nên hương vị cuốn hút.",
                "Bun Rieu Yen attracts diners with its naturally sweet crab broth. The rich crab paste creates a captivating flavor.",
                "Bun Rieu Yen attire les convives avec un bouillon de crabe doux. La pâte de crabe crée une saveur captivante.",
                "Yen 蟹林米线以鲜甜的螃蟹汤底吸引食客。浓郁的蟹膏营造出极具吸引力的风味。",
                "옌 분리우는 게 육수로 식객을 사로잡습니다. 진한 게 페이스트가 매력적인 맛을 냅니다."
            ),
            ["Cơm tấm Ba Ghiền"] = (
                "Cơm tấm Ba Ghiền nổi tiếng với sườn nướng khổng lồ, thơm phức. Đĩa cơm kết hợp bì, chả mang đến bữa ăn no nê thỏa mãn.",
                "Ba Ghien Broken Rice is famous for its giant grilled pork chop. The hot broken rice provides a deeply satisfying meal.",
                "Le riz cassé Ba Ghien est célèbre pour sa côtelette de porc grillée géante. C'est un repas copieux et satisfaisant.",
                "Ba Ghien 碎米饭以巨大的烤猪排而闻名。搭配肉丝提供了一顿丰盛的餐点。",
                "바기엔 컴탐은 거대한 숯불 돼지갈비로 유명합니다. 매우 푸짐하고 만족스럽습니다."
            ),
            ["Lễ giỗ Đức Thánh Trần Hưng Đạo"] = (
                "Lễ giỗ Đức Thánh Trần Hưng Đạo là sự kiện tâm linh tưởng nhớ vị anh hùng. Lễ hội diễn ra trang nghiêm, gắn kết cộng đồng.",
                "The Commemoration of Saint Tran Hung Dao honors the national hero. The festival features traditional rituals uniting the community.",
                "La commémoration de Saint Tran Hung Dao honore le héros national. Le festival unit la communauté.",
                "陈兴道圣人忌日是纪念伟大民族英雄的活动。节日庄严的仪式深深团结了社区。",
                "쩐흥다오 성인 추모제는 위대한 민족 영웅을 기립니다. 전통 제례가 열립니다."
            ),
            ["Lễ giỗ tổ nghề Kim Hoàn"] = (
                "Lễ giỗ tổ nghề Kim Hoàn tôn vinh nghệ nhân kim hoàn. Lễ hội quy tụ đông đảo người làm nghề dâng hương và trưng bày tác phẩm.",
                "The Goldsmith Founders' Commemoration celebrates jewelry artisans. Craftsmen gather to honor founders and showcase exquisite pieces.",
                "La commémoration des fondateurs orfèvres célèbre les artisans. Les orfèvres se rassemblent pour honorer les fondateurs.",
                "金银匠祖师爷忌日庆祝才华横溢的珠宝工匠。工匠聚集展示精美的珠宝首饰。",
                "금세공 창시자 추모제는 보석 장인들을 기립니다. 장인들이 모여 창시자를 기립니다."
            )
        };
    }
}
