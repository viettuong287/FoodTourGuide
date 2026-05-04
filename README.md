# FoodTourGuide - Web

## Yêu cầu

- [.NET SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [dotnet-ef CLI](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)

---
test
## Khởi động

### 1. Chạy database bằng Docker Compose

```bash
docker-compose up -d
```

### 2. Chạy ứng dụng (hot reload)

```bash
dotnet watch
```

---

## NuGet Packages

```
Microsoft.AspNetCore.Authentication.JwtBearer
System.IdentityModel.Tokens.Jwt
Microsoft.EntityFrameworkCore
Microsoft.EntityFrameworkCore.SqlServer
Microsoft.EntityFrameworkCore.Design
Microsoft.EntityFrameworkCore.Tools
AutoMapper
AutoMapper.Extensions.Microsoft.DependencyInjection
```

Thêm package InMemory (dùng khi test):

```bash
dotnet add package Microsoft.EntityFrameworkCore.InMemory
```

---

## Migrations (Entity Framework Core)

Cài công cụ CLI nếu chưa có:

```bash
dotnet tool install --global dotnet-ef
```

> Đảm bảo package `Microsoft.EntityFrameworkCore.Design` đã được thêm vào project chứa `DbContext`.

---

### 1. Tạo migration mới

```bash
dotnet ef migrations add <TênMigration>
```

Sinh file migration chứa các thay đổi schema so với migration trước.

---

### 2. Áp dụng migration lên cơ sở dữ liệu

```bash
dotnet ef database update [<Migration>] --project <ProjectPath> --startup-project <StartupProject> --context <DbContext>
```

Áp dụng tất cả migration chưa được áp dụng (hoặc tới migration cụ thể nếu truyền tên). Bỏ tên để cập nhật tới migration mới nhất.

---

### 3. Hoàn tác migration cuối cùng

```bash
dotnet ef migrations remove --project <ProjectPath> --startup-project <StartupProject> --context <DbContext>
```

> Không nên dùng nếu migration đã được áp dụng lên DB trừ khi rollback DB trước.

---

### 4. Liệt kê migration

```bash
dotnet ef migrations list --project <ProjectPath> --startup-project <StartupProject>
```

---

### 5. Sinh script SQL từ migration

```bash
dotnet ef migrations script [<FromMigration>] [<ToMigration>] -o <file.sql> --project <ProjectPath> --startup-project <StartupProject>
```

---

### 6. Reverse engineering (scaffold) từ database

```bash
dotnet ef dbcontext scaffold "<ConnectionString>" Microsoft.EntityFrameworkCore.SqlServer \
  --output-dir <ThưMụcModels> \
  --context <DbContext> \
  --project <ProjectPath> \
  --startup-project <StartupProject>
```

---

### 7. Package Manager Console (Visual Studio)

```powershell
# Thêm migration
Add-Migration <TênMigration> -Project <ProjectName> -StartupProject <StartupProject>

# Áp dụng migration
Update-Database -Project <ProjectName> -StartupProject <StartupProject> -Migration <TênMigration>
# Bỏ -Migration để cập nhật tới mới nhất

# Xóa migration
Remove-Migration -Project <ProjectName>

# Tạo script SQL
Script-Migration -From <FromMigration> -To <ToMigration> -Output <file.sql>

# Scaffold từ DB
Scaffold-DbContext "<ConnectionString>" Microsoft.EntityFrameworkCore.SqlServer -OutputDir <ThưMụcModels> -Context <DbContext>
```

---

### 8. Tùy chọn hữu ích

| Tùy chọn | Mô tả |
|---|---|
| `--output-dir <Đường_dẫn>` | Thư mục lưu các file migration |
| `--context <DbContext>` | Chỉ định DbContext khi project có nhiều context |
| `--project` / `--startup-project` | Dùng khi repo chứa nhiều project |
| `--verbose` | Hiển thị thông tin chi tiết khi chạy lệnh |

---

### Ví dụ nhanh

```bash
dotnet ef migrations add InitialCreate --project Api --startup-project Api
dotnet ef database update --project Api --startup-project Api
```

> Thực hiện các lệnh trên từ terminal trong thư mục solution hoặc cung cấp rõ `--project`/`--startup-project` khi cần.
