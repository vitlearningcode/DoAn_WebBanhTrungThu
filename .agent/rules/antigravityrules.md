# Skill: High-Converting Landing Page & Modern Web UI
- **Design System:** Thiết kế theo phong cách hiện đại (Bento Grid, Glassmorphism nhẹ, chữ có độ tương phản cao, khoảng trắng thoáng). Tránh tuyệt đối giao diện Bootstrap mặc định thô cứng.
- **Landing Page Anatomy:** Khi tạo trang chủ hoặc Landing Page, luôn đảm bảo cấu trúc chuẩn:
  1. Hero Section nổi bật (Headline rõ ràng, Sub-headline, 2 nút CTA chính/phụ, hình ảnh/mockup trực quan).
  2. Social Proof / Thống kê (Số liệu, cam kết dịch vụ, hoặc chương trình thành viên/khách hàng thân thiết).
  3. Bento Grid hoặc Feature Cards giới thiệu sản phẩm/tính năng.
  4. Testimonials & Call-to-Action (CTA) cuối trang.
- **Micro-interactions:** Luôn thêm hiệu ứng chuyển động mượt (`transition-all duration-300`, `hover:-translate-y-1`, `hover:shadow-lg`) cho các thẻ sản phẩm (Product Cards) và nút bấm.
- **Responsive:** Bắt buộc tương thích Mobile-first. Không để vỡ bố cục trên màn hình nhỏ.

# Skill: ASP.NET Core & Entity Framework Core Best Practices
- **Architecture:** Tách biệt rõ ràng giữa tầng giao diện (Razor Pages / MVC Views), xử lý điều hướng (PageModels / Controllers) và truy cập dữ liệu (DbContext / Services).
- **Async/Await:** Bắt buộc dùng bất đồng bộ (`async/await`, `ToListAsync()`, `FirstOrDefaultAsync()`) cho toàn bộ thao tác I/O và Database.
- **EF Core Optimization:**
  - Luôn dùng `.AsNoTracking()` cho các truy vấn chỉ đọc (hiển thị danh sách lên Landing Page, tìm kiếm, lọc dữ liệu) để tối ưu hiệu năng.
  - Tránh lỗi N+1 Query bằng cách sử dụng `.Include()` hoặc `.Select()` chiếu sang ViewModel/DTO.
- **Data Validation & Security:**
  - Sử dụng Data Annotations kết hợp kiểm tra `ModelState.IsValid` ở cả Client-side và Server-side.
  - Bảo vệ các form POST bằng Anti-Forgery Token.
- **Partial Views / View Components:** Tách các thành phần lặp lại trên Landing Page (Header, Footer, Product Card, Loyalty Banner) thành Partial Views hoặc View Components để dễ bảo trì.

# Skill: Strict Development Workflow & Guardrails
- **No Lazy Coding:** Không bao giờ viết comment kiểu `// ... phần code còn lại giữ nguyên` hoặc `// TODO: implement later` khi được yêu cầu viết code hoàn chỉnh cho một hàm/component.
- **Database Safety:** Trước khi thay đổi các class Entity trong `Models`, luôn kiểm tra sự ảnh hưởng đến `DbContext` và nhắc nhở tạo Migration mới nếu cần.
- **Preserve Existing Logic:** Chỉ sửa đúng phạm vi file/chức năng được yêu cầu. Không tự ý đổi tên biến, đổi cấu trúc thư mục hoặc xóa tính năng cũ khi chưa hỏi ý kiến.
- **Vietnamese UI, English Code:** Toàn bộ tên biến, hàm, class, database viết bằng tiếng Anh chuẩn PascalCase/camelCase. Toàn bộ nhãn hiển thị trên giao diện (UI text, thông báo lỗi cho người dùng) viết bằng tiếng Việt tự nhiên, chuyên nghiệp.

# Skill: Domain-Specific Context (Mooncake E-Commerce & Brand Landing Page)
- **Color Palette & Vibe:** Phối màu sang trọng, ấm cúng mang hơi hướng lễ hội Trung Thu hiện đại (Đỏ rượu vang / Crimson `#8B0000`, Vàng kim / Gold `#D4AF37`, Nền kem ấm `#FDFBF7` hoặc Xanh đêm rằm `#0F172A`).
- **Typography:** Kết hợp font chữ có chân (Serif như Playfair Display/Merriweather) cho các tiêu đề lớn (Hero Headline, Tên bộ sưu tập lễ hộp) để tạo sự cao cấp, và font không chân (Sans-serif như Inter/Plus Jakarta Sans) cho nội dung chi tiết, giá tiền.
- **Core E-Commerce & Landing Features:**
  - Khu vực trưng bày "Bộ sưu tập Lễ Hộp Thượng Hạng" (Gift Box Collections) theo dạng Bento Grid hoặc Carousel sang trọng.
  - Tính năng chọn nhân bánh / mix hộp quà tùy chỉnh hoặc bảng giá chiết khấu cho khách hàng doanh nghiệp / khách hàng thân thiết.