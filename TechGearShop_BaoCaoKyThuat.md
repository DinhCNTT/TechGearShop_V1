# BÁO CÁO KỸ THUẬT DỰ ÁN — TECHGEAR SHOP

> *Nền tảng Thương mại Điện tử Thiết bị Công nghệ*

| Thông tin | Chi tiết |
|---|---|
| **Người thực hiện** | Đoàn Tuệ Định |
| **Loại dự án** | Thương mại điện tử (E-commerce) |
| **Thời gian thực hiện** | 15 ngày (Fast-track) |
| **Tech Stack** | ASP.NET MVC \| SQL Server \| HTML5/CSS3/jQuery \| Bootstrap |

---

## 1. TỔNG QUAN HỆ THỐNG (SYSTEM OVERVIEW)

Xây dựng một nền tảng mua sắm thiết bị công nghệ toàn diện. Hệ thống tập trung vào trải nghiệm người dùng mượt mà, tối ưu hóa công cụ tìm kiếm (SEO) và quản lý kinh doanh chặt chẽ phía Backend.

### Kiến trúc hệ thống

- Kiến trúc: **3-Tier Architecture** (Presentation — Business Logic — Data Access)

### Tech Stack

- **Frontend:** HTML5, CSS3, JavaScript, thư viện jQuery
- **Backend:** ASP.NET MVC Framework
- **Database:** Microsoft SQL Server
- **Tiêu chuẩn:** Responsive Design (Bootstrap), SEO On-page, SSL Security

---

## 2. YÊU CẦU TÍNH NĂNG (FUNCTIONAL REQUIREMENTS)

### A. Module Khách hàng (Storefront)

#### 1. Trải nghiệm người dùng

- Giao diện Responsive tương thích mọi thiết bị (Mobile / Tablet / PC).
- Popup quảng cáo / khuyến mãi hiển thị khi vào trang (Sử dụng jQuery).
- Hỗ trợ đa ngôn ngữ (Google Translate API) và Nút liên hệ nhanh (Zalo / Facebook / Hotline).

#### 2. Mục lục & Sản phẩm

- Tìm kiếm nâng cao & Lọc sản phẩm (theo giá, hãng, loại thiết bị) sử dụng jQuery AJAX — không load lại trang.
- Đánh dấu hình ảnh (Watermark) tự động khi hiển thị ảnh sản phẩm.

#### 3. Giỏ hàng & Thanh toán

- Quản lý giỏ hàng (Thêm / Sửa số lượng / Xóa).
- Hệ thống Mã giảm giá (Coupon): Kiểm tra tính hợp lệ và áp dụng chiết khấu trực tiếp.
- Tính toán phí vận chuyển dựa trên khu vực.
- Thanh toán trực tuyến (Tích hợp thanh toán giả lập hoặc cổng VNPAY / MOMO).

### B. Module Thành viên (Membership)

- **Tài khoản:** Đăng ký, Đăng nhập, Quản lý thông tin cá nhân.
- **Khách hàng thân thiết:** Hệ thống tích lũy điểm thưởng dựa trên giá trị đơn hàng thành công (Point-based system).

### C. Module Quản trị (Admin Dashboard)

- **Quản lý sản phẩm:** CRUD sản phẩm, danh mục; đóng dấu Watermark khi upload ảnh bằng thư viện C#.
- **Quản lý đơn hàng:** Theo dõi trạng thái đơn hàng (Chờ duyệt → Đang giao → Thành công).
- **Báo cáo & Marketing:** Thống kê doanh thu, quản lý mã giảm giá, thiết lập Popup.

---

## 3. CẤU TRÚC DỮ LIỆU DỰ KIẾN (DATABASE SCHEMA)

Sơ đồ cơ sở dữ liệu gồm các bảng chính như sau:

| Bảng (Table) | Các cột chính (Columns) |
|---|---|
| `Users` | ID, Username, PasswordHash, Email, Role (Admin/Customer), Points |
| `Categories` | ID, Name, Description |
| `Products` | ID, Name, CategoryID, Price, Stock, ImagePath, Description, IsActive |
| `Coupons` | ID, Code, DiscountValue, ExpiryDate, MinOrderValue |
| `Orders` | ID, UserID, OrderDate, TotalAmount, ShippingFee, Status, PaymentMethod |
| `OrderDetails` | ID, OrderID, ProductID, Quantity, UnitPrice |

---

## 4. LỘ TRÌNH THỰC THI (15-DAY SPRINT)

| Giai đoạn | Nội dung công việc |
|---|---|
| **Giai đoạn 1** (Ngày 1–4) | Thiết kế DB SQL Server; Setup ASP.NET Project; Ráp giao diện Template HTML; CRUD Sản phẩm & Danh mục. |
| **Giai đoạn 2** (Ngày 5–9) | Làm trang chủ, trang danh sách & Chi tiết sản phẩm; Viết logic Lọc/Tìm kiếm AJAX; Xử lý Giỏ hàng (Session/Cookie). |
| **Giai đoạn 3** (Ngày 10–13) | Code chức năng Coupon, Thanh toán & Phí vận chuyển; Làm hệ thống Đăng ký/Đăng nhập & Phân quyền. |
| **Giai đoạn 4** (Ngày 14–15) | Tích hợp Popup, Watermark ảnh, Đa ngôn ngữ; Kiểm thử Responsive; Fix bug & Hoàn thiện báo cáo. |

---

## 5. CHỈ THỊ CHO AI (AI INSTRUCTIONS)

Phần này định nghĩa các quy ước và chuẩn mực lập trình bắt buộc mà AI (và cả Developer) phải tuân theo khi sinh code cho dự án TechGear Shop. Mục tiêu là đảm bảo code có chất lượng cao, dễ bảo trì và nhất quán.

### 5.1. Kiến trúc & Tổ chức Code (Architecture & Code Structure)

- **Repository Pattern:** Tách biệt hoàn toàn logic truy cập dữ liệu khỏi Business Logic. Mỗi Entity (Product, Order, User...) phải có Interface riêng (`IProductRepository`) và Implementation (`ProductRepository`).
- **Service Layer:** Tạo lớp Service (`ProductService`, `OrderService`) làm cầu nối giữa Controller và Repository, tránh đặt Business Logic trực tiếp trong Controller.
- **Dependency Injection (DI):** Đăng ký tất cả Repository và Service vào DI Container trong `Startup.cs` / `Program.cs`. Controller chỉ nhận dependency qua Constructor Injection.
- Tuân thủ nguyên tắc **SOLID**, đặc biệt là Single Responsibility Principle (SRP) — mỗi class chỉ có một lý do để thay đổi.

### 5.2. Chuẩn Clean Code

- **Đặt tên rõ nghĩa:** Dùng tiếng Anh, `camelCase` cho biến/method, `PascalCase` cho class/interface. Tránh tên viết tắt mơ hồ (vd: dùng `totalAmount` thay vì `ta`).
- Mỗi method chỉ làm một việc, không quá 30–40 dòng. Nếu dài hơn, cần refactor.
- **Không để Magic Number/String:** Dùng constants hoặc Enum. Vd: `OrderStatus.Pending` thay vì chuỗi `"Pending"`.
- Comment giải thích *"tại sao"* (why), không phải *"làm gì"* (what) — code phải tự giải thích được.
- Xóa hết code thừa (dead code), `using` thừa trước khi submit.

### 5.3. Xử lý Database (SQL & ORM)

- Ưu tiên **Entity Framework Core** (Code-First hoặc DB-First) cho các thao tác CRUD cơ bản. Dùng `DbContext` với các `DbSet` tương ứng.
- Dùng **Stored Procedures** cho các truy vấn phức tạp: báo cáo doanh thu, lọc sản phẩm nhiều điều kiện, tính điểm thưởng. Gọi SP qua EF bằng `FromSqlRaw` hoặc `ExecuteSqlRaw`.
- Bắt buộc dùng **Parameterized Queries** hoặc EF LINQ — tuyệt đối không nối chuỗi SQL thủ công để phòng SQL Injection.
- Thêm **Index** trên các cột hay filter/join: `Products.CategoryID`, `Orders.UserID`, `Orders.Status`.
- Sử dụng **Transaction** khi thực hiện nhiều thao tác liên quan (vd: tạo Order + cập nhật Stock + trừ Points).

### 5.4. Giao diện (UI — ASP.NET MVC Views)

- Tách nhỏ View bằng **Partial Views**: `_Header.cshtml`, `_Footer.cshtml`, `_ProductCard.cshtml`, `_CartSummary.cshtml` để tái sử dụng và dễ bảo trì.
- Dùng Layout (`_Layout.cshtml`) làm khung chung. Các trang con chỉ định nghĩa nội dung chính qua `@RenderBody()` và `@section`.
- Sử dụng **ViewModels** (không truyền thẳng Entity vào View) để kiểm soát dữ liệu hiển thị và tránh over-posting.
- **AJAX / jQuery:** Tất cả thao tác lọc, tìm kiếm, thêm giỏ hàng phải dùng AJAX (`$.ajax` hoặc `$.get/$.post`) để tránh reload trang. Trả về JSON từ Action Method.
- **Responsive:** Dùng Bootstrap Grid System. Kiểm thử trên ít nhất 3 breakpoints: Mobile (< 576px), Tablet (768px), Desktop (≥ 992px).

### 5.5. Bảo mật (Security)

- **Authentication & Authorization:** Dùng ASP.NET Identity hoặc custom session-based auth. Phân quyền rõ ràng giữa `[Authorize(Roles="Admin")]` và `[Authorize(Roles="Customer")]`.
- **Anti-CSRF:** Luôn dùng `@Html.AntiForgeryToken()` trong form POST và validate bằng `[ValidateAntiForgeryToken]`.
- **Mã hóa mật khẩu:** Bắt buộc dùng BCrypt hoặc ASP.NET Identity `PasswordHasher` — không lưu plain-text.
- **Validate Input:** Dùng Data Annotations trên ViewModel (`Required`, `MaxLength`, `Range`, `RegularExpression`) kết hợp `ModelState.IsValid` trong Controller.
- **Upload ảnh:** Kiểm tra extension và MIME type trước khi lưu. Lưu vào thư mục ngoài `wwwroot` nếu cần bảo mật cao hơn.

### 5.6. Xử lý lỗi & Logging

- Bắt exception ở tầng Service/Controller. Không để lộ stack trace ra ngoài — log nội bộ, hiển thị thông báo thân thiện cho user.
- Dùng `try-catch` + logging (có thể dùng **NLog** hoặc **Serilog**) cho các nghiệp vụ quan trọng: xử lý thanh toán, tạo đơn hàng.
- Tạo trang lỗi tùy chỉnh: `404.cshtml`, `500.cshtml` và cấu hình trong `Startup.cs`.

### 5.7. Hiệu năng (Performance)

- Dùng **Caching** (`MemoryCache` hoặc `OutputCache`) cho dữ liệu ít thay đổi: danh mục sản phẩm, banner, cấu hình popup.
- **Phân trang (Pagination):** Bắt buộc có phân trang cho danh sách sản phẩm — không load toàn bộ dữ liệu. Dùng `.Skip().Take()` trong LINQ.
- **Lazy Loading vs Eager Loading:** Cân nhắc dùng `.Include()` (Eager) cho các truy vấn cần Navigation Property, tránh N+1 Query.
- **Tối ưu ảnh:** Nén và tạo thumbnail khi upload. Dùng lazy loading ảnh phía Frontend (`<img loading="lazy">`).

### 5.8. Quy trình làm việc với AI

- **Khi yêu cầu sinh code:** Luôn cung cấp context đầy đủ — tên Entity, Schema bảng liên quan, và output mong muốn.
- **Kiểm tra code AI sinh ra:** Xem xét kỹ trước khi dùng. Đặc biệt chú ý phần Security, SQL, và Business Logic.
- **Tái sử dụng Partial View:** Khi yêu cầu AI tạo UI, nêu rõ cần tách thành Partial View hay Layout.
- **Nhất quán Tech Stack:** Không trộn lẫn công nghệ (vd: không dùng Razor Pages lẫn MVC trong cùng project). Yêu cầu AI tuân thủ stack đã chọn.
- **Version Control:** Commit code theo từng feature/task nhỏ. Message commit rõ ràng theo convention: `feat:`, `fix:`, `refactor:`, `docs:`.

---

*Tài liệu được soạn thảo phục vụ mục đích nội bộ dự án TechGear Shop. Mọi thay đổi về yêu cầu cần được cập nhật lại báo cáo này.*
