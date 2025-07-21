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

            // �⺻ CSV ���� ��� (����� �� ������ ��ȯ)
            string defaultDstPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\Assets\Resources\Data"));

            // ��� ����
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
                MessageBox.Show("��θ� ��� �������ּ���.");
                return;
            }

            try
            {
                Converter.Convert(srcPath, dstPath, Converter.TargetType.Client);
                var result = MessageBox.Show("��ȯ �Ϸ�!\n���� ������ ���ðڽ��ϱ�?", "�Ϸ�", MessageBoxButtons.OKCancel);

                if (result == DialogResult.OK)
                {
                    System.Diagnostics.Process.Start("explorer.exe", dstPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("���� �߻�: " + ex.Message);
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
                    // �� ����
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

                    // 3�� ����
                    sheet.Row(3).Delete();

                    // �� �� ���� (1�� ����)
                    for (int row = sheet.LastRowUsed().RowNumber(); row >= 1; row--)
                    {
                        var value = sheet.Cell(row, 1).GetValue<string>();
                        if (string.IsNullOrWhiteSpace(value))
                            sheet.Row(row).Delete();
                    }

                    // CSV�� ����
                    var csvPath = Path.Combine(dstPath, $"{sheet.Name}.csv");
                    using var writer = new StreamWriter(csvPath, false, Encoding.UTF8);
                    foreach (var row in sheet.RowsUsed())
                    {
                        var values = new List<string>();
                        foreach (var cell in row.CellsUsed())
                        {
                            values.Add(cell.GetValue<string>().Replace("|", "��")); // | ���� ġȯ
                        }
                        writer.WriteLine(string.Join("|", values));
                    }
                }
            }
        }
    }
}