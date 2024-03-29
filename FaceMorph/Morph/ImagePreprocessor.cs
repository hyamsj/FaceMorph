﻿using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using FaceMorph.ViewsAndControllers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceMorph.Morph
{
    /// <summary>
    /// detects face and ffps before displaying images in the preview window
    /// </summary>
    public class ImagePreprocessor
    {

        FacemarkLBFParams facemarkParam;
        FacemarkLBF facemark;
        public VectorOfVectorOfPointF landmarksCurr { get; set; }
        public VectorOfVectorOfPointF landmarksNext { get; set; }
        public VectorOfPointF ffpCurr { get; set; } // points with landmarks
        public VectorOfPointF ffpNext { get; set; }

        Mat currImageMat = new Mat();
        Mat nextImageMat = new Mat();
        Rectangle[] facesArrCurr { get; set; }
        Rectangle[] facesArrNext { get; set; }

        public const int RECT_WIDTH = 5;
        public const double HAAR_SCALE_FACTOR = 1.4;
        public const int HAAR_SCALE_MIN_NEIGHBOURS = 6;
        public const double HAAR_MIN_FACE_FACTOR = 0.3;
        public const double HAAR_MAX_FACE_FACTOR = 0.8;

        public Triangle2DF[] delaunayTrianglesCurr { get; set; }
        public Triangle2DF[] delaunayTrianglesNext { get; set; }
        PointF[] ptsCurr, ptsNext;

        public List<Rectangle> FacesListCurr { get; set; }
        public List<Rectangle> FacesListNext { get; set; }
        public Image<Bgr, byte> CurrImageI { get; set; }
        public Image<Bgr, byte> NextImageI { get; set; }

        public ImageDetails curr { get; set; }
        public ImageDetails next { get; set; }

        public bool MorphEnabled { get; set; }

        //public ImagePreprocessor(Image<Bgr, byte> currImageI, Image<Bgr, byte> nextImageI)
        public ImagePreprocessor(ImageDetails curr, ImageDetails next)
        {
            this.curr = curr;
            this.next = next;

            this.CurrImageI = new Image<Bgr, byte>(curr.Title);
            this.NextImageI = new Image<Bgr, byte>(next.Title);
            //CvInvoke.Imwrite("testimages/test03.jpg", CurrImageI);

            DetectFace();
            ResizeImage();
            DetectFace();
            if ((facesArrCurr.Length > 0) && (facesArrNext.Length > 0))
            {
                MorphEnabled = true;
                FindFacialFeaturePoints();
                CreateDelaunay();
            }
            else
            {
                MorphEnabled = false;
            }
            Console.WriteLine(facesArrCurr.Length);
            Console.WriteLine(facesArrNext.Length);
            // DetectFaceInfo();
        }

        public ImagePreprocessor(ImageDetails curr, ImageDetails next, System.Drawing.Size sizeOfImage)
        {
            this.curr = curr;
            this.next = next;

            this.CurrImageI = new Image<Bgr, byte>(curr.Title);
            this.NextImageI = new Image<Bgr, byte>(next.Title);
            //CvInvoke.Imwrite("testimages/test03.jpg", CurrImageI);

            DetectFace();
            ResizeImage2(sizeOfImage);
            DetectFace();
            if ((facesArrCurr.Length > 0) && (facesArrNext.Length > 0))
            {
                MorphEnabled = true;
                FindFacialFeaturePoints();
                CreateDelaunay();
            }
            else
            {
                MorphEnabled = false;
            }
            Console.WriteLine(facesArrCurr.Length);
            Console.WriteLine(facesArrNext.Length);
            // DetectFaceInfo();
        }

        private void ResizeImage2(System.Drawing.Size sizeOfImage)
        {
            double resizeFactor = 0.4;
            if (facesArrCurr.Length > 0 && facesArrNext.Length > 0)
            {
                Rectangle rectCurr = facesArrCurr[0]; // todo: change
                int widthC = rectCurr.Width;
                widthC = (int)(widthC * resizeFactor);
                int heightC = rectCurr.Height;
                heightC = (int)(heightC * resizeFactor);
                rectCurr.Inflate(widthC, heightC);
                CurrImageI.ROI = rectCurr;

                Rectangle rectNext = facesArrNext[0];
                int widthN = rectNext.Width;
                widthN = (int)(widthN * resizeFactor);
                int heightN = rectNext.Height;
                heightN = (int)(heightN * resizeFactor);
                rectNext.Inflate(widthN, heightN);
                NextImageI.ROI = rectNext;
            }

            

            // --------------------------------------

            // resize image: todo -> do while morphing, not during face detect step (maybe)
            System.Drawing.Size currImageSize = new System.Drawing.Size(CurrImageI.Width, CurrImageI.Height);
            System.Drawing.Size nextImageSize = new System.Drawing.Size(NextImageI.Width, NextImageI.Height);

            // downscale to 1080p
            if (currImageSize.Height > 1080 || currImageSize.Width > 1920)
            {
                var tmp = CurrImageI.Mat;
                currImageMat = GetSquareImage(tmp, 1920);
                CurrImageI = currImageMat.ToImage<Bgr, byte>();
            }

            if (nextImageSize.Height > 1080 || nextImageSize.Width > 1920)
            {
                var tmp = NextImageI.Mat;
                nextImageMat = GetSquareImage(tmp, 1920);
                NextImageI = nextImageMat.ToImage<Bgr, byte>();
            }


            if (currImageSize.Height > nextImageSize.Height || currImageSize.Width > nextImageSize.Width)
            {
                var tmp = CurrImageI.Mat;
                currImageMat = GetSquareImage(tmp, NextImageI.Width);
                CurrImageI = currImageMat.ToImage<Bgr, byte>();
            }
            else
            {
                var tmp = NextImageI.Mat;
                nextImageMat = GetSquareImage(tmp, CurrImageI.Width);
                NextImageI = nextImageMat.ToImage<Bgr, byte>();
            }

            // -> resize
            CurrImageI = CurrImageI.Resize(sizeOfImage.Width, sizeOfImage.Height, Emgu.CV.CvEnum.Inter.Linear);
            NextImageI = NextImageI.Resize(sizeOfImage.Width, sizeOfImage.Height, Emgu.CV.CvEnum.Inter.Linear);

            this.curr.ResizedImage = CurrImageI;
            this.next.ResizedImage = NextImageI;
        }

        private void ResizeImage()
        {
            double resizeFactor = 0.4;
            if (facesArrCurr.Length > 0 && facesArrNext.Length > 0)
            {
                Rectangle rectCurr = facesArrCurr[0]; // todo: change
                int widthC = rectCurr.Width;
                widthC = (int)(widthC * resizeFactor);
                int heightC = rectCurr.Height;
                heightC = (int)(heightC * resizeFactor);
                rectCurr.Inflate(widthC, heightC);
                CurrImageI.ROI = rectCurr;

                Rectangle rectNext = facesArrNext[0];
                int widthN = rectNext.Width;
                widthN = (int)(widthN * resizeFactor);
                int heightN = rectNext.Height;
                heightN = (int)(heightN * resizeFactor);
                rectNext.Inflate(widthN, heightN);
                NextImageI.ROI = rectNext;
            }

            // --------------------------------------

            // resize image: todo -> do while morphing, not during face detect step (maybe)
            System.Drawing.Size currImageSize = new System.Drawing.Size(CurrImageI.Width, CurrImageI.Height);
            System.Drawing.Size nextImageSize = new System.Drawing.Size(NextImageI.Width, NextImageI.Height);

            // downscale to 1080p
            if (currImageSize.Height > 1080 || currImageSize.Width > 1920)
            {
                var tmp = CurrImageI.Mat;
                currImageMat = GetSquareImage(tmp, 1920);
                CurrImageI = currImageMat.ToImage<Bgr, byte>();
            }

            if (nextImageSize.Height > 1080 || nextImageSize.Width > 1920)
            {
                var tmp = NextImageI.Mat;
                nextImageMat = GetSquareImage(tmp, 1920);
                NextImageI = nextImageMat.ToImage<Bgr, byte>();
            }


            if (currImageSize.Height > nextImageSize.Height || currImageSize.Width > nextImageSize.Width)
            {
                var tmp = CurrImageI.Mat; 
                currImageMat = GetSquareImage(tmp, NextImageI.Width);
                CurrImageI = currImageMat.ToImage<Bgr, byte>();
            }
            else
            {
                var tmp = NextImageI.Mat; 
                nextImageMat = GetSquareImage(tmp, CurrImageI.Width);
                NextImageI = nextImageMat.ToImage<Bgr, byte>();
            }

            this.curr.ResizedImage = CurrImageI;
            this.next.ResizedImage = NextImageI;
        }

        private void DetectFace()
        {
            string facePath;
            try
            {
                // get face detect dataset
                facePath = Path.GetFullPath(@"data/haarcascade_frontalface_default.xml");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }


            CascadeClassifier classifierFace = new CascadeClassifier(facePath);
            Image<Gray, byte> imgGrayCurr = CurrImageI.Convert<Gray, byte>().Clone();
            Image<Gray, byte> imgGrayNext = NextImageI.Convert<Gray, byte>().Clone();

            int minWidthCurr = (int)(CurrImageI.Width * HAAR_MIN_FACE_FACTOR);
            int minHeightCurr = (int)(CurrImageI.Height * HAAR_MIN_FACE_FACTOR);
            int maxWidthCurr = (int)(CurrImageI.Width * HAAR_MAX_FACE_FACTOR);
            int maxHeightCurr = (int)(CurrImageI.Height * HAAR_MAX_FACE_FACTOR);

            int minWidthNext = (int)(NextImageI.Width * HAAR_MIN_FACE_FACTOR);
            int minWHeightNext = (int)(NextImageI.Height * HAAR_MIN_FACE_FACTOR);
            int maxWidthNext = (int)(NextImageI.Width * HAAR_MAX_FACE_FACTOR);
            int maxHeightNext = (int)(NextImageI.Height * HAAR_MAX_FACE_FACTOR);

            System.Drawing.Size minSizeCurr = new System.Drawing.Size(minWidthCurr, minHeightCurr);
            System.Drawing.Size maxSizeCurr = new System.Drawing.Size(maxWidthCurr, maxHeightCurr);

            System.Drawing.Size minSizeNext = new System.Drawing.Size(minWidthNext, minWHeightNext);
            System.Drawing.Size maxSizeNext = new System.Drawing.Size(maxWidthNext, maxHeightNext);


            // Detect Faces
            facesArrCurr = classifierFace.DetectMultiScale(imgGrayCurr, HAAR_SCALE_FACTOR, HAAR_SCALE_MIN_NEIGHBOURS, minSizeCurr, maxSizeCurr);
            facesArrNext = classifierFace.DetectMultiScale(imgGrayNext, HAAR_SCALE_FACTOR, HAAR_SCALE_MIN_NEIGHBOURS, minSizeNext, maxSizeNext);

            FacesListCurr = facesArrCurr.ToList<Rectangle>();
            FacesListNext = facesArrNext.ToList<Rectangle>();

        }

        private void CreateDelaunay()
        {
            // Delaunay
            if (facesArrNext != null && facesArrCurr != null)
            {
                using (VectorOfPointF vpfCurr = ffpCurr)
                using (VectorOfPointF vpfNext = ffpNext)
                {
                    ptsCurr = vpfCurr.ToArray();
                    ptsNext = vpfNext.ToArray();

                    using (Subdiv2D subdivisionLeft = new Subdiv2D(ptsCurr))
                    using (Subdiv2D subdivisionRight = new Subdiv2D(ptsNext))
                    {
                        //Obtain the delaunay's triangulation from the set of points;
                        delaunayTrianglesCurr = subdivisionLeft.GetDelaunayTriangles();
                        delaunayTrianglesNext = subdivisionRight.GetDelaunayTriangles();
                    }
                }

            }

        }



        private void FindFacialFeaturePoints()
        {
            string facePath;
            try
            {
                
                // get face detect dataset
                facePath = Path.GetFileName(@"data/haarcascade_frontalface_default.xml");

                // get FFP dataset
                facemarkParam = new FacemarkLBFParams();
                facemark = new FacemarkLBF(facemarkParam);
                facemark.LoadModel(@"data/lbfmodel.yaml");
            }

            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }

            // initialize imageMat
            currImageMat = CurrImageI.Mat;
            nextImageMat = NextImageI.Mat;

            // Current Face
            FacesListCurr = facesArrCurr.OfType<Rectangle>().ToList();

            // Find facial feature points
            VectorOfRect vrLeft = new VectorOfRect(facesArrCurr);
            landmarksCurr = new VectorOfVectorOfPointF();

            facemark.Fit(currImageMat, vrLeft, landmarksCurr);
            ffpCurr = landmarksCurr[curr.SelectedFace];


            // Next Face
            FacesListNext = facesArrNext.OfType<Rectangle>().ToList();

            // Find facial feature points
            VectorOfRect vrRight = new VectorOfRect(facesArrNext);
            landmarksNext = new VectorOfVectorOfPointF();

            facemark.Fit(nextImageMat, vrRight, landmarksNext);
            ffpNext = landmarksNext[next.SelectedFace];

            // Add Corner points
            ffpCurr = AddCornerPoints(ffpCurr, this.curr.ResizedImage.Mat);
            ffpNext = AddCornerPoints(ffpNext, this.next.ResizedImage.Mat);


        }

        public VectorOfPointF AddCornerPoints(VectorOfPointF points, Mat img)
        {
            if (points.Size < 76)
            {
                int width = img.Width;
                int height = img.Height;

                // top left
                PointF[] p0 = { new PointF(0, 0) };
                points.Push(p0);

                // top center
                PointF[] p1 = { new PointF((width / 2) - 1, 0) };
                points.Push(p1);

                // top right
                PointF[] p2 = { new PointF(width - 1, 0) };
                points.Push(p2);

                // center right
                PointF[] p3 = { new PointF(width - 1, (height / 2) - 1) };
                points.Push(p3);

                // bottom right
                PointF[] p4 = { new PointF(width - 1, height - 1) };
                points.Push(p4);

                // bottom center
                PointF[] p5 = { new PointF((width / 2) - 1, height - 1) };
                points.Push(p5);

                // bottom left
                PointF[] p6 = { new PointF(0, height - 1) };
                points.Push(p6);

                //center left
                PointF[] p7 = { new PointF(0, (height / 2) - 1) };
                points.Push(p7);


            }
            return points;


        }

        public Mat GetSquareImage(Mat img, int targetWidth)
        {
            int width = img.Cols;
            int height = img.Rows;

            if (targetWidth > 1920)
            {
                targetWidth = 1920;
            }

            Mat square = Mat.Zeros(targetWidth, targetWidth, Emgu.CV.CvEnum.DepthType.Cv8U, 3);

            int maxDim = (width >= height) ? width : height;
            float scale = ((float)targetWidth) / maxDim;
            Rectangle roi = new Rectangle();
            if (width >= height)
            {
                roi.Width = targetWidth;
                roi.X = 0;
                roi.Height = (int)(height * scale);
                roi.Y = (targetWidth - roi.Height) / 2;
            }
            else
            {
                roi.Y = 0;
                roi.Height = targetWidth;
                roi.Width = (int)(width * scale);
                roi.X = (targetWidth - roi.Width) / 2;
            }
            CvInvoke.Resize(img, square, roi.Size);
            return square;

        }

        public void UpdateSelectedFace(int currSelectedFace, int nextSelectedFace)
        {
            ffpCurr = landmarksCurr[currSelectedFace];
            ffpNext = landmarksNext[nextSelectedFace];
        }
    }
}
