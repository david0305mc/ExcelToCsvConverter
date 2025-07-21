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

            // 기본 CSV 저장 경로 (상대경로 → 절대경로 변환)
            string defaultDstPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\Assets\Resources\Data"));

            // 경로 설정
            srcPath = Properties.Settings.Default.ExcelPath;
            dstPath = string.IsNullOrWhiteSpace(Properties.Settings.Default.CsvPath) ? defaultDstPath : Properties.Settings.Default.CsvPath;

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
                var result = MessageBox.Show("변환 완료!\n저장 폴더를 여시겠습니까?", "완료", MessageBoxButtons.OKCancel);

                if (result == DialogResult.OK)
                {
                    System.Diagnostics.Process.Start("explorer.exe", dstPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("오류 발생: " + ex.Message);
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
            if (!Directory.Exists(srcPath) || !Directory.Exists(dstPath))
                throw new DirectoryNotFoundException("Source or Destination path does not exist.");

            var di = new DirectoryInfo(srcPath);
            foreach (var file in di.GetFiles("*.xlsx"))
            {
                using var workbook = new XLWorkbook(file.FullName);
                foreach (var sheet in workbook.Worksheets)
                {
                    // 열 제거
                    for (int col = sheet.LastColumnUsed().ColumnNumber(); col >= 1; col--)
                    {
                        var header = sheet.Cell(1, col).GetValue<string>().Trim();
                        var typeStr = sheet.Cell(3, col).GetValue<string>().ToLower().Trim();

                        if (string.IsNullOrEmpty(header) ||
                            typeStr == "nodata" ||
                            (targetType == TargetType.Client && typeStr == "server") ||
                            (targetType == TargetType.Server && typeStr == "client"))
                        {
                            sheet.Column(col).Delete();
                        }
                    }

                    // 3행 삭제
                    sheet.Row(3).Delete();

                    // 빈 행 제거 (1열 기준)
                    for (int row = sheet.LastRowUsed().RowNumber(); row >= 1; row--)
                    {
                        var value = sheet.Cell(row, 1).GetValue<string>();
                        if (string.IsNullOrWhiteSpace(value))
                            sheet.Row(row).Delete();
                    }

                    // CSV로 저장
                    var csvPath = Path.Combine(dstPath, $"{sheet.Name}.csv");
                    using var writer = new StreamWriter(csvPath, false, Encoding.UTF8);
                    foreach (var row in sheet.RowsUsed())
                    {
                        var values = new List<string>();
                        foreach (var cell in row.CellsUsed())
                        {
                            values.Add(cell.GetValue<string>().Replace("|", "｜")); // | 문자 치환
                        }
                        writer.WriteLine(string.Join("|", values));
                    }
                }
            }
        }
    }
}