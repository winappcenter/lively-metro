using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using WebReuest;

namespace TrineZip
{
    /// <summary>
    /// Interaction logic for PromotionWindow.xaml
    /// </summary>
    public partial class PromotionWindow : MetroWindow
    {
        private PromotionInfo promotionInfo;
        private Thread countThread;
        public PromotionWindow()
        {
            InitializeComponent();
            countThread = new Thread(new ThreadStart(LoadPromotionInfo));
            countThread.Start();
        }

        private async void GetIt_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(promotionInfo.DownloadUrl));
            this.Close();
        }
        private void LoadPromotionInfo()
        {
            using (var client = new WebTestClient())
            {

                int BytesToRead = 100;
                try
                {
                    promotionInfo = client.GetConfigFromJson<PromotionInfo>("https://winappcenter.com/products/ecms/lively-metro/PromotionInfo.php");
                    if (promotionInfo == null)
                    {
                        promotionInfo = new PromotionInfo();
                    }
                    WebRequest request = WebRequest.Create(new Uri(promotionInfo.Logo, UriKind.Absolute));
                    request.Timeout = -1;
                    WebResponse response = request.GetResponse();
                    Stream responseStream = response.GetResponseStream();
                    BinaryReader reader = new BinaryReader(responseStream);
                    MemoryStream memoryStream = new MemoryStream();

                    byte[] bytebuffer = new byte[BytesToRead];
                    int bytesRead = reader.Read(bytebuffer, 0, BytesToRead);

                    while (bytesRead > 0)
                    {
                        memoryStream.Write(bytebuffer, 0, bytesRead);
                        bytesRead = reader.Read(bytebuffer, 0, BytesToRead);
                    }
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal,

                       new Action(() =>

                       {
                           appName.Text = promotionInfo.Name;
                           var image = new BitmapImage();
                           image.BeginInit();
                           memoryStream.Seek(0, SeekOrigin.Begin);

                           image.StreamSource = memoryStream;
                           image.EndInit();

                           imageLogo.Source = image;


                           stackLoading.Visibility = Visibility.Collapsed;
                           stackProduct.Visibility = Visibility.Visible;
                           Title = "Get " + promotionInfo.Name;



                       }));
                }
                catch (Exception)
                {
                    
                    this.Close();
                }

            }



        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
           
        }
     }
}
