using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using FaceMorph.Morph;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FaceMorph.Helpers
{
    public class VideoGenerator
    {
        VideoWriter videoWriter;
        string destinationPath;
        System.Drawing.Size sizeOfVid;
        private ObservableCollection<ImageDetails> images;

        public VideoGenerator(ImageDetails imgdet1, ImageDetails imgdet2, VectorOfPointF points1, VectorOfPointF points2, int fpsUser, float alphaUser, string path)
        {
            this.destinationPath = path;
            sizeOfVid = GetSizeOfImages(imgdet1, imgdet2);
            float alpha = 0.0f;
            MorphImage m;
            videoWriter = new VideoWriter(fileName: destinationPath, fps: fpsUser, size: sizeOfVid, isColor: true);
            while (alpha < 1.0f)
            {
                m = new MorphImage(imgdet1, imgdet2, points1, points2, alpha);
                Image<Bgr, byte> morphedImage = m.GetMorphedImageI();
                videoWriter.Write(morphedImage.Mat);
                alpha += alphaUser;
                morphedImage.Dispose();
            }
            if (videoWriter.IsOpened)
            {
                videoWriter.Dispose();
            }
            MessageBox.Show($"Completed");
        }


        /// <summary>
        /// Generates full video
        /// </summary>
        /// <param name="images"></param>
        public VideoGenerator(ObservableCollection<ImageDetails> images, int fpsUser, float alphaUser, string path)
        {
            this.images = images;
            this.destinationPath = path;
            ImagePreprocessor ip1;
            System.Drawing.Size tmpSize = new System.Drawing.Size(0, 0);

            for (int i = 0; i < images.Count - 1; i++)
            {
                ip1 = new ImagePreprocessor(images.ElementAt(i), images.ElementAt(i + 1));
                if (ip1.FacesListCurr.Count == 0)
                {
                    images.RemoveAt(i);
                }
                if (ip1.FacesListNext.Count == 0)
                {
                    images.RemoveAt(i+1);
                }
                else
                {
                    if (ip1.curr.ResizedImage.Width > tmpSize.Width)
                    {
                        tmpSize.Width = ip1.curr.ResizedImage.Width;
                    }
                    if (ip1.curr.ResizedImage.Height > tmpSize.Height)
                    {
                        tmpSize.Height = ip1.curr.ResizedImage.Width;
                    }

                }

            }
            if (tmpSize.Width > 1280)
            {
                tmpSize.Width = 1280;
                tmpSize.Height = 720;
            }

            MorphImage m;
            ImagePreprocessor ip;


            //videoWriter = new VideoWriter(fileName: destinationPath, fps: 30, size: tmpSize, isColor: true);
            videoWriter = new VideoWriter(fileName: destinationPath, compressionCode: VideoWriter.Fourcc('M', 'P', '4', 'V'), fps: fpsUser, size: tmpSize, isColor: true);


            List<Mat> frames = new List<Mat>();

            for (int i = 0; i < images.Count - 1; i++)
            {
                ip = new ImagePreprocessor(images.ElementAt(i), images.ElementAt(i + 1), tmpSize);

                float alpha = 0.0f;
                while (alpha < 1.0f)
                {
                    m = new MorphImage(ip.curr, ip.next, ip.ffpCurr, ip.ffpNext, alpha);
                    Image<Bgr, byte> morphedImage = m.GetMorphedImageI();
                    //CvInvoke.Imwrite($"testimages/img{alpha}.png", morphedImage.Mat);
                    frames.Add(morphedImage.Mat);
                    //videoWriter.Write(morphedImage.Mat);
                    alpha += alphaUser;
                    //morphedImage.Dispose();
                }




            }
            int img1 = 0;
            foreach (Mat mat in frames)
            {
                //CvInvoke.Imwrite($"testimages/img{img1}.png", mat);
                img1 += 1;
                videoWriter.Write(mat);
            }
            if (videoWriter.IsOpened)
            {
                videoWriter.Dispose();
            }
            MessageBox.Show($"Completed");
        }

        private System.Drawing.Size GetSizeOfImages(ImageDetails img1, ImageDetails img2)
        {
            int width, height;
            if (img1.ResizedImage.Width > img2.ResizedImage.Width)
            {
                width = img1.ResizedImage.Width;
            }
            else
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

            return new System.Drawing.Size(width, height);
        }
    }
}
