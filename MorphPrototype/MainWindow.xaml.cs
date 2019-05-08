using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Drawing;
using System.Windows;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using Size = System.Drawing.Size;

namespace MorphPrototype
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public int count = 0;
        public float alpha = 0.5f;
        public float sign = 1.0f;

        Mat img1 = new Mat();
        Mat img2 = new Mat();

        VectorOfVectorOfInt triangleIndexes = new VectorOfVectorOfInt();
        
        VectorOfPointF points1 = new VectorOfPointF();
        VectorOfPointF points2 = new VectorOfPointF();
        VectorOfPointF pointsM = new VectorOfPointF();


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
            var x = new Mat(imgM, rM);
            tmp.CopyTo(x);



            Console.WriteLine();
        }

        public MainWindow()
        {
            InitializeComponent();

            // Read input images
            img1 = CvInvoke.Imread($"../../hillary_clinton.jpg");
            img2 = CvInvoke.Imread($"../../donald_trump.jpg");

            img1.ConvertTo(img1, Emgu.CV.CvEnum.DepthType.Cv32F);
            img2.ConvertTo(img2, Emgu.CV.CvEnum.DepthType.Cv32F);

            points1 = new VectorOfPointF();
            points2 = new VectorOfPointF();
            //Read points of face 1
            PointF[] pts1 = {
                new PointF(125, 358), new PointF(128, 402), new PointF(132, 445), new PointF(137, 490), new PointF(151, 532), new PointF(178, 566),
                new PointF(216, 595), new PointF(260, 616), new PointF(304, 622), new PointF(347, 612), new PointF(388, 591), new PointF(426, 563),
                new PointF(452, 526), new PointF(466, 482), new PointF(470, 437), new PointF(474, 392), new PointF(477, 345), new PointF(150, 332),
                new PointF(171, 312), new PointF(200, 304), new PointF(230, 307), new PointF(259, 319), new PointF(315, 314), new PointF(345, 299),
                new PointF(377, 294), new PointF(410, 300), new PointF(434, 319), new PointF(289, 350), new PointF(290, 382), new PointF(291, 413),
                new PointF(292, 444), new PointF(258, 458), new PointF(275, 462), new PointF(294, 467), new PointF(313, 460), new PointF(331, 454),
                new PointF(184, 358), new PointF(201, 344), new PointF(224, 345), new PointF(245, 363), new PointF(224, 368), new PointF(201, 368),
                new PointF(339, 358), new PointF(358, 337), new PointF(381, 335), new PointF(401, 349), new PointF(383, 359), new PointF(360, 361),
                new PointF(214, 493), new PointF(245, 489), new PointF(274, 488), new PointF(295, 489), new PointF(316, 485), new PointF(346, 483),
                new PointF(381, 484), new PointF(351, 524), new PointF(321, 540), new PointF(299, 543), new PointF(277, 542), new PointF(246, 530),
                new PointF(223, 495), new PointF(275, 499), new PointF(296, 499), new PointF(317, 496), new PointF(372, 487), new PointF(319, 523),
                new PointF(298, 526), new PointF(276, 525), new PointF(495, 400), new PointF(264, 736), new PointF(0, 774),   new PointF(599, 706),
                new PointF(0, 0),     new PointF(0, 400),   new PointF(0, 799),   new PointF(300, 799), new PointF(599, 799), new PointF(599, 400),
                new PointF(599, 0),   new PointF(300, 0)
            };
            //Read points of face 2
            PointF[] pts2 = {
                new PointF(80, 311),  new PointF(80, 357),  new PointF(83, 405),  new PointF(88, 454),  new PointF(96, 502),  new PointF(114, 546),
                new PointF(144, 580), new PointF(180, 607), new PointF(226, 616), new PointF(278, 611), new PointF(335, 591), new PointF(391, 568),
                new PointF(434, 531), new PointF(464, 486), new PointF(479, 433), new PointF(487, 377), new PointF(494, 321), new PointF(109, 259),
                new PointF(126, 238), new PointF(154, 235), new PointF(183, 239), new PointF(212, 248), new PointF(283, 255), new PointF(321, 244),
                new PointF(359, 240), new PointF(397, 247), new PointF(427, 271), new PointF(241, 298), new PointF(237, 327), new PointF(232, 354),
                new PointF(227, 383), new PointF(201, 418), new PointF(215, 423), new PointF(230, 427), new PointF(250, 425), new PointF(270, 421),
                new PointF(141, 301), new PointF(159, 293), new PointF(181, 294), new PointF(199, 309), new PointF(178, 313), new PointF(156, 311),
                new PointF(309, 312), new PointF(331, 299), new PointF(355, 298), new PointF(376, 309), new PointF(356, 317), new PointF(331, 317),
                new PointF(177, 503), new PointF(194, 484), new PointF(213, 473), new PointF(228, 477), new PointF(244, 474), new PointF(271, 488),
                new PointF(299, 507), new PointF(271, 523), new PointF(244, 528), new PointF(226, 528), new PointF(209, 525), new PointF(192, 517),
                new PointF(190, 500), new PointF(213, 489), new PointF(228, 491), new PointF(244, 491), new PointF(286, 504), new PointF(244, 508),
                new PointF(228, 507), new PointF(211, 504), new PointF(538, 410), new PointF(215, 727), new PointF(0, 760),   new PointF(599, 597),
                new PointF(0, 0),     new PointF(0, 400),   new PointF(0, 799),   new PointF(300, 799), new PointF(599, 799), new PointF(599, 400),
                new PointF(599, 0),   new PointF(300, 0)
            };
            points1.Push(pts1);
            points2.Push(pts2);



            // Create an instance of Subdiv2D
            Rectangle rect = new Rectangle(0, 0, img1.Size.Width, img1.Size.Height);
            Subdiv2D subdiv = new Subdiv2D(rect);

            // Create and Draw the Delaunay triangulation
            triangleIndexes = new VectorOfVectorOfInt();
            CreateDelaunay(ref img1, ref subdiv, ref points1, false, ref triangleIndexes);

            // Draw the Delaunay triangulation of face 1
            Mat img1D = img1.Clone();
            DrawDelaunay(ref img1D, ref subdiv, new MCvScalar(255, 255, 255));


            // Draw the Delaunay triangulation of face 2
            Mat img2D = img2.Clone();
            DrawDelaunay(ref img2D, ref points2, triangleIndexes, new MCvScalar(255, 255, 255));
            img2D.ConvertTo(img2D, Emgu.CV.CvEnum.DepthType.Cv8U);


            

            
            Console.WriteLine("test");
            //}

            CreateMorph();
        }

        public void CreateMorph()
        {

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
            Mat imgM = Mat.Zeros(img1.Rows, img1.Cols, Emgu.CV.CvEnum.DepthType.Cv32F, 3);

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
            CvInvoke.Imshow("Morphed Face", imgM);
        }

        private void SlValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var newAlpha = e.NewValue;
            this.alpha = (float)newAlpha;
            CreateMorph();

        }
    }
}
