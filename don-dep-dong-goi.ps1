# =============================================================================
# SCRIPT DỌN DẸP RÁC BIÊN DỊCH & ĐÓNG GÓI ĐỒ ÁN SẠCH (< 3MB)
# Cách chạy: Mở PowerShell tại thư mục gốc Solution và gõ: ./don-dep-dong-goi.ps1
# =============================================================================

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
$thuMucGoc = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $thuMucGoc

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "1. ĐANG DỌN DẸP CÁC THƯ MỤC TẠM (graphify-out, bin, obj)..." -ForegroundColor Yellow
Write-Host "================================================================" -ForegroundColor Cyan

$danhSachThuMucRac = @(
    (Join-Path $thuMucGoc "graphify-out"),
    (Join-Path $thuMucGoc "WebBanhTrungThu\bin"),
    (Join-Path $thuMucGoc "WebBanhTrungThu\obj")
)

foreach ($thuMuc in $danhSachThuMucRac) {
    if (Test-Path $thuMuc) {
        Remove-Item -Path $thuMuc -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "   [Đã xóa] $thuMuc" -ForegroundColor Green
    }
}

# Xóa các file rác *.tmp, *.bak nếu có
Get-ChildItem -Path (Join-Path $thuMucGoc "WebBanhTrungThu") -Include *.tmp, *.bak -Recurse -File -ErrorAction SilentlyContinue |
    ForEach-Object {
        Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue
    }

Write-Host "`n================================================================" -ForegroundColor Cyan
Write-Host "2. ĐANG ĐÓNG GÓI MÃ NGUỒN SẠCH RA FILE ZIP NỘP ĐỒ ÁN..." -ForegroundColor Yellow
Write-Host "================================================================" -ForegroundColor Cyan

$duongDanFileZip = Join-Path $thuMucGoc "NopDoAn_WebBanhTrungThu_Sach.zip"
if (Test-Path "NopDoAn_WebBanhTrungThu_Sach.zip") { Remove-Item "NopDoAn_WebBanhTrungThu_Sach.zip" -Force }
if (Test-Path $duongDanFileZip) {
    Remove-Item -Path $duongDanFileZip -Force
}

$danhSachThanhPhanCanNen = @()
foreach ($muc in @("WebBanhTrungThu", "DoAn_WebBanhTrungThu.slnx", ".gitignore")) {
    $duongDanDayDu = Join-Path $thuMucGoc $muc
    if (Test-Path $duongDanDayDu) {
        $danhSachThanhPhanCanNen += $duongDanDayDu
    }
}

Compress-Archive -Path $danhSachThanhPhanCanNen -DestinationPath $duongDanFileZip -CompressionLevel Optimal -Force

$thongTinZip = Get-Item $duongDanFileZip
$dungLuongMb = [Math]::Round($thongTinZip.Length / 1MB, 2)
Write-Host "   [Hoàn tất] Đã tạo file: $($thongTinZip.Name) (Dung lượng: $dungLuongMb MB)" -ForegroundColor Green

Write-Host "`n================================================================" -ForegroundColor Cyan
Write-Host "3. ĐANG KHÔI PHỤC BIÊN DỊCH (dotnet build) ĐỂ SẴN SÀNG CHẠY..." -ForegroundColor Yellow
Write-Host "================================================================" -ForegroundColor Cyan

dotnet build (Join-Path $thuMucGoc "WebBanhTrungThu\WebBanhTrungThu.csproj") --nologo
Write-Host "`n[THÀNH CÔNG] Đồ án đã được đóng gói sạch ($dungLuongMb MB) và biên dịch sẵn sàng chạy!" -ForegroundColor Green
