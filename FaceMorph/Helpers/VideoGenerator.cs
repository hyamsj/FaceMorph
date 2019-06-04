using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FaceMorph.Helpers
{
    public class VideoGenerator
    {
        VideoWriter videoWriter;
        string destinationPath = @"C:\Users\joni\Desktop\testvideo.mp4";
        int FrameNo = 0;
        System.Drawing.Size sizeOfVid;

        public VideoGenerator(ImageDetails imgdet1, ImageDetails imgdet2, VectorOfPointF points1, VectorOfPointF points2)
        {

            sizeOfVid = GetSizeOfImages(imgdet1, imgdet2);
            float alpha = 0.0f;
            MorphImage m;
            videoWriter = new VideoWriter(fileName: destinationPath, fps: 60, size: sizeOfVid, isColor: true);
            while (alpha < 1.0f)
            {
                m = new MorphImage(imgdet1, imgdet2, points1, points2, alpha);
                Image<Bgr, byte> morphedImage = m.GetMorphedImageI();
                videoWriter.Write(morphedImage.Mat);
                alpha += 0.05f;
            }
            if(videoWriter.IsOpened)
            {
                videoWriter.Dispose();
            }
            MessageBox.Show($"Completed");
            //CvInvoke.Imwrite("testimages/videogen.png",x);

        }

        private System.Drawing.Size GetSizeOfImages(ImageDetails img1, ImageDetails img2)
        {
            int width, height;
            if (img1.ResizedImage.Width > img2.ResizedImage.Width)
            {
                width = img1.ResizedImage.Width;
            } else
            {
                width = img2.ResizedImage.Width;
            }

            if (img1.ResizedImage.Height > img2.ResizedImage.Height)
            {
                height = img1.ResizedImage.Height;
            }
            else
            {
                height = img2.ResizedImage.Height;
            }

            return new System.Drawing.Size(width,height); ;
        }
    }
}
