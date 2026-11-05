using System.Text;
using ClosedXML.Excel;

namespace ExcelToCsvConverter
{
    public partial class MainForm : Form
    {
        private string srcPath = "";
        private string dstPath = "";

        public MainForm()
        {
            InitializeComponent();

            string defaultDstPath = Path.GetFullPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\Assets\Resources\Data")
            );

            srcPath = Properties.Settings.Default.ExcelPath;
            dstPath = string.IsNullOrWhiteSpace(Properties.Settings.Default.CsvPath)
                ? defaultDstPath
                : Properties.Settings.Default.CsvPath;

            lblSource.Text = srcPath;
            lblTarget.Text = dstPath;
        }

        private void btnSelectExcelFolder_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                srcPath = dialog.SelectedPath;
                lblSource.Text = srcPath;
                Properties.Settings.Default.ExcelPath = srcPath;
                Properties.Settings.Default.Save();
            }
        }

        private void btnSelectCsvFolder_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                dstPath = dialog.SelectedPath;
                lblTarget.Text = dstPath;
                Properties.Settings.Default.CsvPath = dstPath;
                Properties.Settings.Default.Save();
            }
        }

        private void btnConvert_Click_1(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(srcPath) || string.IsNullOrWhiteSpace(dstPath))
            {
                MessageBox.Show("경로를 모두 선택해주세요.");
                return;
            }

            try
            {
                Converter.Convert(srcPath, dstPath, Converter.TargetType.Client);
                var result = MessageBox.Show(
                    "변환 완료!\n저장 폴더를 여시겠습니까?",
                    "완료",
                    MessageBoxButtons.OKCancel
                );

                if (result == DialogResult.OK)
                {
                    System.Diagnostics.Process.Start("explorer.exe", dstPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "변환 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class Converter
    {
        public enum TargetType
        {
            Server,
            Client
        }

        public static void Convert(string srcPath, string dstPath, TargetType targetType)
        {
            var context = new ConversionContext(srcPath, dstPath);
            try
            {
                if (!Directory.Exists(srcPath))
                    throw new DirectoryNotFoundException("Excel 원본 폴더가 존재하지 않습니다.");
                if (!Directory.Exists(dstPath))
                    throw new DirectoryNotFoundException("CSV 저장 폴더가 존재하지 않습니다.");

                context.Stage = "Excel 파일 목록 읽기";
                var di = new DirectoryInfo(srcPath);
                foreach (var file in di.GetFiles("*.xlsx"))
                {
                    context.FilePath = file.FullName;
                    context.SheetName = null;
                    context.OutputPath = null;
                    context.SetStage("Excel 파일 열기");
                    using var workbook = new XLWorkbook(file.FullName);

                    foreach (var sheet in workbook.Worksheets)
                    {
                        // '_'로 시작하는 참고용 시트는 검증과 CSV 저장에서 제외한다.
                        if (sheet.Name.StartsWith("_", StringComparison.Ordinal))
                            continue;

                        context.SheetName = sheet.Name;
                        context.OutputPath = null;
                        context.SetStage("시트 구조 확인");
                        context.OutputPath = Path.Combine(dstPath, $"{sheet.Name}.csv");
                        var lastColumn = sheet.LastColumnUsed();
                        if (lastColumn == null)
                            throw new InvalidDataException("빈 시트입니다. 1행의 컬럼명과 데이터를 확인해주세요.");

                        // 삭제로 위치가 바뀌어도 오류에는 원본 Excel 좌표를 표시한다.
                        var originalColumns = Enumerable.Range(1, lastColumn.ColumnNumber()).ToList();
                        var headers = new Dictionary<int, string>();

                        for (int col = originalColumns.Count; col >= 1; col--)
                        {
                            context.SetStage("컬럼명 읽기");
                            var header = ReadCell(sheet, 1, col, 1, col, null, context).Trim();
                            headers[col] = header;
                            context.SetStage("컬럼 구분 읽기 (3행)");
                            var typeStr = ReadCell(sheet, 3, col, 3, col, header, context)
                                .ToLowerInvariant().Trim();

                            if (string.IsNullOrEmpty(header) ||
                                typeStr == "nodata" ||
                                (targetType == TargetType.Client && typeStr == "server") ||
                                (targetType == TargetType.Server && typeStr == "client"))
                            {
                                context.Stage = "컬럼 제외";
                                sheet.Column(col).Delete();
                                originalColumns.RemoveAt(col - 1);
                            }
                        }

                        context.SetStage("필터링 결과 확인");
                        if (originalColumns.Count == 0)
                            throw new InvalidDataException("변환할 컬럼이 없습니다. 1행의 컬럼명과 3행의 client/server/nodata 구분을 확인해주세요.");

                        context.SetStage("컬럼 구분 행 제거");
                        context.Row = 3;
                        sheet.Row(3).Delete();

                        context.SetStage("빈 행 확인");
                        var lastRow = sheet.LastRowUsed();
                        if (lastRow == null)
                            throw new InvalidDataException("컬럼 구분 행을 제거한 뒤 남은 데이터가 없습니다.");
                        var originalRows = Enumerable.Range(1, lastRow.RowNumber())
                            .Select(row => row >= 3 ? row + 1 : row).ToList();

                        // 기존 규칙대로 필터링 후 첫 컬럼이 빈 행을 제외한다.
                        for (int row = originalRows.Count; row >= 1; row--)
                        {
                            context.SetStage("빈 행 확인");
                            int originalCol = originalColumns[0];
                            var value = ReadCell(sheet, row, 1, originalRows[row - 1],
                                originalCol, headers[originalCol], context);
                            if (string.IsNullOrWhiteSpace(value))
                            {
                                context.Stage = "빈 행 제거";
                                sheet.Row(row).Delete();
                                originalRows.RemoveAt(row - 1);
                            }
                        }

                        context.SetStage("CSV 파일 열기");
                        using (var writer = new StreamWriter(context.OutputPath, false, new UTF8Encoding(false)))
                        {
                            foreach (var row in sheet.RowsUsed())
                            {
                                var values = new List<string>();
                                int originalRow = originalRows[row.RowNumber() - 1];
                                for (int col = 1; col <= originalColumns.Count; col++)
                                {
                                    context.SetStage("셀 값 변환");
                                    int originalCol = originalColumns[col - 1];
                                    var cellValue = ReadCell(sheet, row.RowNumber(), col, originalRow,
                                        originalCol, headers[originalCol], context).Replace("|", "｜");
                                    values.Add(cellValue);
                                }

                                context.SetStage("CSV 행 저장");
                                context.Row = originalRow;
                                writer.WriteLine(string.Join("|", values));
                            }

                            context.SetStage("CSV 저장 마무리");
                        }
                    }

                    context.SheetName = null;
                    context.OutputPath = null;
                    context.SetStage("Excel 파일 닫기");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(context.FormatError(ex), ex);
            }
        }

        private static string ReadCell(IXLWorksheet sheet, int row, int col,
            int originalRow, int originalCol, string? header, ConversionContext context)
        {
            context.Row = originalRow;
            context.Column = originalCol;
            context.Header = header;
            return Sanitize(sheet.Cell(row, col).GetValue<string>());
        }

        private sealed class ConversionContext(string sourcePath, string destinationPath)
        {
            public string Stage { get; set; } = "폴더 경로 확인";
            public string? FilePath { get; set; }
            public string? SheetName { get; set; }
            public string? OutputPath { get; set; }
            public int? Row { get; set; }
            public int? Column { get; set; }
            public string? Header { get; set; }

            public void SetStage(string stage)
            {
                Stage = stage;
                Row = null;
                Column = null;
                Header = null;
            }

            public string FormatError(Exception exception)
            {
                var message = new StringBuilder("Excel → CSV 변환 중 오류가 발생했습니다.");
                message.AppendLine().AppendLine().AppendLine($"작업: {Stage}");
                if (FilePath != null)
                {
                    message.AppendLine($"파일: {Path.GetFileName(FilePath)}");
                    message.AppendLine($"원본 경로: {FilePath}");
                }
                else
                    message.AppendLine($"원본 폴더: {sourcePath}");
                if (SheetName != null)
                    message.AppendLine($"시트: {SheetName}");
                if (Column is int column)
                {
                    string letter = XLHelper.GetColumnLetterFromNumber(column);
                    string name = string.IsNullOrWhiteSpace(Header) ? "컬럼명 없음 또는 읽기 실패" : Header;
                    message.AppendLine($"컬럼: {name} ({letter}열, {column}번째)");
                    if (Row is int cellRow)
                        message.AppendLine($"셀: {letter}{cellRow} (원본 Excel 기준)");
                }
                if (Row is int row)
                    message.AppendLine($"행: {row} (원본 Excel 기준)");
                message.AppendLine($"저장 경로: {OutputPath ?? destinationPath}");
                message.AppendLine().AppendLine($"원인: {exception.Message}");
                if (exception.GetBaseException() != exception)
                    message.AppendLine($"상세 원인: {exception.GetBaseException().Message}");
                message.Append($"오류 유형: {exception.GetType().Name}");
                return message.ToString();
            }
        }

        // 🔹 문자열 정규화 (네모 문자 방지 핵심)
        private static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            return s
                .Replace("\u00A0", " ") // NBSP (Non-Breaking Space)
                .Replace("\u200B", "")  // Zero-Width Space
                .Replace("\uFEFF", ""); // BOM / ZWNBSP
        }
    }
}
