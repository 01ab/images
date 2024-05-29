using images;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Windows.Forms;

namespace Images
{

    public partial class MainForm : Form
    {
        INeuroProcess processor;
        IDatabase database;

        public MainForm()
        {
            InitializeComponent();
            processor = new CodeEye();
            database = new SqliteDatabase("data.db");
        }

        public MainForm(INeuroProcess process)
        {
            InitializeComponent();
            processor = process;
        }

        private void open_Click(object sender, EventArgs e)
        {
            var d = new OpenFileDialog();
            d.Filter = "Изображения|*.jpg;*.png";
            if (d.ShowDialog() == DialogResult.OK)
            {
                openFile(d.FileName);
            }
        }

        void openFile(String filename)
        {
            toolStripStatusLabel1.Text = filename;
            pictureBox1.Image = Image.FromFile(filename);
            workLabel.Text = "Поиск дефектов";
            ThreadPool.QueueUserWorkItem((o) =>
            {
                var res = processor.Process(new Bitmap(Image.FromFile(filename)));
                this.BeginInvoke(new Action(() =>
                {
                    database.SaveObjects(filename, res);
                    drawObjects(res);
                    workLabel.Text = "Готово";
                }));
            });
        }

        private void showDiffMap_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null || pictureBox1.Image.Size == Size.Empty)
                return;
            var map = CodeEye.getDiffMap(pictureBox1.Image);
            // нарисуем свою картинку из мапы
            var bmp = new Bitmap(map.GetUpperBound(0) + 1, map.GetUpperBound(1) + 1, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            {
                var data = bmp.LockBits(rect: new Rectangle(Point.Empty, bmp.Size), ImageLockMode.ReadWrite, bmp.PixelFormat);
                var pos = data.Scan0;
                var lengthBytes = data.Height * data.Stride - 1;
                var endPos = data.Scan0 + lengthBytes;
                var x = 0; var y = 0;
                var width = bmp.Width;
                // есть варианты
                while (pos.ToInt64() < endPos.ToInt64())
                {
                    if (map[x, y] > 80)
                    {
                        Marshal.WriteByte(pos + 0, 10); //B
                        Marshal.WriteByte(pos + 1, 255); //G
                        Marshal.WriteByte(pos + 2, 200);   //R
                        Marshal.WriteByte(pos + 3, 255);
                    }
                    else
                    {
                        Marshal.WriteByte(pos + 3, 255);
                    }
                    pos += 4;
                    x += 1;
                    if (x >= width)
                    {
                        x = 0;
                        y += 1;
                    }
                }
                bmp.UnlockBits(data);
            }
            bmp.Save(toolStripStatusLabel1.Text + "-diff.jpg", ImageFormat.Jpeg);
            pictureBox1.Image = bmp;
            pictureBox1.Refresh();

        }

        private string[] extensions = new string[] { ".jpg", ".png" };

        private bool isAllowedExtension(string filename)
        {
            foreach (string ext in extensions)
            {
                if (filename.EndsWith(ext, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        private void mainForm_DragDrop(object sender, DragEventArgs e)
        {
            // если файл то берём
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var f = (String[])e.Data.GetData(DataFormats.FileDrop);
                if (f != null && f.Length == 1 && isAllowedExtension(f[0]))
                {
                    openFile(f[0]);
                }
            }
        }

        private void mainForm_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void drawObjects(IEnumerable<INeuroProcess.NnRes> r)
        {
            var g = Graphics.FromImage(pictureBox1.Image);
            var brush = new SolidBrush(Color.FromArgb(20, 13, 255, 0));
            var border = new Pen(Color.FromArgb(200, 13, 255, 0));
            foreach (var f in r)
            {
                g.FillRectangle(brush, f.rect);
                f.rect.Inflate(1, 1);
                g.DrawRectangle(border, f.rect);
            }
            g.Dispose();
            pictureBox1.Refresh();
        }

        private void fillDefects(List<Rectangle> r)
        {
            var g = Graphics.FromImage(pictureBox1.Image);
            var brush = new SolidBrush(Color.FromArgb(20, 13, 255, 0));
            var border = new Pen(Color.FromArgb(200, 13, 255, 0));
            foreach (var f in r)
            {
                g.FillRectangle(brush, f);
                f.Inflate(1, 1);
                g.DrawRectangle(border, f);
            }
            g.Dispose();
            pictureBox1.Refresh();
        }

        private void pictureBox1_DoubleClick(object sender, EventArgs e)
        {
            if (pictureBox1.SizeMode == PictureBoxSizeMode.Zoom)
            {
                pictureBox1.SizeMode = PictureBoxSizeMode.Normal;
                pictureBox1.Cursor = Cursors.Hand;
            }
            else
            {
                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBox1.Cursor = Cursors.Default;
            }

        }

        private Point mouseDownStart;

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && pictureBox1.SizeMode == PictureBoxSizeMode.Normal)
            {
                var dx = mouseDownStart.X - e.Location.X;
                var dy = mouseDownStart.Y - e.Location.Y;
                if (dx != 0 || dy != 0)
                {
#pragma warning disable CS1690 // Доступ к члену в поле класса маршалинга по ссылке может вызвать исключение времени выполнения
                    pictureBox1.ImagePosition.Offset(-dx, -dy);
#pragma warning restore CS1690 // Доступ к члену в поле класса маршалинга по ссылке может вызвать исключение времени выполнения
                    pictureBox1.Invalidate();
                }
                mouseDownStart = e.Location;

            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                mouseDownStart = e.Location;
        }
         
        private void makeReport_Click(object sender, EventArgs e)
        {
            string r = database.Report();
            if (r != null)
            {
                File.WriteAllText("report.txt", r);
                Process.Start("notepad.exe", "report.txt");
            }
        }
    }
}
