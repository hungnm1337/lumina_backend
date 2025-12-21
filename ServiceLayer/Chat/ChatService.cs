using DataLayer.DTOs.Chat;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Net.Http;
using ServiceLayer.UploadFile;
using DataLayer.DTOs;
using System.Net.Http.Headers;

namespace ServiceLayer.Chat
{
    public class ChatService : IChatService
    {
        private readonly LuminaSystemContext _context;
        private readonly OpenAIOptions _openAIOptions;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUploadService _uploadService;

        // Valid TOEIC topics for vocabulary generation (600+ keywords)
        private static readonly HashSet<string> ValidTOEICTopics = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // ===== BUSINESS & WORKPLACE ===== (~80 keywords)
            "business", "work", "office", "workplace", "corporate", "company", "corporation", "firm", "enterprise",
            "meeting", "conference", "presentation", "proposal", "contract", "agreement", "deal", "negotiation",
            "management", "manager", "administration", "executive", "director", "supervisor", "leader", "leadership",
            "employee", "staff", "personnel", "worker", "team", "colleague", "coworker", "partner",
            "human resources", "hr", "recruitment", "hiring", "employment", "job", "career", "position", "vacancy",
            "sales", "selling", "marketing", "advertising", "promotion", "campaign", "brand", "customer service",
            "client", "customer", "consumer", "supplier", "vendor", "distributor", "retailer", "wholesaler",
            "manufacturing", "production", "factory", "warehouse", "facility", "plant", "assembly", "processing",
            "supply chain", "logistics", "shipping", "delivery", "distribution", "inventory", "stock",
            "merger", "acquisition", "partnership", "collaboration", "joint venture", "alliance",
            "project", "planning", "strategy", "goal", "objective", "target", "mission", "vision",
            "performance", "productivity", "efficiency", "quality", "improvement", "development",
            
            // ===== FINANCE & ECONOMICS ===== (~70 keywords)
            "finance", "financial", "banking", "bank", "money", "economy", "economic", "fiscal", "monetary",
            "accounting", "accountant", "bookkeeping", "budget", "expense", "revenue", "profit", "loss", "income",
            "investment", "investor", "investing", "stock", "share", "equity", "bond", "security", "asset",
            "dividend", "portfolio", "capital", "funding", "financing", "loan", "credit", "debt", "liability",
            "tax", "taxation", "deduction", "refund", "audit", "auditor",
            "insurance", "policy", "premium", "claim", "coverage", "risk",
            "payment", "transaction", "purchase", "sale", "invoice", "receipt", "billing",
            "currency", "exchange", "exchange rate", "foreign exchange", "forex",
            "inflation", "deflation", "interest", "interest rate", "rate", "price", "cost", "value", "worth",
            "savings", "deposit", "withdrawal", "balance", "account",
            
            // ===== TRAVEL & TRANSPORTATION ===== (~90 keywords)
            "travel", "traveling", "travelling", "tourism", "tourist", "vacation", "holiday", "trip", "journey", "tour",
            "hotel", "motel", "inn", "lodge", "resort", "accommodation", "lodging", "guest house",
            "reservation", "booking", "check-in", "check-out", "checkout", "checkin", "confirm", "confirmation",
            "room", "suite", "single", "double", "deluxe", "standard", "amenity", "service",
            "transportation", "transport", "vehicle", "car", "automobile", "auto", "bus", "coach", "train", "railway",
            "subway", "metro", "tram", "taxi", "cab", "ride", "driving", "driver",
            "flight", "fly", "flying", "airline", "airways", "airport", "terminal", "gate", "boarding",
            "plane", "aircraft", "jet", "departure", "arrival", "takeoff", "landing", "layover", "stopover",
            "ship", "cruise", "ferry", "boat", "vessel", "sailing", "port", "harbor", "dock", "pier",
            "ticket", "pass", "fare", "price", "cost", "fee", "charge",
            "schedule", "timetable", "itinerary", "route", "destination", "location", "place", "site",
            "luggage", "baggage", "suitcase", "carry-on", "checked", "lost and found",
            "passport", "visa", "customs", "immigration", "border", "security",
            
            // ===== TECHNOLOGY & INTERNET ===== (~80 keywords)
            "technology", "tech", "technological", "innovation", "digital", "electronic", "electronics",
            "computer", "pc", "laptop", "desktop", "tablet", "smartphone", "phone", "mobile", "device",
            "internet", "online", "web", "website", "webpage", "site", "portal", "platform",
            "e-commerce", "ecommerce", "online shopping", "digital commerce",
            "software", "application", "app", "program", "tool", "utility", "system", "operating system",
            "database", "data", "information", "file", "document", "storage", "backup",
            "network", "networking", "wifi", "wireless", "connection", "connectivity", "bandwidth",
            "server", "host", "hosting", "cloud", "cloud computing", "virtual", "remote",
            "email", "e-mail", "message", "messaging", "chat", "instant message", "text",
            "communication", "videoconference", "video conference", "teleconference", "webinar", "zoom", "meeting",
            "password", "username", "login", "logout", "account", "profile", "security", "encryption",
            "update", "upgrade", "download", "upload", "install", "uninstall", "setup",
            
            // ===== EDUCATION & LEARNING ===== (~85 keywords)
            "education", "educational", "learning", "studying", "study", "academic", "academia",
            "school", "elementary", "primary", "secondary", "high school", "university", "college", "institute", "academy",
            "student", "pupil", "learner", "undergraduate", "graduate", "postgraduate", "scholar",
            "teacher", "instructor", "professor", "lecturer", "educator", "tutor", "mentor", "coach",
            "course", "program", "curriculum", "syllabus", "class", "classroom", "lesson", "lecture", "tutorial",
            "seminar", "workshop", "training", "session", "module", "subject", "topic",
            "exam", "examination", "test", "quiz", "assessment", "evaluation", "midterm", "final",
            "grade", "score", "mark", "result", "performance", "achievement",
            "certificate", "certification", "diploma", "degree", "bachelor", "master", "doctorate", "phd",
            "assignment", "homework", "project", "essay", "thesis", "dissertation", "research", "paper",
            "textbook", "book", "reference", "material", "resource", "library", "lab", "laboratory",
            "scholarship", "grant", "tuition", "fee", "enrollment", "registration", "admission",
            
            // ===== HEALTHCARE & MEDICINE ===== (~70 keywords)
            "healthcare", "health", "healthy", "wellness", "wellbeing", "fitness",
            "medical", "medicine", "clinical", "therapeutic", "treatment", "care",
            "hospital", "clinic", "infirmary", "medical center", "health center",
            "pharmacy", "drugstore", "dispensary", "pharmacist",
            "doctor", "physician", "surgeon", "specialist", "practitioner", "gp", "general practitioner",
            "nurse", "nursing", "caregiver", "paramedic", "medic",
            "patient", "outpatient", "inpatient", "case", "medical record",
            "treatment", "therapy", "rehabilitation", "recovery", "healing",
            "consultation", "visit", "checkup", "check-up", "examination", "exam", "diagnosis", "prognosis",
            "prescription", "medication", "medicine", "drug", "pill", "tablet", "dose", "dosage",
            "vaccine", "vaccination", "immunization", "shot", "injection",
            "surgery", "operation", "procedure", "surgical",
            "appointment", "scheduling", "booking", "emergency", "urgent", "ambulance", "first aid",
            "symptom", "condition", "disease", "illness", "sick", "pain", "injury",
            
            // ===== ENVIRONMENT & NATURE ===== (~65 keywords)
            "environment", "environmental", "nature", "natural", "eco", "ecology", "ecological",
            "green", "sustainability", "sustainable", "renewable", "conservation", "preserve", "protection",
            "pollution", "pollutant", "contamination", "emission", "waste", "disposal", "cleanup",
            "recycling", "recycle", "reuse", "reduce", "compost",
            "climate", "climate change", "global warming", "greenhouse", "carbon", "emission",
            "weather", "temperature", "rain", "wind", "sun", "solar",
            "energy", "power", "electricity", "renewable energy", "solar energy", "wind energy",
            "forest", "tree", "plant", "vegetation", "wildlife", "habitat",
            "ocean", "sea", "marine", "water", "river", "lake", "aquatic",
            "animal", "species", "biodiversity", "ecosystem", "organism",
            "resource", "natural resource", "mineral", "oil", "gas", "fuel",
            
            // ===== DAILY LIFE & ROUTINE ===== (~75 keywords)
            "daily life", "everyday", "daily", "routine", "lifestyle", "living", "life",
            "household", "domestic", "home", "house", "apartment", "residence", "dwelling", "flat",
            "furniture", "furnishing", "appliance", "equipment", "tool", "utensil",
            "cleaning", "housework", "chore", "maintenance", "repair", "fix", "renovation",
            "family", "parent", "child", "children", "sibling", "relative", "relation",
            "friend", "friendship", "neighbor", "neighbourhood", "community", "society", "social",
            "personal", "private", "individual", "habit", "behavior", "activity",
            "morning", "afternoon", "evening", "night", "weekend", "weekday",
            "wake up", "sleep", "rest", "relax", "leisure time", "free time",
            "shopping", "grocery", "groceries", "supermarket", "store", "shop", "retail", "market", "mall",
            "clothing", "clothes", "garment", "dress", "shirt", "pants", "shoe", "shoes",
            "fashion", "style", "trend", "designer", "accessory", "jewelry",
            
            // ===== FOOD & DINING ===== (~70 keywords)
            "food", "foods", "eating", "dining", "meal", "dish", "cuisine",
            "restaurant", "cafe", "cafeteria", "canteen", "diner", "bistro", "eatery", "dining room",
            "breakfast", "lunch", "dinner", "brunch", "supper", "snack", "appetizer", "dessert",
            "menu", "order", "ordering", "serve", "service", "waiter", "waitress", "server",
            "recipe", "cooking", "culinary", "preparation", "ingredient", "seasoning", "spice",
            "chef", "cook", "kitchen", "cooking", "baking", "frying", "boiling", "grilling",
            "nutrition", "nutritious", "healthy", "diet", "dietary", "calorie", "protein", "vitamin",
            "organic", "fresh", "local", "seasonal", "natural", "homemade",
            "beverage", "drink", "water", "juice", "coffee", "tea", "soda",
            "taste", "flavor", "delicious", "tasty", "sweet", "salty", "spicy", "sour",
            
            // ===== ENTERTAINMENT & LEISURE ===== (~80 keywords)
            "entertainment", "leisure", "hobby", "pastime", "recreation", "recreational", "fun", "enjoyment",
            "movie", "film", "cinema", "theater", "theatre", "show", "screening", "premiere",
            "concert", "performance", "show", "gig", "live", "stage", "venue",
            "music", "musical", "song", "album", "band", "artist", "musician", "singer", "concert",
            "art", "artwork", "painting", "sculpture", "gallery", "museum", "exhibition", "exhibit", "display",
            "festival", "event", "celebration", "party", "gathering", "occasion",
            "sport", "sports", "sporting", "game", "match", "competition", "tournament", "championship",
            "fitness", "gym", "gymnasium", "workout", "exercise", "training", "athletic", "athlete",
            "club", "membership", "member", "join", "participate", "activity",
            "reading", "book", "novel", "magazine", "newspaper", "journal", "publication", "literature",
            "media", "broadcasting", "broadcast", "radio", "television", "tv", "podcast",
            "hobby", "interest", "collection", "craft", "creative",
            
            // ===== COMMUNICATION & MEDIA ===== (~65 keywords)
            "communication", "communicate", "communicating", "contact", "interaction",
            "media", "mass media", "social media", "network", "networking", "social network",
            "telephone", "phone", "call", "calling", "phone call", "mobile", "cell phone", "smartphone",
            "message", "messaging", "text", "texting", "sms", "chat", "chatting", "conversation",
            "email", "e-mail", "mail", "letter", "correspondence", "memo", "notice",
            "interview", "discussion", "meeting", "talk", "dialogue", "debate",
            "announcement", "notify", "notification", "alert", "inform", "information",
            "publication", "publish", "press", "journalism", "journalist", "reporter",
            "news", "article", "story", "report", "reporting", "coverage", "headline",
            "advertisement", "advertising", "ad", "commercial", "publicity", "public relations", "pr",
            
            // ===== HOUSING & REAL ESTATE ===== (~55 keywords)
            "housing", "real estate", "property", "estate", "realty",
            "building", "structure", "construction", "architect", "architecture", "design",
            "apartment", "condo", "condominium", "flat", "unit", "house", "home", "residence", "dwelling",
            "tenant", "renter", "landlord", "owner", "lessor", "lessee",
            "rent", "rental", "renting", "lease", "leasing", "tenancy",
            "mortgage", "loan", "financing", "down payment", "installment",
            "utilities", "utility", "electricity", "water", "gas", "heating", "cooling",
            "maintenance", "repair", "upkeep", "renovation", "remodel", "upgrade",
            "furnished", "unfurnished", "amenity", "facility", "parking", "garage",
            
            // ===== VIETNAMESE TOPICS ===== (~150+ keywords)
            
            // Kinh doanh & Công việc
            "công việc", "làm việc", "việc làm", "nghề nghiệp", "sự nghiệp",
            "văn phòng", "nơi làm việc", "doanh nghiệp", "công ty", "tập đoàn", "kinh doanh",
            "họp", "cuộc họp", "hội nghị", "hội thảo", "thuyết trình", "bài thuyết trình",
            "hợp đồng", "thỏa thuận", "thương lượng", "đàm phán", "giao dịch",
            "quản lý", "người quản lý", "giám đốc", "lãnh đạo", "trưởng phòng",
            "nhân viên", "cán bộ", "công nhân", "đồng nghiệp", "đội ngũ",
            "tuyển dụng", "tìm việc", "ứng tuyển", "phỏng vấn",
            "bán hàng", "tiếp thị", "quảng cáo", "khuyến mãi", "chăm sóc khách hàng",
            "khách hàng", "người tiêu dùng", "đối tác", "nhà cung cấp",
            "sản xuất", "chế tạo", "nhà máy", "kho", "chuỗi cung ứng",
            
            // Tài chính & Kinh tế
            "tài chính", "tài chánh", "ngân hàng", "tiền", "tiền bạc", "kinh tế", "kinh tế học",
            "kế toán", "kế toán viên", "ngân sách", "chi phí", "doanh thu", "lợi nhuận",
            "đầu tư", "nhà đầu tư", "cổ phiếu", "trái phiếu", "chứng khoán",
            "thuế", "khai thuế", "kiểm toán", "bảo hiểm", "quyền lợi",
            "thanh toán", "giao dịch", "hóa đơn", "biên lai", "chuyển khoản",
            "tiền tệ", "tỷ giá", "lạm phát", "lãi suất", "giá cả",
            
            // Du lịch & Giao thông
            "du lịch", "đi du lịch", "khách du lịch", "kỳ nghỉ", "nghỉ mát", "chuyến đi",
            "khách sạn", "nhà nghỉ", "khu nghỉ dưỡng", "chỗ ở", "phòng nghỉ",
            "đặt phòng", "đặt chỗ", "xác nhận", "nhận phòng", "trả phòng",
            "giao thông", "phương tiện", "vận chuyển", "vận tải", "di chuyển",
            "xe", "ô tô", "xe buýt", "xe lửa", "tàu hòa", "tàu điện ngầm",
            "máy bay", "hàng không", "sân bay", "chuyến bay", "cất cánh", "hạ cánh",
            "tàu", "tàu thủy", "du thuyền", "cảng", "bến cảng",
            "vé", "giá vé", "lịch trình", "tuyến đường", "điểm đến", "địa điểm",
            "hành lý", "va li", "hộ chiếu", "thị thực", "hải quan",
            
            // Công nghệ & Internet
            "công nghệ", "kỹ thuật", "máy tính", "máy vi tính", "laptop", "điện thoại", "di động",
            "internet", "mạng", "trực tuyến", "trang web", "website", "nền tảng",
            "phần mềm", "ứng dụng", "chương trình", "hệ thống", "dữ liệu",
            "mạng lưới", "kết nối", "wifi", "máy chủ", "đám mây",
            "email", "thư điện tử", "tin nhắn", "nhắn tin", "trò chuyện",
            "mật khẩu", "đăng nhập", "tài khoản", "bảo mật", "an toàn",
            
            // Giáo dục & Đào tạo
            "giáo dục", "đào tạo", "học tập", "học", "nghiên cứu",
            "trường", "trường học", "trường đại học", "đại học", "cao đẳng", "học viện",
            "học sinh", "sinh viên", "người học", "nghiên cứu sinh",
            "giáo viên", "giảng viên", "thầy giáo", "cô giáo", "gia sư",
            "khóa học", "lớp học", "bài học", "bài giảng", "buổi học",
            "kỳ thi", "thi", "kiểm tra", "đánh giá", "điểm số", "kết quả",
            "bằng cấp", "chứng chỉ", "văn bằng", "học vị",
            "bài tập", "dự án", "luận văn", "nghiên cứu", "tài liệu",
            "sách", "sách giáo khoa", "thư viện", "phòng thí nghiệm",
            
            // Y tế & Sức khỏe
            "y tế", "chăm sóc sức khỏe", "sức khỏe", "sức khoẻ", "khỏe mạnh",
            "y học", "y khoa", "lâm sàng", "điều trị", "chữa trị",
            "bệnh viện", "phòng khám", "trung tâm y tế",
            "nhà thuốc", "hiệu thuốc", "dược sĩ",
            "bác sĩ", "y sĩ", "y tá", "điều dưỡng", "nhân viên y tế",
            "bệnh nhân", "ca bệnh", "hồ sơ bệnh án",
            "khám bệnh", "khám", "chẩn đoán", "tư vấn",
            "thuốc", "dược phẩm", "đơn thuốc", "liều lượng",
            "tiêm", "tiêm chủng", "vắc xin", "phòng ngừa",
            "phẫu thuật", "mổ", "thủ thuật",
            "hẹn khám", "đặt lịch", "cấp cứu", "khẩn cấp", "sơ cứu",
            
            // Môi trường & Thiên nhiên
            "môi trường", "môi sinh", "thiên nhiên", "tự nhiên", "sinh thái",
            "xanh", "xanh sạch", "bền vững", "tái tạo", "bảo tồn", "bảo vệ",
            "ô nhiễm", "chất thải", "rác", "xử lý rác", "dọn dẹp",
            "tái chế", "tái sử dụng", "giảm thiểu", "phân hủy",
            "khí hậu", "biến đổi khí hậu", "nóng lên toàn cầu", "hiệu ứng nhà kính",
            "thời tiết", "nhiệt độ", "mưa", "gió", "mặt trời",
            "năng lượng", "điện", "năng lượng mặt trời", "năng lượng gió",
            "rừng", "cây", "thực vật", "động vật hoang dã", "môi trường sống",
            "đại dương", "biển", "nước", "sông", "hồ",
            "tài nguyên", "tài nguyên thiên nhiên", "khoáng sản",
            
            // Đời sống hàng ngày
            "đời sống", "cuộc sống", "sinh hoạt", "hàng ngày", "thường ngày",
            "gia đình", "nhà", "nhà ở", "căn hộ", "chỗ ở",
            "đồ nội thất", "nội thất", "đồ dùng", "thiết bị", "dụng cụ",
            "vệ sinh", "dọn dẹp", "công việc nhà", "sửa chữa", "bảo trì",
            "bạn bè", "hàng xóm", "láng giềng", "cộng đồng", "xã hội",
            "cá nhân", "riêng tư", "thói quen", "hoạt động",
            
            // Ẩm thực & Ăn uống
            "ẩm thực", "món ăn", "đồ ăn", "thức ăn", "bữa ăn",
            "nhà hàng", "quán ăn", "quán cà phê", "căng tin", "nhà ăn",
            "bữa sáng", "bữa trưa", "bữa tối", "bữa phụ", "món khai vị", "tráng miệng",
            "thực đơn", "đặt món", "gọi món", "phục vụ", "bồi bàn",
            "nấu ăn", "nấu nướng", "chế biến", "nguyên liệu", "gia vị",
            "đầu bếp", "bếp", "nhà bếp",
            "dinh dưỡng", "chế độ ăn", "lành mạnh", "hữu cơ", "tươi",
            "đồ uống", "nước", "nước trái cây", "cà phê", "trà",
            "vị", "ngon", "ngọt", "mặn", "cay", "chua",
            
            // Mua sắm
            "mua sắm", "mua bán", "mua hàng", "shopping",
            "cửa hàng", "cửa hiệu", "chợ", "siêu thị", "trung tâm thương mại",
            "bán lẻ", "bán buôn", "người bán", "khách hàng",
            "quần áo", "áo", "quần", "giày", "dép",
            "thời trang", "phong cách", "xu hướng", "thiết kế", "phụ kiện", "trang sức",
            
            // Giải trí & Thư giãn
            "giải trí", "vui chơi", "thư giãn", "nghỉ ngơi", "sở thích", "tiêu khiển",
            "phim", "điện ảnh", "rạp chiếu phim", "rạp phim", "chiếu phim",
            "buổi hòa nhạc", "ca nhạc", "biểu diễn", "trình diễn", "sân khấu",
            "âm nhạc", "bài hát", "ca sĩ", "nghệ sĩ", "nhạc sĩ",
            "nghệ thuật", "tranh", "bảo tàng", "triển lãm", "trưng bày",
            "lễ hội", "sự kiện", "lễ kỷ niệm", "tiệc", "tụ họp",
            "thể thao", "vận động", "thi đấu", "giải đấu", "trận đấu",
            "tập thể dục", "phòng tập", "rèn luyện", "vận động viên",
            "câu lạc bộ", "thành viên", "tham gia", "hoạt động",
            "đọc sách", "sách", "tiểu thuyết", "tạp chí", "báo", "văn học",
            
            // Truyền thông & Liên lạc
            "truyền thông", "thông tin", "liên lạc", "giao tiếp",
            "mạng xã hội", "mạng", "kết nối",
            "điện thoại", "gọi điện", "cuộc gọi", "điện thoại di động",
            "tin nhắn", "nhắn tin", "trò chuyện", "trao đổi",
            " thư", "thư từ", "thông báo", "thông tin",
            "phỏng vấn", "thảo luận", "cuộc họp", "đối thoại",
            "công bố", "cảnh báo",
            "xuất bản", "báo chí", "nhà báo", "phóng viên",
            "tin tức", "bài báo", "bản tin", "tiêu đề"
        };

        public ChatService(
            LuminaSystemContext context, 
            IOptions<OpenAIOptions> openAIOptions, 
            IHttpClientFactory httpClientFactory, 
            IUploadService uploadService)
        {
            _context = context;
            _openAIOptions = openAIOptions.Value;
            _httpClientFactory = httpClientFactory;
            _uploadService = uploadService;
        }

        public async Task<ChatResponseDTO> ProcessMessage(ChatRequestDTO request)
        {
            try
            {
                // Kiểm tra câu hỏi ngoài phạm vi TOEIC
                if (IsOutOfScopeQuestion(request.Message))
                {
                    return new ChatResponseDTO
                    {
                        Answer = "Xin lỗi, tôi chỉ có thể hỗ trợ bạn về TOEIC và học tiếng Anh. Bạn có câu hỏi nào về từ vựng, ngữ pháp, chiến lược làm bài TOEIC, hoặc luyện tập không?",
                        ConversationType = "out_of_scope",
                        Suggestions = new List<string> 
                        { 
                            "Từ vựng TOEIC thường gặp",
                            "Ngữ pháp Part 5",
                            "Chiến lược làm Part 7", 
                            "Luyện tập Listening",
                            "Mẹo làm bài Reading"
                        }
                    };
                }
                
                // Xác định loại câu hỏi
                var questionType = DetermineQuestionType(request.Message);
                
                switch (questionType)
                {
                    /*case "vocabulary":
                        return await HandleVocabularyQuestion(request);*/
                    case "grammar":
                        return await HandleGrammarQuestion(request);
                    case "toeic_strategy":
                        return await HandleTOEICStrategyQuestion(request);
                    case "practice":
                        return await HandlePracticeQuestion(request);
                    case "vocabulary_generation":
                        return await GenerateVocabularyResponse(request);
                    default:
                        return await HandleGeneralQuestion(request);
                }
            }
            catch (Exception ex)
            {
                return new ChatResponseDTO
                {
                    Answer = $"Xin lỗi, tôi gặp lỗi khi xử lý câu hỏi của bạn: {ex.Message}",
                    ConversationType = "error",
                    Suggestions = new List<string> { "Hãy thử hỏi lại", "Liên hệ hỗ trợ" }
                };
            }
        }

        private bool IsOutOfScopeQuestion(string message)
        {
            var lowerMessage = message.ToLower();
            
            // Danh sách từ khóa ngoài phạm vi TOEIC (chỉ những thứ KHÔNG liên quan TOEIC)
            // NOTE: Không bao gồm các topics thuộc TOEIC như technology, business, travel, food, healthcare
            var outOfScopeKeywords = new[]
            {
                // Programming (KHÔNG phải TOEIC technology context)
                "lập trình", "programming", "code", "javascript", "python", "java", "c#", "html", "css", "react", "angular", "nodejs", "api",
                
                // Politics
                "chính trị", "politics", "bầu cử", "chính phủ", "đảng phái", "tổng thống", "thủ tướng",
                
                // Legal (chỉ phần legal system, không phải business contracts)
                "pháp luật", "legal system", "luật sư", "tòa án", "kiện tụng",
                
                // Sports (competition sports, không phải fitness/health)
                "bóng đá", "football", "soccer", "bóng rổ", "basketball", "tennis", "cầu lông", "badminton", "bơi lội", "swimming competition",
                
                // Academic subjects (không phải education context)
                "toán học", "math", "mathematics", "vật lý", "physics", "hóa học", "chemistry", "sinh học", "biology",
                "lịch sử", "history", "địa lý", "geography", "triết học", "philosophy",
                "tâm lý học", "psychology", "xã hội học", "sociology",
                
                // Entertainment specifics (tên riêng, không phải leisure general)
                "game show", "ca sĩ cụ thể", "diễn viên cụ thể",
                
                // Cryptocurrency & speculative finance
                "bitcoin", "cryptocurrency", "crypto", "cổ phiếu cụ thể", "chứng khoán cụ thể",
                
                // Very general non-English questions
                "1+1", "thời tiết hôm nay", "giờ mấy", "what time"
            };
            
            return outOfScopeKeywords.Any(keyword => lowerMessage.Contains(keyword));
        }

        private string GetSystemPrompt()
        {
            return @"You are Lumina AI Tutor, a specialized TOEIC English learning assistant. 

**YOUR EXPERTISE AREAS:**
- TOEIC vocabulary and word usage
- English grammar for TOEIC test  
- TOEIC test strategies and tips
- Practice exercises and study plans
- English learning motivation and guidance

**LANGUAGE SUPPORT:**
- Accept questions in BOTH Vietnamese and English
- Users can ask in either language or mix both languages
- Always respond in Vietnamese with English examples when relevant

**IMPORTANT RULES:**
1. ONLY answer questions related to TOEIC English learning
2. If asked about topics outside TOEIC/English learning, politely redirect:
   'Xin lỗi, tôi chỉ có thể hỗ trợ bạn về TOEIC và học tiếng Anh. Bạn có câu hỏi nào về từ vựng, ngữ pháp, chiến lược làm bài TOEIC, hoặc luyện tập không?'

3. Be encouraging and educational
4. Provide TOEIC-specific context when applicable

**OUT OF SCOPE TOPICS:**
- General knowledge outside English learning
- Technical programming questions
- Personal advice unrelated to English learning
- Current events or politics
- Medical or legal advice
- Any topic not related to TOEIC English learning

**RESPONSE FORMAT:**
Always respond with a valid JSON object in the specified format for each question type.";
        }

        private string DetermineQuestionType(string message)
        {
            var lowerMessage = message.ToLower();
            
            // Vocabulary generation - Improved logic for bilingual support
            // Support both Vietnamese and English
            bool hasCreateAction = lowerMessage.Contains("tạo") || 
                                  lowerMessage.Contains("create") || 
                                  lowerMessage.Contains("generate") ||
                                  lowerMessage.Contains("make");
            
            bool hasVocabKeyword = lowerMessage.Contains("từ vựng") || 
                                  lowerMessage.Contains("từ") ||
                                  lowerMessage.Contains("vocabulary") || 
                                  lowerMessage.Contains("vocabularies") ||
                                  lowerMessage.Contains("word") ||
                                  lowerMessage.Contains("words");
            
            if (hasCreateAction && hasVocabKeyword)
                return "vocabulary_generation";
                
            // Vocabulary questions
           /* if (lowerMessage.Contains("từ vựng") || lowerMessage.Contains("vocabulary") || 
                lowerMessage.Contains("nghĩa") || lowerMessage.Contains("từ"))
                return "vocabulary";
                */
            // Grammar questions
            if (lowerMessage.Contains("ngữ pháp") || lowerMessage.Contains("grammar") ||
                lowerMessage.Contains("thì") || lowerMessage.Contains("tense"))
                return "grammar";
                
            // TOEIC strategy questions
            if (lowerMessage.Contains("part") || lowerMessage.Contains("mẹo") ||
                lowerMessage.Contains("chiến lược") || lowerMessage.Contains("strategy"))
                return "toeic_strategy";
                
            // Practice questions
            if (lowerMessage.Contains("luyện tập") || lowerMessage.Contains("practice") ||
                lowerMessage.Contains("bài tập") || lowerMessage.Contains("exercise"))
                return "practice";
                
            return "general";
        }

        private async Task<ChatResponseDTO> HandleVocabularyQuestion(ChatRequestDTO request)
        {
            // Lấy từ vựng của user làm context
            var userVocabularies = await GetUserVocabularies(request.UserId);
            
            var prompt = $@"You are a TOEIC vocabulary expert. Answer the user's question about vocabulary.

**User's Question:** {request.Message}
**User's Current Vocabulary:** {string.Join(", ", userVocabularies.Select(v => v.Word))}

**Instructions:**
1. Answer in Vietnamese with English examples
2. Provide TOEIC-specific context
3. Suggest related words from user's vocabulary
4. Give memory tips if applicable

IMPORTANT: You must respond with ONLY a valid JSON object in this exact format:
{{
    ""answer"": ""Your detailed answer in Vietnamese here"",
    ""suggestions"": [""Related question 1"", ""Related question 2""],
    ""relatedWords"": [""word1"", ""word2""],
    ""conversationType"": ""vocabulary""
}}

Do not include any text outside the JSON object. Start your response with {{ and end with }}.";

            return await CallOpenAIAPI(prompt);
        }

        private async Task<ChatResponseDTO> HandleGrammarQuestion(ChatRequestDTO request)
        {
            var prompt = $@"You are a TOEIC grammar expert. Answer the user's grammar question.

**User's Question:** {request.Message}

**Instructions:**
1. Explain grammar rules clearly in Vietnamese
2. Provide English examples
3. Give TOEIC-specific tips
4. Include common mistakes to avoid

IMPORTANT: You must respond with ONLY a valid JSON object in this exact format:
{{
    ""answer"": ""Detailed grammar explanation here"",
    ""suggestions"": [""Practice question 1"", ""Practice question 2""],
    ""examples"": [""Example 1"", ""Example 2""],
    ""conversationType"": ""grammar""
}}

Do not include any text outside the JSON object. Start your response with {{ and end with }}.";

            return await CallOpenAIAPI(prompt);
        }

        private async Task<ChatResponseDTO> HandleTOEICStrategyQuestion(ChatRequestDTO request)
        {
            // Lấy điểm số gần nhất của user
            var recentScores = await GetUserRecentScores(request.UserId);
            
            var prompt = $@"You are a TOEIC test strategy expert. Help the user with TOEIC strategies.

**User's Question:** {request.Message}
**User's Recent Scores:** {string.Join(", ", recentScores.Select(s => $"{s.Exam.Name}: {s.Score}"))}

**Instructions:**
1. Provide specific strategies for TOEIC parts
2. Give time management tips
3. Suggest practice methods
4. Be encouraging and practical

IMPORTANT: You must respond with ONLY a valid JSON object in this exact format:
{{
    ""answer"": ""Detailed strategy explanation here"",
    ""suggestions"": [""Practice tip 1"", ""Practice tip 2""],
    ""conversationType"": ""toeic_strategy""
}}

Do not include any text outside the JSON object. Start your response with {{ and end with }}.";

            return await CallOpenAIAPI(prompt);
        }

        private async Task<ChatResponseDTO> HandlePracticeQuestion(ChatRequestDTO request)
        {
            var prompt = $@"You are a TOEIC practice expert. Help the user with practice and exercises.

**User's Question:** {request.Message}

**Instructions:**
1. Provide practice suggestions
2. Give exercise recommendations
3. Suggest study schedules
4. Be motivational and practical

IMPORTANT: You must respond with ONLY a valid JSON object in this exact format:
{{
    ""answer"": ""Practice recommendations here"",
    ""suggestions"": [""Practice method 1"", ""Practice method 2""],
    ""conversationType"": ""practice""
}}

Do not include any text outside the JSON object. Start your response with {{ and end with }}.";

            return await CallOpenAIAPI(prompt);
        }

        private async Task<ChatResponseDTO> GenerateVocabularyResponse(ChatRequestDTO request)
        {
            // STEP 0: Check if user specified exact words (comma-separated list)
            var (isSpecificWords, specifiedWords, wordCount, specificWordsError) = DetectSpecificWordsRequest(request.Message);
            
            if (isSpecificWords)
            {
                // Handle specific words mode
                if (!string.IsNullOrEmpty(specificWordsError))
                {
                    // Validation error (count mismatch or > 30 words)
                    return new ChatResponseDTO
                    {
                        Answer = specificWordsError,
                        ConversationType = "error",
                        Suggestions = new List<string>
                        {
                            "tạo 3 từ vựng book, read, study",
                            "tạo từ vựng computer, phone, internet",
                            "generate vocabulary meeting, presentation, report"
                        }
                    };
                }
                
                // Generate for specific words (SKIP topic validation)
                return await GenerateSpecificVocabularies(request, specifiedWords!);
            }
            
            // STEP 1: Topic-based mode - Validate count first
            var (count, countError) = ExtractVocabularyCountWithValidation(request.Message);
            if (!string.IsNullOrEmpty(countError))
            {
                return new ChatResponseDTO
                {
                    Answer = countError,
                    ConversationType = "error",
                    Suggestions = new List<string>
                    {
                        "Tạo 10 từ vựng về Business",
                        "Tạo 15 từ vựng về Travel",
                        "Tạo 20 từ vựng về Technology"
                    }
                };
            }

            // STEP 2: Validate topic
            var (topic, topicError) = ExtractAndValidateTopic(request.Message);
            if (!string.IsNullOrEmpty(topicError))
            {
                // Check if it's "ask for topic" vs "rejection"
                var conversationType = topicError.Contains("Bạn muốn tạo từ vựng về chủ đề gì") ? "question" : "error";
                
                return new ChatResponseDTO
                {
                    Answer = topicError,
                    ConversationType = conversationType,
                    Suggestions = new List<string>
                    {
                        "Tạo từ vựng về Business",
                        "Tạo từ vựng về Travel",
                        "Tạo từ vựng về Technology",
                        "Tạo từ vựng về Education"
                    }
                };
            }

            // STEP 3: Both validations passed - generate vocabularies
            var prompt = $@"You are a TOEIC vocabulary expert. Generate vocabulary words based on user's request.

**User's Request:** {request.Message}
**Topic:** {topic}
**Number of words:** {count}

**Instructions:**
Generate EXACTLY {count} vocabulary words related to the topic ""{topic}"". Each word should be:
1. Commonly used in TOEIC exams
2. Include definition in Vietnamese
3. Include example sentence
4. Include word type (Noun, Verb, Adjective, etc.)
5. Include category: ""{topic}""
6. Include a detailed image description for EACH word (not one for the whole topic)

For EACH vocabulary word, create a detailed image description that will be used to generate a visual representation. The image description should be:
- In English
- Descriptive and relevant to THAT SPECIFIC WORD
- Suitable for educational/learning context
- About 15-25 words
- Focus on the meaning and context of that specific word

IMPORTANT: You must respond with ONLY a valid JSON object in this exact format:
{{
    ""answer"": """",
    ""vocabularies"": [
        {{
            ""word"": ""acquire"",
            ""definition"": ""đạt được, thu được"",
            ""example"": ""The company acquired a new building last year."",
            ""typeOfWord"": ""Verb"",
            ""category"": ""{topic}"",
            ""imageDescription"": ""A business person signing a contract and acquiring a new property, professional office setting with documents""
        }}
    ],
    ""hasSaveOption"": true,
    ""saveAction"": ""CREATE_VOCABULARY_LIST"",
    ""conversationType"": ""vocabulary_generation""
}}

CRITICAL REQUIREMENTS:
- You MUST generate EXACTLY {count} vocabulary words (not more, not less)
- EVERY vocabulary item MUST have an imageDescription field (do not skip any)
- imageDescription must be a valid string (not null, not empty)
- Set ""answer"" to empty string (""""), do not include any text in answer field
- Do not include any text outside the JSON object
- Start your response with {{ and end with }}.";

            var response = await CallOpenAIAPI(prompt);
            
            // Parse vocabularies if present
            if (response.Vocabularies != null && response.Vocabularies.Count > 0)
            {
                response.HasSaveOption = true;
                response.SaveAction = "CREATE_VOCABULARY_LIST";
                
                // Generate Pollinations URL cho TỪNG vocabulary từ imageDescription của nó
                // KHÔNG upload lên Cloudinary ngay - chỉ upload khi user click save button
                foreach (var vocab in response.Vocabularies)
                {
                    // Nếu không có imageDescription, tạo một mô tả đơn giản từ word
                    if (string.IsNullOrWhiteSpace(vocab.ImageDescription))
                    {
                        // Fallback: Tạo imageDescription từ word và definition
                        vocab.ImageDescription = $"A visual representation of {vocab.Word.ToLower()}, {vocab.Definition}";
                    }
                    
                    // Generate Pollinations AI URL từ imageDescription
                    // Lưu Pollinations URL tạm thời, sẽ upload lên Cloudinary khi user click save
                    var pollinationsUrl = GeneratePollinationsImageUrl(vocab.ImageDescription);
                    vocab.ImageUrl = pollinationsUrl; // Lưu Pollinations URL tạm thời
                }
            }
            
            return response;
        }

        // Extract and validate vocabulary count from user request
        private (int count, string? errorMessage) ExtractVocabularyCountWithValidation(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return (10, null); // Default 10 từ
            
            // Patterns để tìm số (bao gồm cả số âm)
            var patterns = new[]
            {
                @"(-?\d+)\s*(?:từ|từ vựng|vocabulary|vocabularies|words|word)",
                @"(?:tạo|create|generate|make)\s+(-?\d+)",
                @"(-?\d+)\s*(?:cho|for|về|about)"
            };
            
            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(message, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int count))
                {
                    // Validate số lượng
                    if (count <= 0)
                    {
                        return (0, "Số lượng từ vựng phải lớn hơn 0. Vui lòng nhập số từ 1 đến 30!");
                    }
                    
                    if (count > 30)
                    {
                        return (0, "Số lượng từ vựng tối đa là 30. Vui lòng giảm số lượng xuống!");
                    }
                    
                    return (count, null);
                }
            }
            
            // Fallback: tìm bất kỳ số nào trong message
            var numberMatch = System.Text.RegularExpressions.Regex.Match(message, @"-?\d+");
            if (numberMatch.Success && int.TryParse(numberMatch.Value, out int number))
            {
                if (number <= 0)
                {
                    return (0, "Số lượng từ vựng phải lớn hơn 0. Vui lòng nhập số từ 1 đến 30!");
                }
                
                if (number > 30)
                {
                    return (0, "Số lượng từ vựng tối đa là 30. Vui lòng giảm số lượng xuống!");
                }
                
                if (number >= 1 && number <= 30)
                {
                    return (number, null);
                }
            }
            
            return (10, null); // Default 10 từ
        }

        // Extract and validate topic from user request
        private (string? topic, string? errorMessage) ExtractAndValidateTopic(string message)
        {
            var lowerMessage = message.ToLower();
            
            // Patterns để extract topic
            var patterns = new[]
            {
                @"(?:tạo|generate|create|make)\s+(?:-?\d+\s*)?(?:từ\s*vựng|từ|vocabulary|vocabularies|words?)\s+(?:về|cho|for|about|on)\s+(.+)",
                @"(?:từ\s*vựng|vocabulary|vocabularies)\s+(?:về|cho|for|about|on)\s+(.+)",
                @"(?:chủ\s*đề|topic)\s*[:\s]+(.+)"
            };
            
            string? extractedTopic = null;
            
            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(message, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    extractedTopic = match.Groups[1].Value.Trim();
                    // Remove trailing punctuation
                    extractedTopic = System.Text.RegularExpressions.Regex.Replace(extractedTopic, @"[.!?]+$", "");
                    break;
                }
            }
            
            // Case 1: Không có topic
            if (string.IsNullOrWhiteSpace(extractedTopic))
            {
                return (null, "Bạn muốn tạo từ vựng về chủ đề gì? Ví dụ: Business, Travel, Technology, Education, Healthcare, Environment, v.v.");
            }
            
            // Case 2: Nhiều topics (có "và", "and", ",")
            if (extractedTopic.Contains(" và ") || extractedTopic.Contains(" and ") || 
                extractedTopic.Contains(",") || extractedTopic.Contains(" & "))
            {
                // Split topics
                var topicDelimiters = new[] { " và ", " and ", ",", " & " };
                var topics = extractedTopic.Split(topicDelimiters, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();
                
                if (topics.Count > 1)
                {
                    return (null, $"Vui lòng chọn một chủ đề duy nhất. Bạn đã chỉ định {topics.Count} chủ đề: {string.Join(", ", topics)}. Hãy chọn một trong số đó!");
                }
            }
            
            // Case 3: Validate topic có thuộc TOEIC không
            bool isValidTOEICTopic = ValidTOEICTopics.Any(validTopic => 
                extractedTopic.Contains(validTopic, StringComparison.OrdinalIgnoreCase));
            
            if (!isValidTOEICTopic)
            {
                return (null, $"Xin lỗi, chủ đề \"{extractedTopic}\" không phù hợp với TOEIC. Tôi chỉ có thể tạo từ vựng về các chủ đề TOEIC như: Business, Travel, Technology, Education, Healthcare, Environment, v.v.");
            }
            
            // Topic hợp lệ
            return (extractedTopic, null);
        }

        // Detect if user specified exact words (comma-separated list)
        private (bool isSpecificWords, List<string>? words, int count, string? errorMessage) DetectSpecificWordsRequest(string message)
        {
            // Patterns to detect specific words list with optional count
            var patterns = new[]
            {
                // "tạo 3 từ vựng word1, word2, word3" or "create 3 vocabulary word1, word2, word3"
                @"(?:tạo|create|generate|make)\s+(\d+)\s+(?:từ\s*vựng|vocabulary|vocabularies|words?)\s+(.+)",
                
                // "tạo từ vựng word1, word2, word3" or "create vocabulary word1, word2, word3"
                @"(?:tạo|create|generate|make)\s+(?:từ\s*vựng|vocabulary|vocabularies|words?)\s+(.+)",
                
                // "từ vựng word1, word2, word3" or "vocabulary word1, word2, word3"
                @"(?:từ\s*vựng|vocabulary|vocabularies)\s+(.+)"
            };
            
            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(message, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    // Determine which group has the words text
                    string wordsText = "";
                    int? specifiedCount = null;
                    
                    if (match.Groups.Count == 3 && !string.IsNullOrEmpty(match.Groups[1].Value) && char.IsDigit(match.Groups[1].Value[0]))
                    {
                        // Pattern with count: Group 1 = count, Group 2 = words
                        int.TryParse(match.Groups[1].Value, out int c);
                        specifiedCount = c;
                        wordsText = match.Groups[2].Value.Trim();
                    }
                    else if (match.Groups.Count >= 2)
                    {
                        // Pattern without count: last group = words
                        wordsText = match.Groups[match.Groups.Count - 1].Value.Trim();
                    }
                    
                    // Check if contains comma (indicates specific words list)
                    if (!string.IsNullOrEmpty(wordsText) && (wordsText.Contains(",") || wordsText.Contains("、")))
                    {
                        // Split words by comma
                        var separators = new[] { ",", "、" };
                        var words = wordsText.Split(separators, StringSplitOptions.RemoveEmptyEntries)
                            .Select(w => w.Trim())
                            .Where(w => !string.IsNullOrWhiteSpace(w))
                            .ToList();
                        
                        int actualCount = words.Count;
                        
                        // Validate maximum 30 words
                        if (actualCount > 30)
                        {
                            return (false, null, 0, "Số lượng từ vựng tối đa là 30. Vui lòng giảm số lượng xuống!");
                        }
                        
                        // Validate count matches if specified
                        if (specifiedCount.HasValue)
                        {
                            if (specifiedCount.Value != actualCount)
                            {
                                return (false, null, 0, $"Số lượng bạn chỉ định ({specifiedCount}) không khớp với danh sách từ ({actualCount} từ). Vui lòng kiểm tra lại!");
                            }
                            
                            // Validate count > 0
                            if (specifiedCount.Value <= 0)
                            {
                                return (false, null, 0, "Số lượng từ vựng phải lớn hơn 0. Vui lòng nhập số từ 1 đến 30!");
                            }
                        }
                        
                        // All validations passed
                        return (true, words, actualCount, null);
                    }
                }
            }
            
            // Not a specific words request
            return (false, null, 0, null);
        }

        // Generate vocabularies for user-specified exact words
        private async Task<ChatResponseDTO> GenerateSpecificVocabularies(ChatRequestDTO request, List<string> words)
        {
            int count = words.Count;
            string wordsList = string.Join(", ", words);
            
            var prompt = $@"You are a TOEIC vocabulary expert. Generate vocabulary definitions for SPECIFIC words provided by the user.

**User's Request:** {request.Message}
**Specific Words to Define:** {wordsList}
**Number of words:** {count}

**Instructions:**
Generate vocabulary entries for EXACTLY these {count} words in order: {wordsList}

For EACH word, you MUST:
1. **Word field MUST ALWAYS be in ENGLISH:**
   - If user input is Vietnamese (e.g., ""đọc sách"") → translate to English (""reading"")
   - If user input is English (e.g., ""book"") → keep as English (""book"")
   - The ""word"" field should NEVER contain Vietnamese text
2. **Definition field MUST be in Vietnamese:**
   - Provide Vietnamese meaning/translation
3. Include example sentence in English
4. Include word type (Noun, Verb, Adjective, Gerund, etc.)
5. Determine appropriate TOEIC category (Business, Travel, Technology, Education, etc.)
6. Include a detailed image description for that specific word

**CRITICAL TRANSLATION RULES:**
- User input ""đọc sách"" (Vietnamese) → word: ""reading"" (English), definition: ""đọc, việc đọc sách""
- User input ""sách"" (Vietnamese) → word: ""book"" (English), definition: ""sách, quyển sách""
- User input ""book"" (English) → word: ""book"" (English), definition: ""sách, quyển sách""
- ALL vocabulary entries MUST have English word, Vietnamese definition

**IMPORTANT RULES:**
- Generate entries for ALL {count} words in the EXACT order provided
- Do NOT skip any words or add extra words
- Auto-detect each word's language and translate Vietnamese → English for word field
- If input is a phrase (e.g., ""lập trình Python""), translate entire phrase to English

IMPORTANT: You must respond with ONLY a valid JSON object in this exact format:
{{
    ""answer"": """",
    ""vocabularies"": [
        {{
            ""word"": ""reading"",
            ""definition"": ""đọc, việc đọc sách"",
            ""example"": ""Reading books is my favorite hobby."",
            ""typeOfWord"": ""Noun/Gerund"",
            ""category"": ""Education"",
            ""imageDescription"": ""A person reading a book in a comfortable chair, warm lighting, peaceful atmosphere, educational setting""
        }},
        {{
            ""word"": ""book"",
            ""definition"": ""sách, quyển sách"",
            ""example"": ""I bought a new book yesterday."",
            ""typeOfWord"": ""Noun"",
            ""category"": ""Education"",
            ""imageDescription"": ""A stack of books on a wooden table, colorful covers, library or study room setting""
        }}
    ],
    ""hasSaveOption"": true,
    ""saveAction"": ""CREATE_VOCABULARY_LIST"",
    ""conversationType"": ""vocabulary_generation""
}}

CRITICAL REQUIREMENTS:
- You MUST generate EXACTLY {count} vocabulary entries (not more, not less)
- EVERY vocabulary item MUST have an imageDescription field (do not skip any)
- Process words in the exact order given: {wordsList}
- Set ""answer"" to empty string ("""")
- Do not include any text outside the JSON object
- Start your response with {{ and end with }}.";

            var response = await CallOpenAIAPI(prompt);
            
            // Generate Pollinations URLs for images (same as topic-based generation)
            if (response.Vocabularies != null && response.Vocabularies.Count > 0)
            {
                response.HasSaveOption = true;
                response.SaveAction = "CREATE_VOCABULARY_LIST";
                
                foreach (var vocab in response.Vocabularies)
                {
                    // Nếu không có imageDescription, tạo một mô tả đơn giản từ word
                    if (string.IsNullOrWhiteSpace(vocab.ImageDescription))
                    {
                        vocab.ImageDescription = $"A visual representation of {vocab.Word.ToLower()}, {vocab.Definition}";
                    }
                    
                    var pollinationsUrl = GeneratePollinationsImageUrl(vocab.ImageDescription);
                    vocab.ImageUrl = pollinationsUrl;
                }
            }
            
            return response;
        }

        // Generate Pollinations AI image URL from description
        private string GeneratePollinationsImageUrl(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return string.Empty;

            string encodedPrompt = Uri.EscapeDataString(description.Trim());
            string imageUrl = $"https://image.pollinations.ai/prompt/{encodedPrompt}?model=flux&width=512&height=512&seed=random&nologo=true&enhance=true&safe=true";
            return imageUrl;
        }

        private async Task<ChatResponseDTO> HandleGeneralQuestion(ChatRequestDTO request)
        {
            var prompt = $@"You are an AI assistant specialized in TOEIC English learning.

**User's Question:** {request.Message}

**Instructions:**
1. Answer in Vietnamese with English examples when relevant
2. Be helpful and educational
3. Provide TOEIC-specific context when applicable
4. Be encouraging and supportive

IMPORTANT: You must respond with ONLY a valid JSON object in this exact format:
{{
    ""answer"": ""Helpful answer in Vietnamese here"",
    ""suggestions"": [""Related question 1"", ""Related question 2""],
    ""conversationType"": ""general""
}}

Do not include any text outside the JSON object. Start your response with {{ and end with }}.";

            return await CallOpenAIAPI(prompt);
        }

        private async Task<ChatResponseDTO> CallOpenAIAPI(string prompt)
        {
            try
            {
                if (string.IsNullOrEmpty(_openAIOptions.ApiKey))
                {
                    throw new Exception("OpenAI API key is not configured");
                }
                
                // Thêm System Prompt vào đầu
                var systemPrompt = GetSystemPrompt();
                
                // Create HttpClient from factory
                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", _openAIOptions.ApiKey);
                
                // Prepare the request - OpenAI format
                var requestBody = new
                {
                    model = _openAIOptions.Model,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.3, // Giảm temperature để JSON chính xác hơn
                    max_tokens = 8192 // Tăng token limit để đủ cho nhiều vocabularies
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Make the API call to OpenAI
                var apiUrl = "https://api.openai.com/v1/chat/completions";
                var response = await httpClient.PostAsync(apiUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"OpenAI API error: {response.StatusCode} - {errorContent}");
                }

                var responseText = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(responseText))
                {
                    throw new Exception("No response received from OpenAI API");
                }

                // Parse the response - OpenAI format
                dynamic openAIResponse = JsonConvert.DeserializeObject(responseText);
                var generatedText = openAIResponse?.choices?[0]?.message?.content?.ToString();
                
                if (string.IsNullOrEmpty(generatedText))
                {
                    throw new Exception("No content in OpenAI API response");
                }

                // Clean up potential markdown formatting
                generatedText = generatedText.Trim();
                
                // Remove markdown code blocks
                generatedText = generatedText.Replace("```json", "").Replace("```", "").Trim();
                
                // Extract JSON from text if wrapped in other text
                int firstBrace = generatedText.IndexOf('{');
                int lastBrace = generatedText.LastIndexOf('}');
                if (firstBrace >= 0 && lastBrace > firstBrace)
                {
                    generatedText = generatedText.Substring(firstBrace, lastBrace - firstBrace + 1);
                }
                
                // Try to parse JSON response
                ChatResponseDTO result;
                try
                {
                    result = JsonConvert.DeserializeObject<ChatResponseDTO>(generatedText);
                    if (result == null)
                    {
                        throw new Exception("Failed to deserialize OpenAI API response");
                    }
                    
                    // Nếu có vocabularies, luôn set answer rỗng để frontend chỉ hiển thị vocabulary list
                    if (result.Vocabularies != null && result.Vocabularies.Count > 0)
                    {
                        result.Answer = string.Empty;
                    }
                    // Validate that answer is not raw JSON
                    else if (string.IsNullOrWhiteSpace(result.Answer) || result.Answer.Trim().StartsWith("{"))
                    {
                        // Nếu không có vocabularies và answer rỗng hoặc là JSON, kiểm tra xem có phải là raw JSON không
                        if (generatedText.Contains("\"word\"") || generatedText.Contains("\"vocabularies\""))
                        {
                            result.Answer = string.Empty; // Nếu có vẻ như là JSON vocabulary, set rỗng
                        }
                        else if (!string.IsNullOrWhiteSpace(result.Answer) && result.Answer.Trim().StartsWith("{"))
                        {
                            result.Answer = string.Empty; // Nếu answer là JSON fragment, set rỗng
                        }
                    }
                    
                    // Loại bỏ bất kỳ JSON fragments nào trong answer
                    if (!string.IsNullOrWhiteSpace(result.Answer) && 
                        (result.Answer.Contains("\"word\"") || result.Answer.Contains("\"definition\"") || 
                         result.Answer.Contains("\"example\"") || result.Answer.Contains("\"typeOfWord\"")))
                    {
                        result.Answer = string.Empty; // Nếu answer chứa JSON fragments, set rỗng
                    }
                }
                catch (Exception ex)
                {
                    // If JSON parsing fails, try to extract vocabularies manually if possible
                    Console.WriteLine($"JSON Parse Error: {ex.Message}");
                    Console.WriteLine($"Raw Response: {generatedText.Substring(0, Math.Min(500, generatedText.Length))}");
                    
                    // Try to extract vocabularies using regex if JSON is malformed
                    var vocabularies = new List<GeneratedVocabularyDTO>();
                    try
                    {
                        // Try to find vocabulary patterns in the text
                        var wordPattern = @"\""word\"":\s*\""([^""]+)\""";
                        var definitionPattern = @"\""definition\"":\s*\""([^""]+)\""";
                        var examplePattern = @"\""example\"":\s*\""([^""]+)\""";
                        var typePattern = @"\""typeOfWord\"":\s*\""([^""]+)\""";
                        var categoryPattern = @"\""category\"":\s*\""([^""]+)\""";
                        var imageDescPattern = @"\""imageDescription\"":\s*\""([^""]+)\""";
                        
                        // If we can't parse properly, return error message
                        result = new ChatResponseDTO
                        {
                            Answer = "Xin lỗi, tôi gặp lỗi khi xử lý phản hồi. Vui lòng thử lại.",
                            ConversationType = "error",
                            Suggestions = new List<string> { "Thử hỏi lại", "Đặt câu hỏi khác" },
                            Vocabularies = vocabularies
                        };
                    }
                    catch
                    {
                        // Final fallback
                        result = new ChatResponseDTO
                        {
                            Answer = "Xin lỗi, tôi gặp lỗi khi xử lý phản hồi. Vui lòng thử lại.",
                            ConversationType = "error",
                            Suggestions = new List<string> { "Thử hỏi lại", "Đặt câu hỏi khác" }
                        };
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                Console.WriteLine($"OpenAI API Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                return new ChatResponseDTO
                {
                    Answer = $"Xin lỗi, tôi không thể xử lý câu hỏi này lúc này. Vui lòng thử lại sau.\n\nChi tiết lỗi: {ex.Message}",
                    ConversationType = "error",
                    Suggestions = new List<string> { "Thử hỏi lại", "Liên hệ hỗ trợ" }
                };
            }
        }

        private async Task<List<DataLayer.Models.Vocabulary>> GetUserVocabularies(int userId)
        {
            return await _context.Vocabularies
                .Where(v => v.VocabularyList.MakeBy == userId && v.IsDeleted != true)
                .Include(v => v.VocabularyList)
                .Take(20)
                .ToListAsync();
        }

        private async Task<List<DataLayer.Models.ExamAttempt>> GetUserRecentScores(int userId)
        {
            return await _context.ExamAttempts
                .Where(e => e.UserID == userId)
                .Include(e => e.Exam)
                .OrderByDescending(e => e.StartTime)
                .Take(5)
                .ToListAsync();
        }

        public async Task<SaveVocabularyResponseDTO> SaveGeneratedVocabularies(SaveVocabularyRequestDTO request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // 1. Tạo VocabularyList mới (không cần ImageUrl cho folder, mỗi vocabulary có ảnh riêng)
                var vocabularyList = new VocabularyList
                {
                    MakeBy = request.UserId,
                    Name = request.FolderName,
                    CreateAt = DateTime.Now,
                    // Tự động public và publish folder khi lưu từ từ AI để học sinh xem được ngay
                    IsPublic = true,
                    IsDeleted = false,
                    Status = "Published"
                };
                
                _context.VocabularyLists.Add(vocabularyList);
                await _context.SaveChangesAsync();
                
                // 2. Tạo các Vocabulary và upload ảnh lên Cloudinary khi user click save
                var vocabularies = new List<DataLayer.Models.Vocabulary>();
                foreach (var vocab in request.Vocabularies)
                {
                    // Upload ảnh lên Cloudinary từ Pollinations URL (nếu có)
                    string? finalImageUrl = vocab.ImageUrl; // Mặc định giữ nguyên URL
                    
                    // Kiểm tra nếu ImageUrl là Pollinations URL (chứa "pollinations.ai")
                    if (!string.IsNullOrWhiteSpace(vocab.ImageUrl) && vocab.ImageUrl.Contains("pollinations.ai"))
                    {
                        try
                        {
                            // Upload từ Pollinations URL lên Cloudinary
                            var uploadResult = await _uploadService.UploadFromUrlAsync(vocab.ImageUrl);
                            finalImageUrl = uploadResult.Url; // Lưu Cloudinary URL
                        }
                        catch (Exception ex)
                        {
                            // Nếu upload fail, fallback về Pollinations URL hoặc null
                            Console.WriteLine($"Warning: Failed to upload image to Cloudinary for vocabulary '{vocab.Word}': {ex.Message}");
                            // Giữ nguyên Pollinations URL nếu upload thất bại
                            finalImageUrl = vocab.ImageUrl;
                        }
                    }
                    
                    var vocabulary = new DataLayer.Models.Vocabulary
                    {
                        VocabularyListId = vocabularyList.VocabularyListId,
                        Word = vocab.Word,
                        Definition = vocab.Definition,
                        Example = vocab.Example,
                        TypeOfWord = vocab.TypeOfWord,
                        Category = vocab.Category,
                        IsDeleted = false,
                        ImageUrl = finalImageUrl // Lưu Cloudinary URL hoặc Pollinations URL (nếu upload fail)
                    };
                    vocabularies.Add(vocabulary);
                }
                
                _context.Vocabularies.AddRange(vocabularies);
                await _context.SaveChangesAsync();
                
                // 3. Tạo UserSpacedRepetition cho từng từ
                var spacedRepetitions = vocabularies.Select(v => new DataLayer.Models.UserSpacedRepetition
                {
                    UserId = request.UserId,
                    VocabularyListId = vocabularyList.VocabularyListId,
                    LastReviewedAt = DateTime.Now,
                    NextReviewAt = DateTime.Now.AddDays(1),
                    ReviewCount = 0,
                    Intervals = 1,
                    Status = "New"
                }).ToList();
                
                _context.UserSpacedRepetitions.AddRange(spacedRepetitions);
                await _context.SaveChangesAsync();
                
                // 4. Lưu chat history vào UserNote
                await SaveChatMessage(request.UserId, $"Tạo folder '{request.FolderName}' với {vocabularies.Count} từ vựng", 
                    $"Đã tạo thành công folder '{request.FolderName}' và lưu {vocabularies.Count} từ vựng!");
                
                await transaction.CommitAsync();
                
                return new SaveVocabularyResponseDTO
                {
                    Success = true,
                    Message = $"Đã tạo folder '{request.FolderName}' và lưu {vocabularies.Count} từ vựng!",
                    VocabularyListId = vocabularyList.VocabularyListId,
                    VocabularyCount = vocabularies.Count
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Lỗi khi lưu từ vựng: {ex.Message}");
            }
        }

        private async Task SaveChatMessage(int userId, string userMessage, string aiResponse)
        {
            try
            {
                // Lưu câu hỏi của user
                var userNote = new DataLayer.Models.UserNote
                {
                    UserId = userId,
                    ArticleId = 0, // Không liên quan article
                    SectionId = 0, // Không liên quan section
                    NoteContent = $"User: {userMessage}",
                    CreateAt = DateTime.Now
                };

                // Lưu câu trả lời của AI
                var aiNote = new DataLayer.Models.UserNote
                {
                    UserId = userId,
                    ArticleId = 0,
                    SectionId = 0,
                    NoteContent = $"AI: {aiResponse}",
                    CreateAt = DateTime.Now
                };

                _context.UserNotes.Add(userNote);
                _context.UserNotes.Add(aiNote);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't throw to avoid breaking the main flow
                Console.WriteLine($"Error saving chat message: {ex.Message}");
            }
        }
    }
}