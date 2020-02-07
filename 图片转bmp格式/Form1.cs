using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;

namespace 图片转bmp格式
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        string[] FileNames;
        private void Form1_Load(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.CheckFileExists = true;
            ofd.Filter = "(*.jpg)|*.jpg|(*.png)|*.png";
            ofd.Multiselect = true;
            //ofd.InitialDirectory=
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                FileNames = ofd.FileNames;
            }
            if (FileNames != null && FileNames.Length >= 1)
            {

                PicBox.Load(FileNames[0]);

                ListPath.Items.AddRange(FileNames);


            }

        }

        private void ListPath_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ListPath.SelectedIndex < 0)
            {
                return;
            }

            string path = ListPath.SelectedItem as string;
            PicBox.Load(path);

        }

        private void Btn_Save_Click(object sender, EventArgs e)
        {
            if (ConvertToBitMap(PicBox.ImageLocation, SavePath.Text))
            {

                MessageBox.Show("转换完成");
            }
            else
            {
                MessageBox.Show("转换出现问题");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();

            sfd.Filter = "(*.bmp)|*.bmp";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                SavePath.Text = sfd.FileName;
            }

            sfd.Dispose();

        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fs = new FolderBrowserDialog())
            {


                if (fs.ShowDialog() == DialogResult.OK)
                {
                    SaveDir.Text = fs.SelectedPath;

                }


            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (ListPath.SelectedItems.Count <= 0)
            {

                foreach (string path in FileNames)
                {
                    string name = Path.GetFileNameWithoutExtension(path);

                    name += ".bmp";

                    string newPath = SaveDir.Text + "\\" + name;

                    if (!ConvertToBitMap(path, newPath))
                    {
                        MessageBox.Show("转换出现问题");
                        return;
                    }
                }
                MessageBox.Show("转换全部完成");
            }
            else
            {
                foreach (var item in ListPath.SelectedItems)
                {
                    string path = item.ToString();
                    string name = Path.GetFileNameWithoutExtension(path);

                    name += ".bmp";
                    string newPath = SaveDir.Text + "\\" + name;

                    if (!ConvertToBitMap(path, newPath))
                    {
                        MessageBox.Show("转换出现问题");
                        return;
                    }
                }
                MessageBox.Show("转换全部完成");

            }

        }
        private bool ConvertToBitMap(string oldPath, string newPath)
        {

            try
            {
                using (Bitmap source = new Bitmap(oldPath))
                {
                    using (Bitmap bmp = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                    {
                        Graphics.FromImage(bmp).DrawImage(source, new Rectangle(0, 0, bmp.Width, bmp.Height));
                        bmp.Save(newPath, System.Drawing.Imaging.ImageFormat.Bmp);
                    }
                }

            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }

    }
}
