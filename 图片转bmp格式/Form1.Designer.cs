namespace 图片转bmp格式
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.button1 = new System.Windows.Forms.Button();
            this.Btn_Save = new System.Windows.Forms.Button();
            this.PicBox = new System.Windows.Forms.PictureBox();
            this.ListPath = new System.Windows.Forms.ListBox();
            this.SavePath = new System.Windows.Forms.TextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.SaveDir = new System.Windows.Forms.TextBox();
            this.button3 = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PicBox)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.SavePath);
            this.groupBox1.Controls.Add(this.button1);
            this.groupBox1.Controls.Add(this.Btn_Save);
            this.groupBox1.Controls.Add(this.PicBox);
            this.groupBox1.Location = new System.Drawing.Point(13, 13);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(327, 425);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "单一图片转换";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(252, 338);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 2;
            this.button1.Text = "保存位置";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // Btn_Save
            // 
            this.Btn_Save.Location = new System.Drawing.Point(246, 383);
            this.Btn_Save.Name = "Btn_Save";
            this.Btn_Save.Size = new System.Drawing.Size(75, 23);
            this.Btn_Save.TabIndex = 1;
            this.Btn_Save.Text = "转换";
            this.Btn_Save.UseVisualStyleBackColor = true;
            this.Btn_Save.Click += new System.EventHandler(this.Btn_Save_Click);
            // 
            // PicBox
            // 
            this.PicBox.Location = new System.Drawing.Point(7, 21);
            this.PicBox.Name = "PicBox";
            this.PicBox.Size = new System.Drawing.Size(320, 301);
            this.PicBox.TabIndex = 0;
            this.PicBox.TabStop = false;
            // 
            // ListPath
            // 
            this.ListPath.FormattingEnabled = true;
            this.ListPath.ItemHeight = 12;
            this.ListPath.Location = new System.Drawing.Point(360, 34);
            this.ListPath.Name = "ListPath";
            this.ListPath.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.ListPath.Size = new System.Drawing.Size(401, 304);
            this.ListPath.TabIndex = 1;
            this.ListPath.SelectedIndexChanged += new System.EventHandler(this.ListPath_SelectedIndexChanged);
            // 
            // SavePath
            // 
            this.SavePath.Location = new System.Drawing.Point(7, 338);
            this.SavePath.Name = "SavePath";
            this.SavePath.Size = new System.Drawing.Size(233, 21);
            this.SavePath.TabIndex = 3;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(686, 396);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 2;
            this.button2.Text = "批量转换";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // SaveDir
            // 
            this.SaveDir.Location = new System.Drawing.Point(360, 353);
            this.SaveDir.Name = "SaveDir";
            this.SaveDir.Size = new System.Drawing.Size(320, 21);
            this.SaveDir.TabIndex = 4;
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(686, 351);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(75, 23);
            this.button3.TabIndex = 4;
            this.button3.Text = "保存位置";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.SaveDir);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.ListPath);
            this.Controls.Add(this.groupBox1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PicBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button Btn_Save;
        private System.Windows.Forms.PictureBox PicBox;
        private System.Windows.Forms.ListBox ListPath;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox SavePath;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox SaveDir;
        private System.Windows.Forms.Button button3;
    }
}

