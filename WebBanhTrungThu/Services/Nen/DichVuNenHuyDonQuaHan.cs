using WebBanhTrungThu.Services.Interfaces;

namespace WebBanhTrungThu.Services.Nen
{
    /// <summary>
    /// Tiến trình nền chạy liên tục trong suốt vòng đời ứng dụng, định kỳ mỗi 60 giây
    /// kiểm tra và tự động hủy các đơn hàng QR MoMo quá hạn 15 phút (hoàn kho FEFO & hoàn điểm VIP)
    /// ngay cả khi khách hàng đã đóng trình duyệt, đồng thời mỗi 6 giờ dọn dẹp phiên khách vãng lai quá hạn.
    /// </summary>
    public class DichVuNenHuyDonQuaHan : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DichVuNenHuyDonQuaHan> _logger;
        private DateTime _lanDonPhienKvlCuoi = DateTime.MinValue;

        public DichVuNenHuyDonQuaHan(
            IServiceScopeFactory scopeFactory,
            ILogger<DichVuNenHuyDonQuaHan> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Chờ 10 giây để KhoiTaoCsdl hoàn tất di trú bảng/cột trước chu kỳ đầu tiên
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            using var boDem = new PeriodicTimer(TimeSpan.FromSeconds(60));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var dichVuMoMo = scope.ServiceProvider.GetRequiredService<IDichVuCongThanhToanMoMo>();
                    int soDonDaHuy = await dichVuMoMo.KiemTraVaHuyDonQrQuaHanAsync();
                    if (soDonDaHuy > 0)
                    {
                        _logger.LogInformation("DichVuNenHuyDonQuaHan đã tự động hủy {SoDonDaHuy} đơn QR quá hạn 15 phút.", soDonDaHuy);
                    }

                    DateTime bayGio = DateTime.Now;
                    if ((bayGio - _lanDonPhienKvlCuoi) >= TimeSpan.FromHours(6))
                    {
                        var dichVuGioHang = scope.ServiceProvider.GetRequiredService<IDichVuGioHang>();
                        int soPhienDaDon = await dichVuGioHang.DonDepPhienKhachVangLaiQuaHanAsync(stoppingToken);
                        _lanDonPhienKvlCuoi = bayGio;

                        if (soPhienDaDon > 0)
                        {
                            _logger.LogInformation(
                                "DichVuNenHuyDonQuaHan đã dọn dẹp {SoPhienDaDon} phiên khách vãng lai quá hạn.",
                                soPhienDaDon);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi kiểm tra đơn QR quá hạn");
                }

                try
                {
                    if (!await boDem.WaitForNextTickAsync(stoppingToken))
                    {
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
