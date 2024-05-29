using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms; 

namespace n_vision
{
 
    public partial class MainForm : Form
    {
        INeuroProcess processor;

        public MainForm()
        {
            InitializeComponent();
            processor = new Eye();
        }

        private void open_Click(object sender, EventArgs e)
        {
            var d = new OpenFileDialog();
            d.Filter =  "Изображения|*.jpg;*.png";
            if (d.ShowDialog() == DialogResult.OK)
            {
                openFile(d.FileName); 
            }
        }

        private readonly InferenceSession session;

  
       
        void openFile(String filename)
        {
            try
            {
                toolStripStatusLabel1.Text = filename;
                pictureBox1.Image = Image.FromFile(filename);
                workLabel.Text = "Поиск дефектов";
                ThreadPool.QueueUserWorkItem((o) =>
                {
                    var res = processor.Process(new Bitmap(Image.FromFile(filename)));  
                    this.BeginInvoke(new Action(() =>
                    {
                        drawObjects(res);
                        workLabel.Text = "Готово";
                    }));
                });

            }
            catch (Exception)
            {

            }
        }

        private void showDiffMap_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null || pictureBox1.Image.Size == Size.Empty)
                return;
            var map = Eye.getDiffMap(pictureBox1.Image);
            // нарисуем свою картинку из мапы
            var bmp = new Bitmap(map.GetUpperBound(0)+1, map.GetUpperBound(1)+1, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            {
                var data = bmp.LockBits(rect: new Rectangle(Point.Empty, bmp.Size), ImageLockMode.ReadWrite, bmp.PixelFormat);
                var pos = data.Scan0;
                var lengthBytes = data.Height * data.Stride - 1;
                var endPos = data.Scan0 + lengthBytes;
                byte[] r = new byte[lengthBytes/4+1];
                byte[] g = new byte[lengthBytes/4+1];
                byte[] b = new byte[lengthBytes/4+1];
                var i = 0;
                var x = 0; var y = 0;
                var width = bmp.Width;
                // есть варианты
                while (pos.ToInt64() < endPos.ToInt64())
                {
                    if (map[x, y] > 80)
                    {
                        Marshal.WriteByte(pos+0, 10); //B
                        Marshal.WriteByte(pos+1, 255); //G
                        Marshal.WriteByte(pos+2, 200);   //R
                        Marshal.WriteByte(pos+3, 255);
                    }
                    else
                    {
                        Marshal.WriteByte(pos+3, 255);
                    }
                    pos+= 4;
                    x += 1;
                    if (x >= width)
                    {
                        x = 0;
                        y += 1;
                    }
                }
                bmp.UnlockBits(data);
            }
            bmp.Save(toolStripStatusLabel1.Text+"-diff.jpg", ImageFormat.Jpeg);
            pictureBox1.Image = bmp;
            pictureBox1.Refresh();

        }

        private void mainForm_Load(object sender, EventArgs e)
        {

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
                if (f != null && f.Length == 1 &&  isAllowedExtension(f[0]))
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


        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
