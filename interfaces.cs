using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace n_vision
{
    // Интерфейсы
    public abstract class INeuroProcess
    {
        public struct NnRes
        {
            public Rectangle rect;
            public string label;
            public float value; 
        }

        abstract public IEnumerable<NnRes> Process(Bitmap image);

    }

    public abstract class IDatabase
    {

    }

}
