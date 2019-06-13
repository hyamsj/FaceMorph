using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceMorph.Morph
{
    public class FaceInfoHolder
    {
        public int faceNum { get; set; }
        public VectorOfPointF ffp { get; set; }

        public FaceInfoHolder(int faceNum, VectorOfPointF ffp)
        {
            this.faceNum = faceNum;
            this.ffp = ffp;
        }
    }
}
