using Emgu.CV;
using System;
using Emgu.CV.Structure;
using Emgu.CV.Util;

using System.Drawing;
using System.Windows;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using Size = System.Drawing.Size;

namespace FaceMorph.Helpers
{
    public class MorphImage
    {
        public int count = 0;
        public float alpha = 0.5f;
        public float sign = 1.0f;

        Mat img1 = new Mat();
        Mat img2 = new Mat();
        Mat imgM; 

        VectorOfVectorOfInt triangleIndexes = new VectorOfVectorOfInt();

        VectorOfPointF points1 = new VectorOfPointF();
        VectorOfPointF points2 = new VectorOfPointF();
        VectorOfPointF pointsM = new VectorOfPointF();

        public MorphImage(Mat img1, Mat img2, VectorOfPointF points1, VectorOfPointF points2, float alpha)
        {
            this.img1 = img1;
            this.img2 = img2;
            this.alpha = alpha;

            img1.ConvertTo(img1, Emgu.CV.CvEnum.DepthType.Cv32F);
            img2.ConvertTo(img2, Emgu.CV.CvEnum.DepthType.Cv32F);

            this.points1 = points1;
            this.points2 = points2;

            // Add Points for whole image
            points1 = AddCornerPoints(points1, img1);
            points2 = AddCornerPoints(points2, img2);

            // Create an instance of Subdiv2D
            Rectangle rect = new Rectangle(0, 0, img1.Size.Width, img1.Size.Height);
            Subdiv2D subdiv = new Subdiv2D(rect);

            // Create and Draw the Delaunay triangulation
            triangleIndexes = new VectorOfVectorOfInt();
            CreateDelaunay(ref img1, ref subdiv, ref points1, false, ref triangleIndexes);

            //// Draw the Delaunay triangulation of face 1
            //Mat img1D = img1.Clone();
            //DrawDelaunay(ref img1D, ref subdiv, new MCvScalar(255, 255, 255));


            //// Draw the Delaunay triangulation of face 2
            //Mat img2D = img2.Clone();
            //DrawDelaunay(ref img2D, ref points2, triangleIndexes, new MCvScalar(255, 255, 255));
            //img2D.ConvertTo(img2D, Emgu.CV.CvEnum.DepthType.Cv8U);

            //compute weighted average point coordinates
            pointsM = new VectorOfPointF();
            for (int i = 0; i < points1.Size; i++)
            {
                float x = (1 - alpha) * points1[i].X + alpha * points2[i].X;
                float y = (1 - alpha) * points1[i].Y + alpha * points2[i].Y;
                PointF[] pf = { new PointF(x, y) };
                pointsM.Push(pf);
            }

            //empty image for morphed face
            imgM = Mat.Zeros(img1.Rows, img1.Cols, Emgu.CV.CvEnum.DepthType.Cv32F, 3);

            for (int i = 0; i < triangleIndexes.Size; i++)
            {
                VectorOfPointF t1 = new VectorOfPointF();
                VectorOfPointF t2 = new VectorOfPointF();
                VectorOfPointF tM = new VectorOfPointF();

                PointF ppft10 = points1[triangleIndexes[i][0]];
                PointF ppft11 = points1[triangleIndexes[i][1]];
                PointF ppft12 = points1[triangleIndexes[i][2]];
                PointF ppft20 = points2[triangleIndexes[i][0]];
                PointF ppft21 = points2[triangleIndexes[i][1]];
                PointF ppft22 = points2[triangleIndexes[i][2]];
                PointF ppftM0 = pointsM[triangleIndexes[i][0]];
                PointF ppftM1 = pointsM[triangleIndexes[i][1]];
                PointF ppftM2 = pointsM[triangleIndexes[i][2]];

                PointF[] pft10 = { new PointF(ppft10.X, ppft10.Y) };
                PointF[] pft11 = { new PointF(ppft11.X, ppft11.Y) };
                PointF[] pft12 = { new PointF(ppft12.X, ppft12.Y) };
                PointF[] pft20 = { new PointF(ppft20.X, ppft20.Y) };
                PointF[] pft21 = { new PointF(ppft21.X, ppft21.Y) };
                PointF[] pft22 = { new PointF(ppft22.X, ppft22.Y) };
                PointF[] pftM0 = { new PointF(ppftM0.X, ppftM0.Y) };
                PointF[] pftM1 = { new PointF(ppftM1.X, ppftM1.Y) };
                PointF[] pftM2 = { new PointF(ppftM2.X, ppftM2.Y) };

                t1.Push(pft10);
                t1.Push(pft11);
                t1.Push(pft12);
                t2.Push(pft20);
                t2.Push(pft21);
                t2.Push(pft22);
                tM.Push(pftM0);
                tM.Push(pftM1);
                tM.Push(pftM2);

                MorphTriangle(ref img1, ref img2, ref imgM, ref t1, ref t2, ref tM, alpha);
            }
            imgM.ConvertTo(imgM, Emgu.CV.CvEnum.DepthType.Cv8U);
            //CvInvoke.Imshow("Morphed Face", imgM);

        }

        


        // Draws the Delaunay triangualtion into an image using the Subdiv2D
        private void DrawDelaunay(ref Mat img1D, ref Subdiv2D subdiv, MCvScalar mCvScalar)
        {
            Triangle2DF[] delaunayTriangles = subdiv.GetDelaunayTriangles();
            foreach (Triangle2DF triangle in delaunayTriangles)
            {
                System.Drawing.Point[] vertices = Array.ConvertAll<PointF, System.Drawing.Point>(triangle.GetVertices(), System.Drawing.Point.Round);
                using (VectorOfPoint vp = new VectorOfPoint(vertices))
                {
                    CvInvoke.Polylines(img1D, vp, true, new Bgr(255, 255, 255).MCvScalar);
                }
            }
            img1D.ConvertTo(img1D, Emgu.CV.CvEnum.DepthType.Cv8U);
            CvInvoke.Imshow("Delaunay Triangulation", img1D);

        }

        // Draws the Delaunay triangulation into an image using the triangle indexes
        private void DrawDelaunay(ref Mat img, ref VectorOfPointF points, VectorOfVectorOfInt triangleIndexes, MCvScalar delaunayColor)
        {
            Size size = img.Size;
            Rectangle rect = new Rectangle(0, 0, size.Width, size.Height);

            for (int i = 0; i < triangleIndexes.Size; i++)
            {
                VectorOfPoint tri = new VectorOfPoint();
                PointF pp0 = points[triangleIndexes[i][0]];
                PointF pp1 = points[triangleIndexes[i][1]];
                PointF pp2 = points[triangleIndexes[i][2]];
                Point[] p0 = { new Point((int)pp0.X, (int)pp0.Y) };
                Point[] p1 = { new Point((int)pp1.X, (int)pp1.Y) };
                Point[] p2 = { new Point((int)pp2.X, (int)pp2.Y) };
                tri.Push(p0);
                tri.Push(p1);
                tri.Push(p2);

                if (rect.Contains(tri[0]) && rect.Contains(tri[1]) && rect.Contains(tri[2]))
                {
                    CvInvoke.Line(img, tri[0], tri[1], delaunayColor, 2, Emgu.CV.CvEnum.LineType.AntiAlias, 0);
                    CvInvoke.Line(img, tri[1], tri[2], delaunayColor, 2, Emgu.CV.CvEnum.LineType.AntiAlias, 0);
                    CvInvoke.Line(img, tri[2], tri[0], delaunayColor, 2, Emgu.CV.CvEnum.LineType.AntiAlias, 0);
                }

            }
        }

        private void CreateDelaunay(ref Mat img, ref Subdiv2D subdiv, ref VectorOfPointF points,
            bool drawAnimated, ref VectorOfVectorOfInt triangleIndexes)
        {
            PointF[] pointsArr = points.ToArray();
            foreach (PointF p in pointsArr)
            {
                subdiv.Insert(p);

                if (drawAnimated)
                {
                    Mat imgCopy = img.Clone();
                    DrawDelaunay(ref imgCopy, ref subdiv, new MCvScalar(255, 255, 255));
                    CvInvoke.Imshow("Delaunay Triangulation", imgCopy);
                }
            }

            // Unfortunately we don't get the triangles by there original point indexes.
            // We only get them with their vertex coordinates.
            // So we have to map them again to get the triangles with their point indexes.

            Size size = img.Size;
            Rectangle rect = new Rectangle(0, 0, size.Width, size.Height);

            VectorOfInt ind = new VectorOfInt();
            int[] indArr = new int[3];
            Triangle2DF[] triangleList = subdiv.GetDelaunayTriangles();
            for (int i = 0; i < triangleList.Length; i++)
            {
                Triangle2DF t = triangleList[i];

                PointF ptzero = new PointF { X = t.V0.X, Y = t.V0.Y };
                PointF[] PZero = new PointF[] { ptzero };

                PointF ptone = new PointF { X = t.V1.X, Y = t.V1.Y };
                PointF[] POne = new PointF[] { ptone };

                PointF pttwo = new PointF { X = t.V2.X, Y = t.V2.Y };
                PointF[] PTwo = new PointF[] { pttwo };

                VectorOfPointF pt = new VectorOfPointF();

                pt.Push(PZero);

                pt.Push(POne);
                pt.Push(PTwo);

                if (rect.Contains(new Point((int)pt[0].X, (int)pt[0].Y)) &&
                    rect.Contains(new Point((int)pt[1].X, (int)pt[1].Y)) &&
                    rect.Contains(new Point((int)pt[2].X, (int)pt[2].Y)))

                    for (int j = 0; j < 3; j++)
                    {
                        for (int k = 0; k < points.Size; k++)
                        {
                            if (Math.Abs(pt[j].X - points[k].X) < 1.0 &&
                                Math.Abs(pt[j].Y - points[k].Y) < 1)
                            {
                                indArr[j] = k;
                            }
                        }
                    }
                ind = new VectorOfInt(indArr);
                triangleIndexes.Push(ind);
            }
        }

        // Apply affine transform calculated using srcTri and dstTri to src
        private void ApplyAffineTransform(ref Mat warpImage, ref Mat src, ref VectorOfPointF srcTri, ref VectorOfPointF dstTri)
        {
            Mat warpMat = CvInvoke.GetAffineTransform(srcTri, dstTri);
            CvInvoke.WarpAffine(src, warpImage, warpMat, warpImage.Size, Emgu.CV.CvEnum.Inter.Linear, borderMode: Emgu.CV.CvEnum.BorderType.Reflect101);


        }

        private void MorphTriangle(ref Mat img1, ref Mat img2, ref Mat imgM, ref VectorOfPointF t1, ref VectorOfPointF t2, ref VectorOfPointF tM, float alpha)
        {
            // Find bounding rectangle for each triangle
            Rectangle r1 = CvInvoke.BoundingRectangle(t1);
            Rectangle r2 = CvInvoke.BoundingRectangle(t2);
            Rectangle rM = CvInvoke.BoundingRectangle(tM);

            // Offset points by left top corner of the respective rectangles
            VectorOfPointF t1RectFlt = new VectorOfPointF();
            VectorOfPointF t2RectFlt = new VectorOfPointF();
            VectorOfPointF tMRectFlt = new VectorOfPointF();

            // for fillConvexPoly we need ints
            VectorOfPoint tMrectInt = new VectorOfPoint();

            for (int i = 0; i < 3; i++)
            {
                PointF[] pfArrM = { new PointF(tM[i].X - rM.X, tM[i].Y - rM.Y) };
                tMRectFlt.Push(pfArrM);

                Point[] pArrInt = { new Point((int)(tM[i].X - rM.X), (int)(tM[i].Y - rM.Y)) };
                tMrectInt.Push(pArrInt);

                PointF[] pfArr1 = { new PointF(t1[i].X - r1.X, t1[i].Y - r1.Y) };
                t1RectFlt.Push(pfArr1);

                PointF[] pfArr2 = { new PointF(t2[i].X - r2.X, t2[i].Y - r2.Y) };
                t2RectFlt.Push(pfArr2);
            }

            // Create white triangle mask
            Mat mask = Mat.Zeros(rM.Height, rM.Width, Emgu.CV.CvEnum.DepthType.Cv32F, 3);
            CvInvoke.FillConvexPoly(mask, tMrectInt, new MCvScalar(1.0, 1.0, 1.0), Emgu.CV.CvEnum.LineType.AntiAlias, 0); // different

            // Apply warpImage to small rectangular patches
            Mat img1Rect = new Mat(img1, r1);
            Mat img2Rect = new Mat(img2, r2);

            Mat warpImage1 = Mat.Zeros(rM.Height, rM.Width, Emgu.CV.CvEnum.DepthType.Cv32F, 3);
            Mat warpImage2 = Mat.Zeros(rM.Height, rM.Width, Emgu.CV.CvEnum.DepthType.Cv32F, 3);


            ApplyAffineTransform(ref warpImage1, ref img1Rect, ref t1RectFlt, ref tMRectFlt);
            ApplyAffineTransform(ref warpImage2, ref img2Rect, ref t2RectFlt, ref tMRectFlt);


            // Alpha blend rectangular patches into new image 
            Mat imgRect = new Mat();
            Image<Bgr, Byte> imgRect_I = (1.0f - alpha) * warpImage1.ToImage<Bgr, Byte>() + alpha * warpImage2.ToImage<Bgr, Byte>();
            imgRect = imgRect_I.Mat;

            // Delete all outside of triangle
            imgRect.ConvertTo(imgRect, Emgu.CV.CvEnum.DepthType.Cv32F);
            mask.ConvertTo(mask, Emgu.CV.CvEnum.DepthType.Cv32F);
            CvInvoke.Multiply(imgRect, mask, imgRect);
            //CvInvoke.Imwrite($"testimgs/scalartest{count}.jpg",imgRect);

            // Delete all inside the target triangle
            Mat tmp = new Mat(imgM, rM);
            Image<Bgr, Byte> tmpI = tmp.ToImage<Bgr, Byte>();
            Mat mask_cp = new Mat();
            mask.CopyTo(mask_cp);

            Image<Bgr, Byte> tmp_maskI = mask.ToImage<Bgr, Byte>();
            mask_cp.SetTo(new MCvScalar(1.0f, 1.0f, 1.0f));

            CvInvoke.Subtract(mask_cp, mask, mask);
            CvInvoke.Multiply(tmp, mask, tmp);
            count++;

            // Add morphed triangle to target image
            CvInvoke.Add(tmp, imgRect, tmp); // img(rM) = tmp;
            Mat x = new Mat(imgM, rM);
            tmp.CopyTo(x);
        }

        public System.Windows.Media.Imaging.BitmapSource GetMorphedImage()
        {
            Image<Bgr, byte> imgMI = this.imgM.ToImage<Bgr, byte>();
            System.Windows.Media.Imaging.BitmapSource imgMBitMap = BitmapSourceConvert.ToBitmapSource(imgMI);
            return imgMBitMap;
        }

        private VectorOfPointF AddCornerPoints(VectorOfPointF points, Mat img)
        {
            int width = img.Width;
            int height = img.Height;

            // top left
            PointF[] p0 = { new PointF(0,0) };
            points.Push(p0); 

            // top center
            PointF[] p1 = { new PointF((width / 2) - 1, 0 ) };
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
            PointF[] p5 = { new PointF(width - 1, height - 1) };
            points.Push(p5);

            // bottom left
            PointF[] p6 = { new PointF(0, height - 1) };
            points.Push(p6);

            //center left
            PointF[] p7 = { new PointF(0, (height / 2) - 1) };
            points.Push(p7);


            return points;

            
        }


    }
}
