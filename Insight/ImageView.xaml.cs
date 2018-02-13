﻿using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Insight
{
    /// <summary>
    /// Interaction logic for ImageView.xaml
    /// </summary>
    public partial class ImageView : Window
    {
        public ImageView()
        {
            InitializeComponent();
        }

        public void SetImage(string path)
        {
            _image.Source = new BitmapImage(new Uri(path));
        }
    }
}