using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Images
{
    internal class NeuroModel : INeuroProcess
    {
        private InferenceSession session;
        NeuroModel(string modelFileName)
        {
            session = new InferenceSession(modelFileName);
        }

        public override IEnumerable<NnRes> Process(Bitmap image)
        {
            // params
            var threshold = 0.0f;
            var c = Color.Yellow;
            var font = new Font("Arial", 22);

            // inference session
            Console.WriteLine("Starting inference session...");
            var tic = Environment.TickCount;
            var inputMeta = session.InputMetadata;
            var imKey = inputMeta.Keys.First();
            var imValue = inputMeta.Values.First();
            var name = inputMeta.Keys.ToArray()[0];
            //var labels = File.ReadAllLines(prototxt);
            Console.WriteLine("Session started in " + (Environment.TickCount - tic) + " mls.");

            // image
            Console.WriteLine("Creating image tensor...");
            tic = Environment.TickCount;



            var width = imValue.Dimensions[2];
            var height = imValue.Dimensions[1];
            var dimentions = new int[] { 1, height, width, 1 };
            Console.WriteLine("Tensor was created in " + (Environment.TickCount - tic) + " mls.");

            // prediction
            Console.WriteLine("Detecting objects...");
            tic = Environment.TickCount;
            for (var y = 0; y < image.Height - height; y++)
            {
                var inputs = new List<NamedOnnxValue>() { };
                for (int x = 0; x < image.Width - width; x++)
                {
                    inputs.Add(NamedOnnxValue.CreateFromTensor(name, new DenseTensor<float>(ToGrayTensor(image, new Rectangle(x, 400, width, height)), dimentions)));
                }
                var results = session.Run(inputs);
                // dump the results
                foreach (var r in results)
                {
                    var b = r.AsTensor<float>().GetValue(0);
                    if (b  >0)
                    {
                        Console.WriteLine(r.Name + "\n");
                        Console.WriteLine(r.AsTensor<float>().GetArrayString());

                    }
                }
            }
            Console.WriteLine("Detecting was finished in " + (Environment.TickCount - tic) + " mls.");


            var result = new List<NnRes>();
            return result;
        }

        private static byte[] ToTensor(Bitmap data)
        {
            BitmapData bmData = data.LockBits(new Rectangle(0, 0, data.Width, data.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            byte[] rgb = ToTensor(bmData);
            data.UnlockBits(bmData);
            return rgb;
        }

        private static float[] ToGrayTensor(Bitmap data)
        {
            return ToGrayTensor(data, new Rectangle(0, 0, data.Width, data.Height));
        }

        private static float[] ToGrayTensor(Bitmap data, Rectangle pos)
        {
            BitmapData bmData = data.LockBits(pos, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            var rgb = ToGrayTensor(bmData);
            data.UnlockBits(bmData);
            return rgb;
        }

        private static float[] ToGrayTensor(BitmapData bmData)
        {
            // params
            int width = bmData.Width, height = bmData.Height, stride = bmData.Stride;
            var p = bmData.Scan0;
            var t = new float[1 * height * width];
            int pos = 0;

            // do job
            for (int j = 0; j < height; j++)
            {
                int k, jstride = j * stride;

                for (int i = 0; i < width; i++)
                {
                    k = jstride + i * 3;
                    t[pos++] = (Marshal.ReadByte(p+(k + 2)) + Marshal.ReadByte(p+(k + 1)) + Marshal.ReadByte(p+(k + 0)))/3;
                }
            }

            return t;
        }


        private static byte[] ToTensor(BitmapData bmData)
        {
            // params
            int width = bmData.Width, height = bmData.Height, stride = bmData.Stride;
            var p = bmData.Scan0;
            byte[] t = new byte[3 * height * width];
            int pos = 0;

            // do job
            for (int j = 0; j < height; j++)
            {
                int k, jstride = j * stride;

                for (int i = 0; i < width; i++)
                {
                    k = jstride + i * 3;

                    t[pos++] = Marshal.ReadByte(p+(k + 2));
                    t[pos++] = Marshal.ReadByte(p+(k + 1));
                    t[pos++] = Marshal.ReadByte(p+(k + 0));
                }
            }

            return t;
        }


    }
}
