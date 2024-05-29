using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using static n_vision.INeuroProcess;

namespace n_vision
{
    internal class Eye : INeuroProcess
    {
        private static Bitmap ConvertTo32Rgb(Image image)
        {
            var img = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppRgb);
            var g = Graphics.FromImage(img);
            g.DrawImage(image, 0, 0, image.Width, image.Height);
            g.Dispose();
            return img;
        }

        private static void Sort(byte[] a)
        {
            int[] counts = new int[256];
            for (int i = 0; i < a.Length; i++)
                counts[a[i]]++;
            int k = 0;
            for (int i = 0; i < counts.Length; i++)
                for (int j = 0; j < counts[i]; j++)
                    a[k++] = (byte)i;
        }

        private static Color ScanMedianColor(BitmapData data)
        {
            var pos = data.Scan0;
            var lengthBytes = data.Height * data.Stride - 1;
            var endPos = data.Scan0 + lengthBytes;
            byte[] r = new byte[lengthBytes / 4 + 1];
            byte[] g = new byte[lengthBytes / 4 + 1];
            byte[] b = new byte[lengthBytes / 4 + 1];
            var i = 0;
            // есть варианты
            while (pos.ToInt64() < endPos.ToInt64())
            {
                b[i] = Marshal.ReadByte(pos + 0);
                g[i] = Marshal.ReadByte(pos + 1);
                r[i] = Marshal.ReadByte(pos + 2);
                pos += 4;
                i++;
            }
            Sort(r);
            Sort(g);
            Sort(b);
            return Color.FromArgb(r[r.Length / 2], g[g.Length / 2], b[b.Length / 2]);
        }

        public static float GetDiff(Color a, Color b)
        {
            return GetDiff(new byte[] { a.R, a.G, a.B }, new byte[] { b.R, b.G, b.B });
        }

        public static float GetDiff(byte[] rgb1, byte[] rgb2)
        {
            var dr = (float)Math.Abs(rgb1[0] - rgb2[0]) / 255;
            var dg = (float)Math.Abs(rgb1[1] - rgb2[1]) / 255;
            var db = (float)Math.Abs(rgb1[2] - rgb2[2]) / 255;
            return (dr + dg + db) / 3;
        }

        public static byte[,] getDiffMap(Image image)
        {
            var img = ConvertTo32Rgb(image);
            var data = img.LockBits(rect: new Rectangle(Point.Empty, img.Size), ImageLockMode.ReadOnly, img.PixelFormat);
            var m = ScanMedianColor(data);            // получили некий медианный цвет в rgb 
            var bgr = new byte[3] { m.B, m.G, m.R };
            var diff = new byte[img.Width, img.Height];
            var height = img.Height;
            var width = img.Width;
            byte[] tmp = new byte[4];
            // теперь ищем области которые сильно отличаются от него
            for (int y = 0; y < height; y++)
            {
                var pos = data.Scan0 + y * data.Stride;
                for (int x = 0; x < width; x++)
                {
                    Marshal.Copy(pos, tmp, 0, 4);
                    diff[x, y] = (byte)(GetDiff(bgr, tmp) * 255);
                    pos += 4;
                }
            }
            return diff;
        }


        public override IEnumerable<NnRes> Process(Bitmap image)
        {
            List<NnRes> res = new List<NnRes>();
            var t = LocateSquares(image, 0.000015, 0.4);
            foreach (var n in t)
            {
                res.Add(new NnRes() { rect = n, label = "obj", value = 1f });
            }
            return res;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcImage"></param>
        /// <param name="minSquare">В % от площади всей картинки</param>
        /// <param name="maxSquare">В % от площади всей картинки</param>
        /// <returns></returns>
        public static List<Rectangle> LocateSquares(Image srcImage, double minSquare, double maxSquare)
        {
            List<Rectangle> result = new List<Rectangle>();
            var map = getDiffMap(srcImage);
            var fullSquare = srcImage.Width * srcImage.Height;
            minSquare = fullSquare * minSquare;
            maxSquare = fullSquare * maxSquare;
            var distance = (int)(Math.Sqrt(minSquare) / 2.5);
            while (true)
            {
                var (res, rc) = SelectClosedPoints(ref map, 90, 70, distance);
                if (res == null)
                    break;
                //var rc = selectRectangle(ref res);
                var sq = rc.Width * rc.Height;
                if (sq >= minSquare && sq <= maxSquare)
                    result.Add(rc);
                // зануляем проверенные в map
                Clear(ref res, ref map, rc);
            }
            return result;
        }

        // все не нулевые в data обнуляют в map
        private static void Clear(ref byte[,] data, ref byte[,] map, Rectangle rec)
        {
            for (int x = rec.X; x < rec.X + rec.Width; x++)
                for (int y = rec.Y; y < rec.Y + rec.Height; y++)
                    if (data[x, y] != 0)
                        map[x, y] = 0;
        }

        private static Rectangle selectRectangle(ref byte[,] data)
        {
            var p1 = new Point { };
            var p2 = new Point { };
            // ищем верхнюю точку
            for (int y = data.GetLowerBound(1); y <= data.GetUpperBound(1); y++)
                for (int x = data.GetLowerBound(0); x <= data.GetUpperBound(0); x++)
                    if (data[x, y] != 0)
                    {
                        p1.Y = y; p1.X = x;
                        p2 = p1;
                        y = int.MaxValue - 1;
                        break;
                    }

            // ищем левую точку
            for (int x = data.GetLowerBound(0); x <= p1.X; x++)
                for (int y = data.GetLowerBound(1); y <= data.GetUpperBound(1); y++)
                    if (data[x, y] != 0)
                    {
                        p1.X = x;
                        x = int.MaxValue - 1;
                        break;
                    }

            // ищем правую точку
            for (int x = data.GetUpperBound(0); x > p2.X; x--)
                for (int y = data.GetLowerBound(1); y <= data.GetUpperBound(1); y++)
                    if (data[x, y] != 0)
                    {
                        p2.X = x;
                        x = int.MinValue + 1;
                        break;
                    }
            // ищем нижнюю точку
            for (int y = data.GetUpperBound(1); y > p2.Y; y--)
                for (int x = data.GetLowerBound(0); x <= data.GetUpperBound(0); x++)
                    if (data[x, y] != 0)
                    {
                        p2.Y = y;
                        y = int.MinValue + 1;
                        break;
                    }
            return new Rectangle(p1, new Size(p2.X - p1.X + 1, p2.Y - p1.Y + 1));
        }

        private static (int, int) FindPoint(ref byte[,] data, byte treshold)
        {
            for (int x = data.GetLowerBound(0); x <= data.GetUpperBound(0); x++)
                for (int y = data.GetLowerBound(1); y <= data.GetUpperBound(1); y++)
                {
                    if (data[x, y] >= treshold)
                        return (x, y);
                }
            return (-1, -1);
        }

        private static Point[] MakeCirclePoints(int radius)
        {
            var circles = new List<Point>();
            var ll = radius ^ 2;
            //  радиус, берём квадрат, от -радиус, до +радиус, записываем туда координаты если они входят внутрь круга
            for (int x = -radius; x <= radius; x++)
                for (int y = -radius; y <= radius; y++)
                {
                    if ((x ^ 2 + y ^ 2) <= ll)
                        circles.Add(new Point(x, y));
                }
            return circles.ToArray();
        }

        private static void CheckPoints(ref byte[,] sourceData, ref byte[,] targetData, byte leaveTreshold, ref Point[] points, int baseX, int baseY, ref Rectangle rect)
        {
            var pts = new Queue<Point>();
            pts.Enqueue(new Point(baseX, baseY));
            var minX = targetData.GetLowerBound(0);
            var maxX = targetData.GetUpperBound(0);
            var minY = targetData.GetLowerBound(1);
            var maxY = targetData.GetUpperBound(1);

            var resMaxX = minX;
            var resMinX = maxX;
            var resMaxY = minY;
            var resMinY = maxY;

            while (pts.Count() > 0)
            {
                var t = pts.Dequeue();
                foreach (var point in points)
                {
                    var x = point.X + t.X;
                    var y = point.Y + t.Y;
                    if (x < minX || y < minY || x > maxX || y > maxY)
                        continue;

                    if (targetData[x, y] != 0) continue;
                    if (sourceData[x, y] > leaveTreshold)
                    {
                        targetData[x, y] = 255;
                        if (!point.IsEmpty)
                            pts.Enqueue(new Point(x, y));
                        if (resMaxX < x) resMaxX = x;
                        if (resMinX > x) resMinX = x;
                        if (resMaxY < y) resMaxY = y;
                        if (resMinY > y) resMinY = y;
                    }
                    else
                    {
                        targetData[x, y] = 1; // типа проверили 
                    }
                }
            }
            rect = new Rectangle(resMinX, resMinY, resMaxX - resMinX + 1, resMaxY - resMinY + 1);
        }

        private static (byte[,], Rectangle) SelectClosedPoints(ref byte[,] data, byte watchTreshold, byte leaveTreshold, int distance)
        {
            var (x, y) = FindPoint(ref data, watchTreshold);
            if (x < 0 || y < 0)
                return (null, Rectangle.Empty);

            var width = data.GetUpperBound(0) + 1;
            var height = data.GetUpperBound(1) + 1;
            var result = new byte[width, height];
            var rect = new Rectangle();
            // алгоритм простой, идём по дате, находим watchTreshold и объеденяем все точки вокруг на расстоянии distance которые >= leaveTreshold
            // будем запоминать примерно так, если в результате = 1 - точку проверили, она ниже leaveTreshold, 0 ещё не проверили, 255 - проверили , выше leaveTreshold, и вокруг неё тоже надо проверить всё 
            // будем делать рекурсивненько
            // сначала сделаем массив с относительными координатами элементов которые будем проверять
            var checkField = MakeCirclePoints(distance);

            // проверяем  красим всё в result в 255
            CheckPoints(ref data, ref result, leaveTreshold, ref checkField, x, y, ref rect);

            return (result, rect);
        }



    }
}
