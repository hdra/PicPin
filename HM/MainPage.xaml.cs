using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.IO;
using FlickrNet;
using System.Collections.ObjectModel;
using System.Device.Location;
using Microsoft.Phone.Controls.Maps;
using System.Windows.Media.Imaging;
using Microsoft.Phone.Controls.Maps.Platform;

namespace HM
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        double coordinateThreshold = 1;
        double dayThreshold = 10;
        Flickr flickr;

        ObservableCollection<SearchResult> list;
        public MainPage()
        {
            flickr = new Flickr("89ffd2389fc872dab522305abe482f67");
            list = new ObservableCollection<SearchResult>();
            
            InitializeComponent();
            this.map.Center = new GeoCoordinate(3.07375, 101.6622391);
            this.datePicker.Value = DateTime.Now;
        }

        void initiateSearch(double latitude, double longitude, DateTime day)
        {
            System.Diagnostics.Debug.WriteLine("search, day:"+day.ToShortDateString());
            System.Diagnostics.Debug.WriteLine("search, lat:" + latitude + "long:" + longitude);


            
            PhotoSearchOptions options = new PhotoSearchOptions();
            // TODO: specify more options here
            options.BoundaryBox = new BoundaryBox(longitude-coordinateThreshold,
                                                  latitude-coordinateThreshold,
                                                  longitude+coordinateThreshold,
                                                  latitude+coordinateThreshold);
            //options.Tags = "japan";
            options.MaxTakenDate = day.AddDays(this.dayThreshold);
            options.MinTakenDate = day.AddDays(-this.dayThreshold);
            options.Extras = PhotoSearchExtras.All;
            options.PerPage = 15;
            options.Page = 1;
            options.HasGeo = true;
            flickr.PhotosSearchAsync(options, searchComplete);
            this.loadingText.Text = "Loading";
        }

        void searchComplete(FlickrResult<PhotoCollection> result)
        {
            System.Diagnostics.Debug.WriteLine("Finished: " + result.Result.Count);
            this.loadingText.Text = "Found " + result.Result.Count + " photos"; 
            PhotoCollection res = result.Result;

            foreach (Photo photo in res)
            {
                this.list.Add(new SearchResult(photo.SquareThumbnailUrl, photo.Title));
                if(!MapContainsPinLocation(new GeoCoordinate(photo.Latitude,photo.Longitude)))
                {
                    System.Diagnostics.Debug.WriteLine("pinning " + photo.Title);
                    ImageBrush thumb = new ImageBrush()
                    {
                        ImageSource = new BitmapImage(new Uri(photo.SquareThumbnailUrl))
                    };

                    Pushpin pin = new Pushpin();
                    pin.Tag = photo.PhotoId+"|split|"+photo.MediumUrl+"|split|"+photo.Title+"|split|"+photo.Description;
                    pin.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>(pin_Tap);
                    pin.Content = new System.Windows.Shapes.Rectangle() { Fill = thumb, Height = 70, Width = 70 };
                    pin.Location = new GeoCoordinate(photo.Latitude, photo.Longitude);
                    this.map.Children.Add(pin);
                }
            }
        }

        void pin_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("getting details");
            var pin = sender as Pushpin;

            string[] separators = new string[]{"|split|"};
            string[] splits = pin.Tag.ToString().Split(separators,StringSplitOptions.None);

            this.photopreview.Source = new BitmapImage(new Uri(splits[1]));
            this.phototitle.Text = splits[2];
            this.photodesc.Text = splits[3];
            this.popUp.IsOpen = true;
        }

        void getComplete(FlickrResult<PhotoInfo> result)
        {
            string url = result.Result.MediumUrl;
            string title = result.Result.Title;
            string desc = result.Result.Description;

            System.Diagnostics.Debug.WriteLine("got details "+url+' '+title);

            this.photopreview.Source = new BitmapImage(new Uri(url));
            this.phototitle.Text = title;
            this.photodesc.Text = desc;
        }

        private void map_ViewChangeEnd(object sender, MapEventArgs e)
        {
            initiateSearch(map.Center.Latitude, map.Center.Longitude, (DateTime)this.datePicker.Value);
        }

        private void datePicker_ValueChanged(object sender, DateTimeValueChangedEventArgs e)
        {
            this.map.Children.Clear();
            initiateSearch(map.Center.Latitude, map.Center.Longitude, (DateTime)this.datePicker.Value);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (this.popUp.IsOpen)
            {
                this.popUp.IsOpen = false;
                e.Cancel = true;
            }
            base.OnBackKeyPress(e);
        }

        private bool MapContainsPinLocation(Location locationCheck)
        {
            bool result = false;
            foreach (UIElement p in map.Children)
            {
                if ((p) is Pushpin)
                {
                    if (((Pushpin)p).Location == locationCheck)
                    {
                        result = true;
                        break;
                    }
                }
            }
            return result;
        }
    }


    public class SearchResult
    {
        public string url { get; set; }
        public string title { get; set; }

        public SearchResult(string url, string title)
        {
            this.url = url;
            this.title = title;
        }
    }
}