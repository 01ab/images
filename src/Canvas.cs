using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Images
{
    public class Canvas : PictureBox
    {
        public Point ImagePosition = new Point();

        public Canvas()
        {
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (Image == null)
            {
                return;
            }
            if (SizeMode == PictureBoxSizeMode.Normal)
            {
                e.Graphics.Clear(BackColor);
                e.Graphics.DrawImage(Image, ImagePosition);
            }
            else
            {
                base.OnPaint(e);
            }
        }

    }

}
