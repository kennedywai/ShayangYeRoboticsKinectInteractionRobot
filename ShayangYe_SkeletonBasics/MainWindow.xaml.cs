//------------------------------------------------------------------------------
// <copyright file="SkeletonBasics.cpp" company="ShayangYe Robotics">
//     Copyright (c) ShayangYe Robotics All rights reserved.
//	   Written by Wai Kennedy kennedywai@hotmail.com kennedywaiisawesome@gmail.com
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using System.Threading;
    using System;
    using System.Linq;
    using System.IO.Ports;
    //using System.Linq;    /// <summary>
                          /// Interaction logic for MainWindow.xaml
                          /// </summary>
    public partial class MainWindow : Window
    {
        //serial port
        static SerialPort _serialPort1;
        //static SerialPort _serialPort2;
        //receive parameter       
        public string comport = "";
        private System.Timers.Timer Timer_comport_reconnect;

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
        private readonly Brush inferredJointBrush = Brushes.Red;

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
        private DrawingImage imageSource;

        //Tight hand position x threshold value
        private const float rightHand_X_Max = 0.67f;
        private const float rightHand_X_Min = -0.2f;

        //right hand position y threshold value
        private const float rightHand_Y_Max = 0.2f;
        private const float rightHand_Y_Min = -0.3f;

        //head position x threshold value
        private const float head_X_Max = 0.67f;
        private const float head_X_Min = -0.76f;

        //Data Byte
        private byte X_Pos, Y_Pos;
        private byte[] data_byte;

        //Tracking User
        private bool hasActivePlayer = false;
        Skeleton[] skeletons = new Skeleton[0];
        

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping skeleton data
        /// </summary>
        /// <param name="skeleton">skeleton to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the drawing using our image control
            Image.Source = this.imageSource;

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
                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();

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

            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
            }

            SerialPortInitialization();
            Timer_comport_reconnect = new System.Timers.Timer();
            Timer_comport_reconnect.Elapsed += new System.Timers.ElapsedEventHandler(Timer_comport_reconnect_Elapsed);
            Timer_comport_reconnect.Interval = 5000;
            SerialPortOpen();

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
        
            Timer_comport_reconnect.Stop();

            if (_serialPort1.IsOpen)
                _serialPort1.Close();
        }

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            //Skeleton[] skeletons = new Skeleton[0];
            
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
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        JointCollection jc = skel.Joints;
                        Joint rightHand = jc[JointType.HandRight];
                        Joint leftHand = jc[JointType.HandLeft];
                        Joint leftElbow = jc[JointType.ElbowLeft];
                        Joint head = jc[JointType.Head];
                        //Joint rightShoulder = jc[JointType.ShoulderRight];
                        //Joint leftShoulder = jc[JointType.ShoulderLeft];
                        
                        RenderClippedEdges(skel, dc);
                        //Choose the closet target to track and ignore the people on background
                        TrackClosestSkeleton(skel, jc);    
                        
                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.DrawBonesAndJoints(skel, dc);
                            hasActivePlayer = true;
                        }
                        
                        if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }

                        if (hasActivePlayer == false)
                        {
                            //Servo motos are back to starting positions
                            SendingString("0");
                        }
                        }
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
                this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
                this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
                this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
                this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
                this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
                this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
                this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

                // Left Arm
                this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
                this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
                this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

                // Right Arm
                this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
                this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
                this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

                // Left Leg
                this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
                this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
                this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

                // Right Leg
                this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
                this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
                this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);

                // Render Joints
                foreach (Joint joint in skeleton.Joints)
                {
                    Brush drawBrush = null;

                    if (joint.TrackingState == JointTrackingState.Tracked)
                    {
                        drawBrush = this.trackedJointBrush;                    
                    }
                    else if (joint.TrackingState == JointTrackingState.Inferred)
                    {
                        drawBrush = this.inferredJointBrush;                    
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

            /// <summary>
            /// Handles the checking or unchecking of the seated mode combo box
            /// </summary>
            /// <param name="sender">object sending the event</param>
            /// <param name="e">event arguments</param>
            private void CheckBoxSeatedModeChanged(object sender, RoutedEventArgs e)
            {
                if (null != this.sensor)
                {
                    if (this.checkBoxSeatedMode.IsChecked.GetValueOrDefault())
                    {
                        this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                    }
                    else
                    {
                        this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                    }
                }
            }

            //Tracking only one userID
            //cycle through the proposed skeletons, chose those that fit the needs, 
            //and pass their tracking IDs to the skeletal tracking APIs for full tracking.
            //After the application has control over the users to track, the skeletal tracking system will not take control back: 
            //if the user goes out of the screen, it is up to the application to select a new user to track.
            //Note that if a user exits the scene and comes back, he or she will receive a new tracking ID chosen randomly.
            //The new ID will not be related to the one he or she had when exiting the scene.
            //This app can also select to track only one skeleton or no skeletons at all by passing a null tracking ID to the skeletal tracking APIs.
            private void TrackClosestSkeleton(Skeleton skeleton, JointCollection jc)
            {  
               Joint rightHand = jc[JointType.HandRight];
               Joint leftHand = jc[JointType.HandLeft];
               Joint leftElbow = jc[JointType.ElbowLeft];
               Joint head = jc[JointType.Head];
             if (this.sensor != null && this.sensor.SkeletonStream != null)
             {
                if (!this.sensor.SkeletonStream.AppChoosesSkeletons)
                {
                    // Ensure AppChoosesSkeletons is set
                    this.sensor.SkeletonStream.AppChoosesSkeletons = true; 
                }
                //User's head tracking distance
                float closestDistance = 2.8f; 
                int closestID = 0;

                foreach (Skeleton skel in skeletons.Where(skl => skl.TrackingState != SkeletonTrackingState.NotTracked))
                {  
                    //Track the user if the user's head is close enough
                    if (skeleton.Position.Z < closestDistance)
                    {
                        closestID = skeleton.TrackingId;
                        closestDistance = skeleton.Position.Z;
                    }
                }
                //If the user's ID is tracked
                if (closestID > 0)
                {
                    // Track this skeleton
                    this.sensor.SkeletonStream.ChooseSkeletons(closestID); 
                    Console.WriteLine(closestID.ToString());
                    if(skeleton.Position.Z >= 1.3 && skeleton.Position.Z <= 1.53)
                    {
                    //Tracking user's hand position
                    if (leftHand.Position.Y > leftElbow.Position.Y)
                    {
                        //If the user's right hand is in the interval
                        if ((rightHand.Position.X >= rightHand_X_Min && rightHand.Position.X <= rightHand_X_Max)
                        && (rightHand.Position.Y >= rightHand_Y_Min && rightHand.Position.Y <= rightHand_Y_Max))
                        {

                            if (rightHand.Position.X > -0.113 && rightHand.Position.X <= -0.026)
                            {
                                X_Pos = 0x01;
                            }
                            if (rightHand.Position.X > -0.026 && rightHand.Position.X <= 0.061)
                            {
                                X_Pos = 0x02;
                            }
                            if (rightHand.Position.X > 0.061 && rightHand.Position.X <= 0.148)
                            {
                                X_Pos = 0x03;
                            }
                            if (rightHand.Position.X > 0.148 && rightHand.Position.X <= 0.235)
                            {
                                X_Pos = 0x04;
                            }
                            if (rightHand.Position.X > 0.235 && rightHand.Position.X <= 0.322)
                            {
                                X_Pos = 0x05;
                            }
                            if (rightHand.Position.X > 0.322 && rightHand.Position.X <= 0.409)
                            {
                                X_Pos = 0x06;
                            }
                            if (rightHand.Position.X > 0.409 && rightHand.Position.X <= 0.496)
                            {
                                X_Pos = 0x07;
                            }
                            if (rightHand.Position.X > 0.496 && rightHand.Position.X <= 0.583)
                            {
                                X_Pos = 0x08;
                            }
                            //To the right

                            //Down
                            if (rightHand.Position.Y >= -0.3 && rightHand.Position.Y <= -0.25)
                            {
                                Y_Pos = 0x09;
                            }
                            if (rightHand.Position.Y > -0.25 && rightHand.Position.Y <= -0.2)
                            {
                                Y_Pos = 0x0a;
                            }
                            if (rightHand.Position.Y > -0.2 && rightHand.Position.Y <= -0.15)
                            {
                                Y_Pos = 0x0b;
                            }
                            if (rightHand.Position.Y > -0.15 && rightHand.Position.Y <= -0.1)
                            {
                                Y_Pos = 0x0c;
                            }
                            if (rightHand.Position.Y > -0.1 && rightHand.Position.Y <= -0.05)
                            {
                                Y_Pos = 0x0d;
                            }
                            if (rightHand.Position.Y > -0.05 && rightHand.Position.Y <= 0)
                            {
                                Y_Pos = 0x0e;
                            }
                            if (rightHand.Position.Y > 0 && rightHand.Position.Y <= 0.05)
                            {
                                Y_Pos = 0x0f;
                            }
                            if (rightHand.Position.Y > 0.05 && rightHand.Position.Y <= 0.1)
                            {
                                Y_Pos = 0x10;
                            }
                            //Up
                            data_byte = new byte[] { X_Pos, Y_Pos };
                            SendingData(data_byte);
                        }
                    
                    }

                    }
                    //Tracking user's head postion
                    if (head.Position.X >= head_X_Min && head.Position.X <= head_X_Max)
                    {

                        if (head.Position.X > -0.75 && head.Position.X <= -0.634)
                        {
                            X_Pos = 0x01;
                        }
                        if (head.Position.X > -0.634 && head.Position.X <= -0.471)
                        {
                            X_Pos = 0x02;
                        }
                        if (head.Position.X > -0.471 && head.Position.X <= -0.308)
                        {
                            X_Pos = 0x03;
                        }
                        if (head.Position.X > -0.308 && head.Position.X <= -0.145)
                        {
                            X_Pos = 0x04;
                        }
                        if (head.Position.X > -0.145 && head.Position.X <= 0.018)
                        {
                            X_Pos = 0x05;
                        }
                        if (head.Position.X > 0.018 && head.Position.X <= 0.181)
                        {
                            X_Pos = 0x06;
                        }
                        if (head.Position.X > 0.181 && head.Position.X <= 0.344)
                        {
                            X_Pos = 0x07;
                        }
                        if (head.Position.X > 0.344 && head.Position.X <= 0.507)
                        {
                            X_Pos = 0x08;
                        }
                        
                        data_byte = new byte[] { X_Pos, 0x0f };
                        SendingData(data_byte);
                    }

                    //Back to Starting Positions When there is nothing to track
                    else
                    {
                        SendingString("0");
                    }

                }

            }
            }

            //Serial Port Initialization
            private void SerialPortInitialization()
            {
                _serialPort1 = new SerialPort();

                if (comport != "")
                {
                _serialPort1.PortName = "COM" + comport;
                Console.WriteLine("Robot port number now is " + _serialPort1.PortName);
                }
                else
                {
                    try
                    {
                        _serialPort1.PortName = "COM3";
                        _serialPort1.BaudRate = 9600;
                        _serialPort1.Parity = Parity.None;
                        _serialPort1.DataBits = 8;
                        _serialPort1.StopBits = StopBits.One;
                        _serialPort1.Handshake = Handshake.None;
                    }
                    catch (Exception)
                    {
                    Console.WriteLine("Serial Port Didnt Open Correctly!");
                    }
                }
                // Set the read/write timeouts
                _serialPort1.ReadTimeout = 500;
                _serialPort1.WriteTimeout = 500;
            }
            
            //Serial Port Open
            private void SerialPortOpen()
            {
                try
                {
                _serialPort1.Open();
                this.statusBarText.Text = "Robot Connected";
                //sending data
                if (_serialPort1.IsOpen)
                    try
                    {
                        this.statusBarText.Text = "Robot Connected";
                    }
                    catch
                    {
                        _serialPort1.Close();
                        Timer_comport_reconnect.Start();
                        this.statusBarText.Text = "Reconnecting to Robot...";
                    }
                else
                {
                    Timer_comport_reconnect.Start();
                    this.statusBarText.Text = "Reconnecting to Robot...";
                }
                
                }
                catch
                {
                this.statusBarText.Text = "WTF?!...";
                Timer_comport_reconnect.Start();
                }
             }

             //Time out reconnect
             private void Timer_comport_reconnect_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
             {
                try
                {
                    _serialPort1.Open();
                }
                catch
                {
                }
                if (_serialPort1.IsOpen)
                {
                //this.statusBarText.Text = "";
                Console.WriteLine("Robot Reconnected");
                Timer_comport_reconnect.Stop();
                }

             }
                
             //Sending Data Byte to Micro Controller
             private void SendingData(byte[] data_Byte)
             {
                if (_serialPort1.IsOpen)
                {
                    try
                    {
                    this.statusBarText.Text = "";
                    //Sending data to innovati board via serial port
                    _serialPort1.Write(data_byte, 0, data_byte.Length);
                    }
                    catch (Exception)
                    {
                    _serialPort1.Close();
                    Timer_comport_reconnect.Start();
                    this.statusBarText.Text = "Reconnecting to Robot...";
                    }
                }
                else
                {
                Timer_comport_reconnect.Start();
                this.statusBarText.Text = "Reconnecting to Robot...";
                }
             }
             
             //Sending String
             private void SendingString(String s)
             {
                if (_serialPort1.IsOpen)
                {
                    try
                    {
                    //Console.WriteLine("Not Tracked!");
                    _serialPort1.WriteLine(s);
                    }
                    catch (Exception)
                    {
                    _serialPort1.Close();
                    Timer_comport_reconnect.Start();
                    this.statusBarText.Text = "Reconnecting to Robot...";
                    }
                }
                else
                {
                    Timer_comport_reconnect.Start();
                    this.statusBarText.Text = "Reconnecting to Robot...";
                }
             }


    }
    }
 