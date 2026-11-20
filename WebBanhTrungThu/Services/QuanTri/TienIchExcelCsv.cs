using System.Globalization;
using System.Text;
using ClosedXML.Excel;

namespace WebBanhTrungThu.Services.QuanTri
{
    /// <summary>
    /// Tiện ích đọc và xuất file Excel (.xlsx qua ClosedXML) song song với CSV (UTF-8 BOM)
    /// phục vụ Import/Export danh mục Hàng hóa và Kiểm kho đối soát thực tế.
    /// </summary>
    public static class TienIchExcelCsv
    {
        public static List<List<string>> DocBangDuLieu(Stream stream, string tenFile)
        {
            string ext = Path.GetExtension(tenFile ?? string.Empty).Trim().ToLowerInvariant();
            if (ext == ".csv")
            {
                return DocCsv(stream);
            }

            if (ext == ".xlsx")
            {
                return DocXlsx(stream);
            }

            throw new InvalidOperationException("Định dạng tệp không được hỗ trợ. Vui lòng chọn tệp Excel (.xlsx) hoặc CSV (.csv).");
        }

        private static List<List<string>> DocXlsx(Stream stream)
        {
            var ketQua = new List<List<string>>();
            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheets.FirstOrDefault();
            if (ws == null) return ketQua;

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
            if (lastRow <= 0 || lastCol <= 0) return ketQua;

            for (int r = 1; r <= lastRow; r++)
            {
                var row = new List<string>(lastCol);
                bool coDuLieu = false;
                for (int c = 1; c <= lastCol; c++)
                {
                    var cell = ws.Cell(r, c);
                    string val;
                    if (cell.IsEmpty())
                    {
                        val = string.Empty;
                    }
                    else if (cell.DataType == XLDataType.Number)
                    {
                        val = cell.GetDouble().ToString(CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        val = cell.GetString().Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(val)) coDuLieu = true;
                    row.Add(val);
                }

                if (coDuLieu)
                {
                    ketQua.Add(row);
                }
            }

            return ketQua;
        }

        private static List<List<string>> DocCsv(Stream stream)
        {
            var ketQua = new List<List<string>>();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            string noiDung = reader.ReadToEnd();
            if (string.IsNullOrWhiteSpace(noiDung)) return ketQua;

            // Nhận diện dấu phân cách ',' hoặc ';' ở dòng đầu tiên ngoài chuỗi ngoặc kép
            char delimiter = NhanDienDelimiterCsv(noiDung);
            var dongHienTai = new List<string>();
            var oHienTai = new StringBuilder();
            bool trongNgoacKep = false;

            for (int i = 0; i < noiDung.Length; i++)
            {
                char ch = noiDung[i];
                if (trongNgoacKep)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < noiDung.Length && noiDung[i + 1] == '"')
                        {
                            oHienTai.Append('"');
                            i++;
                        }
                        else
                        {
                            trongNgoacKep = false;
                        }
                    }
                    else
                    {
                        oHienTai.Append(ch);
                    }
                }
                else
                {
                    if (ch == '"')
                    {
                        trongNgoacKep = true;
                    }
                    else if (ch == delimiter)
                    {
                        dongHienTai.Add(oHienTai.ToString().Trim());
                        oHienTai.Clear();
                    }
                    else if (ch == '\r' || ch == '\n')
                    {
                        if (ch == '\r' && i + 1 < noiDung.Length && noiDung[i + 1] == '\n')
                        {
                            i++;
                        }
                        dongHienTai.Add(oHienTai.ToString().Trim());
                        oHienTai.Clear();

                        if (dongHienTai.Any(x => !string.IsNullOrWhiteSpace(x)))
                        {
                            ketQua.Add(dongHienTai);
                        }
                        dongHienTai = new List<string>();
                    }
                    else
                    {
                        oHienTai.Append(ch);
                    }
                }
            }

            if (oHienTai.Length > 0 || dongHienTai.Count > 0)
            {
                dongHienTai.Add(oHienTai.ToString().Trim());
                if (dongHienTai.Any(x => !string.IsNullOrWhiteSpace(x)))
                {
                    ketQua.Add(dongHienTai);
                }
            }

            return ketQua;
        }

        private static char NhanDienDelimiterCsv(string noiDung)
        {
            int soPhay = 0;
            int soChamPhay = 0;
            bool trongNgoac = false;
            for (int i = 0; i < noiDung.Length; i++)
            {
                char ch = noiDung[i];
                if (ch == '"')
                {
                    trongNgoac = !trongNgoac;
                }
                else if (!trongNgoac)
                {
                    if (ch == '\r' || ch == '\n') break;
                    if (ch == ',') soPhay++;
                    else if (ch == ';') soChamPhay++;
                }
            }
            return soChamPhay > soPhay ? ';' : ',';
        }

        public static byte[] TaoFileXlsx(
            string tenSheet,
            string[] tieuDe,
            IEnumerable<object?[]> cacDong,
            HashSet<int>? cotSo = null,
            HashSet<int>? cotNhapTay = null)
        {
            using var workbook = new XLWorkbook();
            string sheetName = string.IsNullOrWhiteSpace(tenSheet) ? "DuLieu" : tenSheet.Trim();
            if (sheetName.Length > 31) sheetName = sheetName[..31];

            var ws = workbook.Worksheets.Add(sheetName);
            int soCot = tieuDe.Length;

            for (int c = 0; c < soCot; c++)
            {
                var cell = ws.Cell(1, c + 1);
                cell.Value = tieuDe[c];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0070F4");
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Alignment.Horizontal = (cotSo != null && cotSo.Contains(c))
                    ? XLAlignmentHorizontalValues.Right
                    : XLAlignmentHorizontalValues.Left;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#D1D5DB");
            }
            ws.Row(1).Height = 24;

            int rowIdx = 2;
            foreach (var dong in cacDong)
            {
                for (int c = 0; c < soCot; c++)
                {
                    var cell = ws.Cell(rowIdx, c + 1);
                    object? val = c < dong.Length ? dong[c] : null;
                    switch (val)
                    {
                        case null:
                            cell.Value = string.Empty;
                            break;
                        case decimal decVal:
                            cell.Value = decVal;
                            break;
                        case int intVal:
                            cell.Value = intVal;
                            break;
                        case long longVal:
                            cell.Value = longVal;
                            break;
                        case double dblVal:
                            cell.Value = dblVal;
                            break;
                        case bool boolVal:
                            cell.Value = boolVal ? "Đang bán" : "Ngừng bán";
                            break;
                        default:
                            cell.Value = val.ToString() ?? string.Empty;
                            break;
                    }

                    if (cotSo != null && cotSo.Contains(c))
                    {
                        cell.Style.NumberFormat.Format = "#,##0";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    }
                    else
                    {
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    }

                    if (cotNhapTay != null && cotNhapTay.Contains(c))
                    {
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF9C3");
                    }

                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#D1D5DB");
                }
                ws.Row(rowIdx).Height = 20;
                rowIdx++;
            }

            ws.SheetView.FreezeRows(1);
            ws.Columns().AdjustToContents();
            for (int c = 1; c <= soCot; c++)
            {
                var col = ws.Column(c);
                if (col.Width < 10) col.Width = 10;
                else if (col.Width > 45) col.Width = 45;
            }

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        public static byte[] TaoFileCsvUtf8Bom(string[] tieuDe, IEnumerable<string[]> cacDong)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", tieuDe.Select(EscapeCsv)));

            foreach (var dong in cacDong)
            {
                var cells = new string[tieuDe.Length];
                for (int i = 0; i < tieuDe.Length; i++)
                {
                    cells[i] = i < dong.Length ? EscapeCsv(dong[i]) : string.Empty;
                }
                sb.AppendLine(string.Join(",", cells));
            }

            var preamble = Encoding.UTF8.GetPreamble();
            var data = Encoding.UTF8.GetBytes(sb.ToString());
            var ketQua = new byte[preamble.Length + data.Length];
            Buffer.BlockCopy(preamble, 0, ketQua, 0, preamble.Length);
            Buffer.BlockCopy(data, 0, ketQua, preamble.Length, data.Length);
            return ketQua;
        }

        private static string EscapeCsv(string? val)
        {
            string s = val ?? string.Empty;
            if (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
            {
                return $"\"{s.Replace("\"", "\"\"")}\"";
            }
            return s;
        }

        public static decimal ParseDecimal(string? raw, decimal macDinh = 0m)
        {
            if (string.IsNullOrWhiteSpace(raw)) return macDinh;
            string s = raw.Trim()
                .Replace("đ", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("₫", string.Empty)
                .Replace("vnd", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace(" ", string.Empty);

            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var valInv))
            {
                // Xử lý trường hợp nhập dạng phân cách hàng nghìn bằng dấu chấm như "120.000"
                if (s.Contains('.') && !s.Contains(','))
                {
                    var parts = s.Split('.');
                    if (parts.Length >= 2 && parts[^1].Length == 3 && parts.Skip(1).All(p => p.Length == 3))
                    {
                        if (decimal.TryParse(s.Replace(".", string.Empty), NumberStyles.Any, CultureInfo.InvariantCulture, out var vnVal))
                            return vnVal;
                    }
                }
                return valInv;
            }

            string clean = s.Replace(",", string.Empty).Replace(".", string.Empty);
            return decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var res) ? res : macDinh;
        }

        public static int ParseInt(string? raw, int macDinh = 0)
        {
            if (string.IsNullOrWhiteSpace(raw)) return macDinh;
            decimal d = ParseDecimal(raw, macDinh);
            return (int)Math.Round(d, 0, MidpointRounding.AwayFromZero);
        }
    }
}
