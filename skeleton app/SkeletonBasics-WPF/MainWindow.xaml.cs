﻿//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.Diagnostics;
using System.Globalization;


namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Width of output drawing
        /// </summary>
        private const float RenderWidth = 640.0f;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private const float RenderHeight = 480.0f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private readonly Brush centerPointBrush = Brushes.Blue;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private bool tracked;
        private bool sitting;
        private DrawingImage imageSource;
        WMPLib.WindowsMediaPlayer a = new WMPLib.WindowsMediaPlayer();
        private int posstatus = 0;
        private Brush greenbrush = new SolidColorBrush(Color.FromRgb(6, 167, 37));
        private Brush redbrush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        private Brush orangebrush = new SolidColorBrush(Color.FromRgb(255, 165, 0));
        private Brush greybrush = new SolidColorBrush(Color.FromRgb(230, 230, 230));
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }        

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (posstatus != 0)
            { 
                statusbar.Value--;
                if (statusbar.Value == 0)
                {
                    statusbar.Background = redbrush;
                    if(a.playState != WMPLib.WMPPlayState.wmppsPlaying){
                        a.URL = "alarm.mp3";
                        a.controls.play();
                    }
                }
                else if (statusbar.Background == redbrush) statusbar.Foreground = greenbrush;
                else if (statusbar.Value < 25) statusbar.Foreground = redbrush;
                else if (statusbar.Value < 50) statusbar.Foreground = orangebrush;
                else statusbar.Foreground = greenbrush;
            }
            else
            {
                statusbar.Value++;
                statusbar.Foreground = greenbrush;
                if (statusbar.Value == 100) statusbar.Background = greybrush;               
            }

            pos_bad1.IsChecked = posstatus == -1;
            pos_bad2.IsChecked = posstatus == 1;
            pos_good.IsChecked = posstatus == 0;

            
            pos_sitting.IsChecked = !sitting;
        }
        private void sitTimer_Tick(object sender, EventArgs e)
        {
            
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private System.Windows.Threading.DispatcherTimer sitTimer = new System.Windows.Threading.DispatcherTimer();
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0,0,0,0,100);
            dispatcherTimer.Start();            
            sitTimer.Tick += new EventHandler(sitTimer_Tick);
            sitTimer.Interval = new TimeSpan(0, 0, 5);            
            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();
            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the drawing using our image control
            Image1.Source = this.imageSource;

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                this.sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(newSensor_AllFramesReady);//cc
                
                //turn on features that you need
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

                TransformSmoothParameters smooth = new TransformSmoothParameters();
                smooth.Correction = 0.01f;
                smooth.JitterRadius = 0.01f;
                smooth.MaxDeviationRadius = 0.5f;
                smooth.Prediction = 0.5f;
                smooth.Smoothing = 0.9f;
                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable(smooth);

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }
        }

        //this event fires when Color/Depth/Skeleton are synchronized
        void newSensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame == null)
                {
                    return;
                }

                byte[] pixels = new byte[colorFrame.PixelDataLength];
                colorFrame.CopyPixelDataTo(pixels);

                int stride = colorFrame.Width * 4;
                Image.Source =
                    BitmapSource.Create(colorFrame.Width, colorFrame.Height,
                    96, 96, PixelFormats.Bgr32, null, pixels, stride);

            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }
        private int i = 0;
        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];
            if (sitting)
            {
                if(!sitTimer.IsEnabled)
                    sitTimer.Start();
                //if (tracked)
                //    sitting = false;
            }
            else
            {
                sitTimer.Stop();
            }

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        //RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            Joint shoulderCenter = (from j in skel.Joints where j.JointType == JointType.ShoulderCenter select j).FirstOrDefault();
                            Joint hipCenter = (from j in skel.Joints where j.JointType == JointType.HipCenter select j).FirstOrDefault();
                            Joint kneeleft = (from j in skel.Joints where j.JointType == JointType.KneeLeft select j).FirstOrDefault();
                            Joint kneeright = (from j in skel.Joints where j.JointType == JointType.KneeRight select j).FirstOrDefault();
                            double x = shoulderCenter.Position.X - hipCenter.Position.X;
                            double y = shoulderCenter.Position.Y - hipCenter.Position.Y;
                            double degrees = Math.Atan2(x, y) * 180 / Math.PI;
                            if (kneeleft.Position.X < hipCenter.Position.X)
                            {
                                if (degrees < -15) posstatus = 1;
                                else if (degrees > 15) posstatus = -1;
                                else posstatus = 0;
                            }
                            else
                            {
                                if (degrees < -15) posstatus = -1;
                                else if (degrees > 15) posstatus = 1;
                                else posstatus = 0;
                            }
                            if ((hipCenter.Position.Y - kneeleft.Position.Y) < .5f) {
                                if (i < 20)
                                {
                                    i++;
                                }
                                else
                                {
                                    sitting = true;
                                    i = 0;
                                }
                            }
                            else { sitting = false; posstatus = 0; }
                            this.DrawBonesAndJoints(skel, dc);
                            tracked = true;
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                            posstatus = 0;
                            tracked = false;
                        }
                    }
                }
                else
                {
                    posstatus = 0;
                    tracked = false;
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }

        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            //this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            //this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            //this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            //this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            //this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            //this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            //this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            //// Left Arm
            //this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            //this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            //this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            //// Right Arm
            //this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            //this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            //this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            //// Left Leg
            //this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            //this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            //this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            //// Right Leg
            //this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            //this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            //this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);
 
            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;                
                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    if (joint.JointType == JointType.ShoulderCenter || joint.JointType == JointType.HipCenter || joint.JointType == JointType.KneeLeft || joint.JointType == JointType.KneeRight)
                    {
                        
                        drawBrush = this.trackedJointBrush;
                        //string text = joint.Position.X.ToString() + joint.Position.Y.ToString() + joint.Position.Z.ToString();
                        //drawingContext.DrawText(new FormattedText(text, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Verdana"), 12, System.Windows.Media.Brushes.Tomato), SkeletonPointToScreen(joint.Position));
                    
                         //*/
                    }
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    //drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }
    }
}