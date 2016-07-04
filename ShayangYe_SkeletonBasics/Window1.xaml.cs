//------------------------------------------------------------------------------
// <copyright file="SkeletonBasics.cpp" company="ShayangYe Robotics">
//     Copyright (c) ShayangYe Robotics All rights reserved.
//	   Written by Wai Kennedy kennedywai@hotmail.com kennedywaiisawesome@gmail.com
// </copyright>
//------------------------------------------------------------------------------

using System;
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
using System.Windows.Shapes;

using System.IO;

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
            this.ID_textbox.Text = Properties.Settings.Default.RobotID;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            string comport;

            StreamReader SettingFile = new StreamReader("setting.ini");
            string setting_string = SettingFile.ReadToEnd();
            string[] ID_list = setting_string.Split(' ');
            //comport = ID_list[Int32.Parse(this.ID_textbox.Text)];
            comport = this.ID_textbox.Text;
            Console.WriteLine("Robot port number is " + comport);

            MainWindow mw = new MainWindow();
            mw.comport = comport;
            Properties.Settings.Default.RobotID = this.ID_textbox.Text;
            Properties.Settings.Default.Save();
            mw.ShowDialog();
            this.Close();
        }

        private void ID_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
