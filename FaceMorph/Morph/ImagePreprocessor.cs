using Emgu.CV;
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
    /// detects face ffp, 
    /// </summary>
    public class ImagePreprocessor
    {

        FacemarkLBFParams facemarkParam;
        FacemarkLBF facemark;
        VectorOfVectorOfPointF landmarksCurr;
        VectorOfVectorOfPointF landmarksNext;
        public VectorOfPointF ffpCurr { get; set; } // points with landmarks
        public VectorOfPointF ffpNext { get; set; }

        Mat currImageMat = new Mat();
        Mat nextImageMat = new Mat();
        Rectangle[] facesArrCurr;
        Rectangle[] facesArrNext;

        public const int RECT_WIDTH = 5;
        public const double HAAR_SCALE_FACTOR = 1.05;
        public const int HAAR_SCALE_MIN_NEIGHBOURS = 4;
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

        //public ImagePreprocessor(Image<Bgr, byte> currImageI, Image<Bgr, byte> nextImageI)
        public ImagePreprocessor(ImageDetails curr, ImageDetails next)
        {
            this.curr = curr;
            this.next = next;

            this.CurrImageI = new Image<Bgr, byte>(curr.Title);
            this.NextImageI = new Image<Bgr, byte>(next.Title);
            //CvInvoke.Imwrite("testimages/test03.jpg", CurrImageI);

            DetectFaceInfo();
        }

        public void DetectFaceInfo()
        {
            string facePath;
            try
            {
                // get face detect dataset
                facePath = Path.GetFullPath(@"../../data/haarcascade_frontalface_default.xml");

                // get FFP dataset
                facemarkParam = new FacemarkLBFParams();
                facemark = new FacemarkLBF(facemarkParam);
                facemark.LoadModel(@"../../data/lbfmodel.yaml");
            }

            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            // initialize imageMat
            currImageMat = CurrImageI.Mat;
            nextImageMat = NextImageI.Mat;

            // resize image: todo -> do while morphing, not during face detect step (maybe)
            System.Drawing.Size currImageSize = new System.Drawing.Size(CurrImageI.Width, CurrImageI.Height);
            System.Drawing.Size nextImageSize = new System.Drawing.Size(NextImageI.Width, NextImageI.Height);

            if (currImageSize.Height > nextImageSize.Height || currImageSize.Width > nextImageSize.Width)
            {

                var tmp = CurrImageI.Mat; // unnecessary copy
                currImageMat = GetSquareImage(tmp, NextImageI.Width);
                CurrImageI = currImageMat.ToImage<Bgr, byte>();
                //CvInvoke.Resize(currImageI, currImageI, nextImageSize);
                //CvInvoke.Imwrite("testimages/currImageIResized.jpg", currImageMat);

            }
            else
            {
                var tmp = NextImageI.Mat; // unnecessary copy
                nextImageMat = GetSquareImage(tmp, CurrImageI.Width);
                NextImageI = nextImageMat.ToImage<Bgr, byte>();
                //CvInvoke.Resize(nextImageI, nextImageI, currImageSize);
                //CvInvoke.Imwrite("testimages/nextImageIresized.jpg", nextImageMat);
            }

            this.curr.ResizedImage = CurrImageI;
            this.next.ResizedImage = NextImageI;
            //CvInvoke.Imwrite("testimages/test01.jpg", CurrImageI);

            CascadeClassifier classifierFace = new CascadeClassifier(facePath);
            Image<Gray, byte> imgGrayCurr = CurrImageI.Convert<Gray, byte>().Clone();
            Image<Gray, byte> imgGrayNext = NextImageI.Convert<Gray, byte>().Clone();


            // defines size of face in picture to be found 
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

            FindFacialFeaturePoints();

            CreateDelaunay();

            
            //DrawFFPCurr();
            //DrawFFPNext();
            //DrawDelaunayCurr();
            //DrawDelaunayNext();
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
            // if no faces found, send error to user -> todo cleanup
            if (facesArrCurr != null && facesArrCurr.Length > 0)
            {
                FacesListCurr = facesArrCurr.OfType<Rectangle>().ToList();

                // Find facial feature points
                VectorOfRect vrLeft = new VectorOfRect(facesArrCurr);
                landmarksCurr = new VectorOfVectorOfPointF();

                facemark.Fit(currImageMat, vrLeft, landmarksCurr);
                ffpCurr = landmarksCurr[curr.SelectedFace];

            }
            if (facesArrNext != null && facesArrNext.Length > 0)
            {
                FacesListNext = facesArrNext.OfType<Rectangle>().ToList();

                // Find facial feature points
                VectorOfRect vrRight = new VectorOfRect(facesArrNext);
                landmarksNext = new VectorOfVectorOfPointF();

                facemark.Fit(nextImageMat, vrRight, landmarksNext);
                ffpNext = landmarksNext[next.SelectedFace]; // todo: user needs to be able to choose face
            }
            else
            {
                throw new Exception("no face found");
            }
        }

        private Mat GetSquareImage(Mat img, int targetWidth)
        {
            int width = img.Cols;
            int height = img.Rows;

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
