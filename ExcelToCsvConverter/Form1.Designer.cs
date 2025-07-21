namespace ExcelToCsvConverter
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnSelectExcelFolder = new Button();
            btnSelectCsvFolder = new Button();
            btnConvert = new Button();
            lblSource = new Label();
            lblTarget = new Label();
            folderBrowserDialog2 = new FolderBrowserDialog();
            folderBrowserDialog1 = new FolderBrowserDialog();
            SuspendLayout();
            // 
            // btnSelectExcelFolder
            // 
            btnSelectExcelFolder.Location = new Point(369, 45);
            btnSelectExcelFolder.Name = "btnSelectExcelFolder";
            btnSelectExcelFolder.Size = new Size(75, 23);
            btnSelectExcelFolder.TabIndex = 0;
            btnSelectExcelFolder.Text = "ExcelPath";
            btnSelectExcelFolder.UseVisualStyleBackColor = true;
            btnSelectExcelFolder.Click += btnSelectExcelFolder_Click;
            // 
            // btnSelectCsvFolder
            // 
            btnSelectCsvFolder.Location = new Point(369, 89);
            btnSelectCsvFolder.Name = "btnSelectCsvFolder";
            btnSelectCsvFolder.Size = new Size(75, 23);
            btnSelectCsvFolder.TabIndex = 1;
            btnSelectCsvFolder.Text = "CSVPath";
            btnSelectCsvFolder.UseVisualStyleBackColor = true;
            btnSelectCsvFolder.Click += btnSelectCsvFolder_Click;
            // 
            // btnConvert
            // 
            btnConvert.Location = new Point(126, 170);
            btnConvert.Name = "btnConvert";
            btnConvert.Size = new Size(75, 23);
            btnConvert.TabIndex = 2;
            btnConvert.Text = "변환";
            btnConvert.UseVisualStyleBackColor = true;
            btnConvert.Click += btnConvert_Click_1;
            // 
            // lblSource
            // 
            lblSource.AutoSize = true;
            lblSource.Location = new Point(36, 49);
            lblSource.Name = "lblSource";
            lblSource.Size = new Size(16, 15);
            lblSource.TabIndex = 3;
            lblSource.Text = "c:";
            // 
            // lblTarget
            // 
            lblTarget.AutoSize = true;
            lblTarget.Location = new Point(36, 89);
            lblTarget.Name = "lblTarget";
            lblTarget.Size = new Size(16, 15);
            lblTarget.TabIndex = 4;
            lblTarget.Text = "c:";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(lblTarget);
            Controls.Add(lblSource);
            Controls.Add(btnConvert);
            Controls.Add(btnSelectCsvFolder);
            Controls.Add(btnSelectExcelFolder);
            Name = "MainForm";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnSelectExcelFolder;
        private Button btnSelectCsvFolder;
        private Button btnConvert;
        private Label lblSource;
        private Label lblTarget;
        private FolderBrowserDialog folderBrowserDialog2;
        private FolderBrowserDialog folderBrowserDialog1;
    }
}
