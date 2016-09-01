using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CustomTFIDF
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            Task.Run(async delegate { await RunKMeans(); });
        }

        private async Task RunKMeans()
        {
            MegaDictionary mega = new MegaDictionary();

            List<string> fileNames = new List<string>() {};
            List<string> data = new List<string>();

            var allfiles = await ApplicationData.Current.LocalFolder.GetFilesAsync();
            
            Debug.WriteLine(ApplicationData.Current.LocalFolder.Path);

            string[] hi = new string[allfiles.Count];

            // get only documents
            //foreach (var file in allfiles)
            //{
            //    fileNames.Add(file.Name);
            //    data.Add(await FileIO.ReadTextAsync(file));
            //}
            var counter = 0;
            foreach (var storageFile in allfiles)
            {
                IBuffer buffer = await FileIO.ReadBufferAsync(storageFile);
                DataReader reader = DataReader.FromBuffer(buffer);
                byte[] fileContent = new byte[reader.UnconsumedBufferLength];
                reader.ReadBytes(fileContent);
                string text = Encoding.UTF8.GetString(fileContent, 0, fileContent.Length);

                hi[counter] = text;
                counter++;
                //data.Add(text);
                //fileNames.Add(storageFile.Name);
            }

            Debug.WriteLine(hi);


            //ClusterKMeansTestElkans KMeans = new ClusterKMeansTestElkans(20, data.ToArray(), fileNames.ToArray());
            //KMeans.calcTFIDFVectors();
            //KMeans.GenerateClustersWithK(30);
            
            //for (int i = 0; i < 10; i++)
            //{
            //    var watch = System.Diagnostics.Stopwatch.StartNew();
            //    KMeans.calcTFIDFVectors();
            //    KMeans.GenerateClustersWithK(5);
            //    watch.Stop();
            //    var elapsedMs = watch.ElapsedMilliseconds;
            //    Debug.WriteLine("Iteration " + i + " Took: " + elapsedMs + " ms");
            //}
        }
    }
}
