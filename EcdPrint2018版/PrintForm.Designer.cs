namespace EcdPrint
{
    partial class PrintForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.comboBoxStyleSheet = new System.Windows.Forms.ComboBox();
            this.groupBoxOrientation = new System.Windows.Forms.GroupBox();
            this.radioButtonHorizontal = new System.Windows.Forms.RadioButton();
            this.radioButtonVertical = new System.Windows.Forms.RadioButton();
            this.groupBox7 = new System.Windows.Forms.GroupBox();
            this.comboBoxMedia = new System.Windows.Forms.ComboBox();
            this.btn_Print = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.groupBox3.SuspendLayout();
            this.groupBoxOrientation.SuspendLayout();
            this.groupBox7.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.comboBoxStyleSheet);
            this.groupBox3.Location = new System.Drawing.Point(12, 106);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(313, 85);
            this.groupBox3.TabIndex = 10;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "打印样式表";
            // 
            // comboBoxStyleSheet
            // 
            this.comboBoxStyleSheet.FormattingEnabled = true;
            this.comboBoxStyleSheet.Location = new System.Drawing.Point(6, 27);
            this.comboBoxStyleSheet.Name = "comboBoxStyleSheet";
            this.comboBoxStyleSheet.Size = new System.Drawing.Size(289, 20);
            this.comboBoxStyleSheet.TabIndex = 0;
            this.comboBoxStyleSheet.SelectedIndexChanged += new System.EventHandler(this.comboBox_UpdateDataBinding);
            // 
            // groupBoxOrientation
            // 
            this.groupBoxOrientation.Controls.Add(this.radioButtonHorizontal);
            this.groupBoxOrientation.Controls.Add(this.radioButtonVertical);
            this.groupBoxOrientation.Location = new System.Drawing.Point(344, 106);
            this.groupBoxOrientation.Name = "groupBoxOrientation";
            this.groupBoxOrientation.Size = new System.Drawing.Size(97, 85);
            this.groupBoxOrientation.TabIndex = 11;
            this.groupBoxOrientation.TabStop = false;
            this.groupBoxOrientation.Text = "图形方向";
            // 
            // radioButtonHorizontal
            // 
            this.radioButtonHorizontal.AutoSize = true;
            this.radioButtonHorizontal.Location = new System.Drawing.Point(7, 49);
            this.radioButtonHorizontal.Name = "radioButtonHorizontal";
            this.radioButtonHorizontal.Size = new System.Drawing.Size(47, 16);
            this.radioButtonHorizontal.TabIndex = 1;
            this.radioButtonHorizontal.TabStop = true;
            this.radioButtonHorizontal.Text = "横向";
            this.radioButtonHorizontal.UseVisualStyleBackColor = true;
            this.radioButtonHorizontal.CheckedChanged += new System.EventHandler(this.control_ValueChanged);
            // 
            // radioButtonVertical
            // 
            this.radioButtonVertical.AutoSize = true;
            this.radioButtonVertical.Checked = true;
            this.radioButtonVertical.Location = new System.Drawing.Point(7, 23);
            this.radioButtonVertical.Name = "radioButtonVertical";
            this.radioButtonVertical.Size = new System.Drawing.Size(47, 16);
            this.radioButtonVertical.TabIndex = 0;
            this.radioButtonVertical.TabStop = true;
            this.radioButtonVertical.Text = "纵向";
            this.radioButtonVertical.UseVisualStyleBackColor = true;
            this.radioButtonVertical.CheckedChanged += new System.EventHandler(this.control_ValueChanged);
            // 
            // groupBox7
            // 
            this.groupBox7.Controls.Add(this.comboBoxMedia);
            this.groupBox7.Location = new System.Drawing.Point(12, 30);
            this.groupBox7.Name = "groupBox7";
            this.groupBox7.Size = new System.Drawing.Size(313, 48);
            this.groupBox7.TabIndex = 14;
            this.groupBox7.TabStop = false;
            this.groupBox7.Text = "图纸尺寸";
            // 
            // comboBoxMedia
            // 
            this.comboBoxMedia.FormattingEnabled = true;
            this.comboBoxMedia.Location = new System.Drawing.Point(6, 19);
            this.comboBoxMedia.Name = "comboBoxMedia";
            this.comboBoxMedia.Size = new System.Drawing.Size(289, 20);
            this.comboBoxMedia.TabIndex = 3;
            this.comboBoxMedia.SelectedIndexChanged += new System.EventHandler(this.comboBox_UpdateDataBinding);
            // 
            // btn_Print
            // 
            this.btn_Print.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btn_Print.Location = new System.Drawing.Point(129, 252);
            this.btn_Print.Name = "btn_Print";
            this.btn_Print.Size = new System.Drawing.Size(75, 23);
            this.btn_Print.TabIndex = 15;
            this.btn_Print.Text = "确定";
            this.btn_Print.UseVisualStyleBackColor = true;
            this.btn_Print.Click += new System.EventHandler(this.btn_Print_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Location = new System.Drawing.Point(282, 252);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 16;
            this.buttonCancel.Text = "取消";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // PrintForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(472, 334);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.groupBox7);
            this.Controls.Add(this.groupBoxOrientation);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.btn_Print);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PrintForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "打印—模型";
            this.Load += new System.EventHandler(this.PrintForm_Load);
            this.groupBox3.ResumeLayout(false);
            this.groupBoxOrientation.ResumeLayout(false);
            this.groupBoxOrientation.PerformLayout();
            this.groupBox7.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.ComboBox comboBoxStyleSheet;
        private System.Windows.Forms.GroupBox groupBoxOrientation;
        private System.Windows.Forms.RadioButton radioButtonHorizontal;
        private System.Windows.Forms.RadioButton radioButtonVertical;
        private System.Windows.Forms.GroupBox groupBox7;
        private System.Windows.Forms.ComboBox comboBoxMedia;
        private System.Windows.Forms.Button btn_Print;
        private System.Windows.Forms.Button buttonCancel;
    }
}