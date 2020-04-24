﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using FaceRecognitionDotNet;
using Microsoft.Extensions.CommandLineUtils;
using OpenCvSharp;
using Point = FaceRecognitionDotNet.Point;

namespace BlinkDetection
{

    internal class Program
    {

        #region Fields

        private const int EyesClosedSeconds = 5;

        #endregion

        #region Methods

        private static void Main(string[] args)
        {
            var app = new CommandLineApplication(false);
            app.Name = nameof(BlinkDetection);
            app.Description = "The program for blink detection demo";
            app.HelpOption("-h|--help");

            app.OnExecute(() =>
            {
                var closedCount = 0;
                var process = true;

                using (var fr = FaceRecognition.Create("models"))
                using (var videoCapture = new VideoCapture(0))
                {
                    while (true)
                    {
                        using (var frame = videoCapture.RetrieveMat())
                        using (var smallFrame = new Mat())
                        {
                            if (process)
                            {
                                //Cv2.Resize(frame, smallFrame, Size.Zero, 0.25, 0.25);
                                Cv2.Resize(frame, smallFrame, Size.Zero, 1, 1);

                                var cols = smallFrame.Cols;
                                var rows = smallFrame.Rows;
                                var elems = smallFrame.ElemSize();


                                // get the correct face landmarks
                                var bytes = new byte[rows * cols * elems];
                                Marshal.Copy(smallFrame.Data, bytes, 0, bytes.Length);
                                using (var rgbSmallFrame = FaceRecognition.LoadImage(bytes, rows, cols, cols * elems, Mode.Rgb))
                                {
                                    var faceLandmarksList = fr.FaceLandmark(rgbSmallFrame).ToArray();

                                    // get eyes
                                    foreach (var faceLandmark in faceLandmarksList)
                                    {
                                        var leftEye = faceLandmark[FacePart.LeftEye].ToArray();
                                        var rightEye = faceLandmark[FacePart.RightEye].ToArray();


                                        var color = new Scalar(255, 0, 0);
                                        var thickness = 2;

                                        var lp = new OpenCvSharp.Point(leftEye[0].X, leftEye[0].Y);
                                        var rp = new OpenCvSharp.Point(rightEye[rightEye.Length - 1].X, rightEye[rightEye.Length - 1].Y);
                                        var l = Math.Min(lp.X, rp.X);
                                        var r = Math.Max(lp.X, rp.X);
                                        var t = Math.Min(lp.Y, rp.Y);
                                        var b = Math.Max(lp.Y, rp.Y);
                                        Cv2.Rectangle(smallFrame, new OpenCvSharp.Point(l, t), new OpenCvSharp.Point(r, b), color, thickness);

                                        Cv2.ImShow("Video", smallFrame);
                                        Cv2.WaitKey(1);

                                        var earLeft = GetEar(leftEye);
                                        var earRight = GetEar(rightEye);

                                        var closed = earLeft < 0.2 && earRight < 0.2;

                                        if (closed)
                                            closedCount += 1;
                                        else
                                            closedCount = 0;

                                        if (closedCount >= EyesClosedSeconds)
                                        {
                                            var asleep = true;
                                            while (asleep) // continue this loop until they wake up and acknowledge music
                                            {
                                                Console.WriteLine("EYE CLOSED");

                                                var key = Console.ReadKey();

                                                if (key.Key == ConsoleKey.Spacebar)
                                                    asleep = false;
                                            }

                                            closedCount = 0;
                                        }
                                    }
                                }
                            }

                            process = !process;
                        }
                    }
                }

                return 0;
            });

            app.Execute(args);
        }

        #region Helpers

        private static double Euclidean(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2.0) + Math.Pow(p1.Y - p2.Y, 2.0));
        }

        private static double GetEar(IList<Point> eye)
        {
            // compute the euclidean distances between the two sets of
            // vertical eye landmarks (x, y)-coordinates
            var a = Euclidean(eye[1], eye[5]);
            var b = Euclidean(eye[2], eye[4]);

            // compute the euclidean distance between the horizontal
            // eye landmark (x, y)-coordinates
            var c = Euclidean(eye[0], eye[3]);

            // compute the eye aspect ratio
            var ear = (a + b) / (2.0 * c);

            // return the eye aspect ratio
            return ear;
        }

        #endregion

        #endregion

    }

}
