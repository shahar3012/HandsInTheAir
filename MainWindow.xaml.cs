using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Windows.Forms;


namespace HandsInTheAir
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // RealSense
        PXCMSenseManager psm;
        PXCMTouchlessController ptc;

        // Scrolling Feature
        ScrollViewer myListscrollViwer;
        double initialScrollPoint;
        double initialScrollOffest;
        const double scrollSensitivity = 10f;
        bool vSign = false;
        int vSignX = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            StartRealSense();

            UpdateConfiguration();

            StartFrameLoop();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopRealSense();
        }

        private void StartRealSense()
        {
            Console.WriteLine("Starting Touchless Controller");
            t.Enabled = true;
            t.Interval = 2000;
            t.Tick += t_Tick;
            pxcmStatus rc;

            // creating Sense Manager
            psm = PXCMSenseManager.CreateInstance();
            Console.WriteLine("Creating SenseManager: " + psm == null ? "failed" : "success");
            if (psm == null)
                Environment.Exit(-1);

            // work from file if a filename is given as command line argument
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                psm.captureManager.SetFileName(args[1], false);
            }

            // Enable touchless controller in the multimodal pipeline
            rc = psm.EnableTouchlessController(null);
            Console.WriteLine("Enabling Touchless Controller: " + rc.ToString());
            if (rc != pxcmStatus.PXCM_STATUS_NO_ERROR)
                Environment.Exit(-1);

            // initialize the pipeline
            PXCMSenseManager.Handler handler = new PXCMSenseManager.Handler();
            rc = psm.Init(handler);
            Console.WriteLine("Initializing the pipeline: " + rc.ToString());
            if (rc != pxcmStatus.PXCM_STATUS_NO_ERROR)
                Environment.Exit(-1);

            // getting touchless controller
            ptc = psm.QueryTouchlessController();
            if (ptc == null)
                Environment.Exit(-1);
            ptc.SubscribeEvent(new PXCMTouchlessController.OnFiredUXEventDelegate(OnTouchlessControllerUXEvent));
            
        }

        void t_Tick(object sender, EventArgs e)
        {
            vSign = false;
            vSignX = 0;
            t.Stop();
            Console.WriteLine("VSign Start");
        }

        // on closing
        private void StopRealSense()
        {
            Console.WriteLine("Disposing SenseManager and Touchless Controller");
            ptc.Dispose();
            psm.Close();
            psm.Dispose();
           
        }

        private void UpdateConfiguration()
        {
            pxcmStatus rc;
            PXCMTouchlessController.ProfileInfo pInfo;

            rc = ptc.QueryProfile(out pInfo);
            Console.WriteLine("Querying Profile: " + rc.ToString());
            if (rc != pxcmStatus.PXCM_STATUS_NO_ERROR)
                Environment.Exit(-1);
            ptc.AddGestureActionMapping("v_sign", PXCMTouchlessController.Action.Action_None, new PXCMTouchlessController.OnFiredActionDelegate(OnVSign));
            //ptc.AddGestureActionMapping("swipeLeft", PXCMTouchlessController.Action.Action_NextTrack, new PXCMTouchlessController.OnFiredActionDelegate(OnSwipeLeft));

            pInfo.config = PXCMTouchlessController.ProfileInfo.Configuration.Configuration_Scroll_Vertically | PXCMTouchlessController.ProfileInfo.Configuration.Configuration_Allow_Zoom;

            rc = ptc.SetProfile(pInfo);
            Console.WriteLine("Setting Profile: " + rc.ToString());
        }

        private void StartFrameLoop()
        {
            psm.StreamFrames(false);
        }

       private void OnVSign(PXCMTouchlessController.Action data)
       {
           if (!vSign)
           {
               vSign = true;
               Console.WriteLine("VSign Start");
               vSignX = (int)MouseInjection.getCursorPos().X;
               t.Start();
           }
       }

       System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
       

        private void OnTouchlessControllerUXEvent(PXCMTouchlessController.UXEventData data)
        {
            if (this.Dispatcher.CheckAccess())
            {
                switch (data.type)
                {
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_CursorVisible:
                        {
                            Console.WriteLine("Cursor Visible");
                        }
                        break;
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_CursorNotVisible:
                        {
                            Console.WriteLine("Cursor Not Visible");
                        }
                        break;
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_Select:
                        {
                            if (HandleHand.EnableSelect)
                            {
                                Console.WriteLine("Select");
                                //     MouseInjection.ClickLeftMouseButton();
                            }
                        }
                        break;
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_StartScroll:
                        {
                            Console.WriteLine("Start Scroll");
                            initialScrollPoint = data.position.y;
                            //    initialScrollOffest = myListscrollViwer.VerticalOffset;
                        }
                        break;
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_Back:
                        {
                            Console.WriteLine("back");
                            //    initialScrollOffest = myListscrollViwer.VerticalOffset;
                        }
                        break;
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_Zoom:
                        {
                            Console.WriteLine("Zooming");
                        }
                        break;
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_StartZoom:
                        {
                            HandleHand.ToggleSelectEnable();
                            Console.WriteLine("StartZoom");
                        }
                        break;
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_EndZoom:
                        {
                            HandleHand.ToggleSelectEnable();
                            Console.WriteLine("EndZoom");
                        }
                        break;
                    case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_CursorMove:
                        {
                           if (HandleHand.MoveEnabled)
                           {       
                                Point point = new Point();
                                point.X = Math.Max(Math.Min(0.9F, data.position.x), 0.1F);
                                point.Y = Math.Max(Math.Min(0.9F, data.position.y), 0.1F);

                               // Point myListBoxPosition = DisplayArea.PointToScreen(new Point(0d, 0d));
                                Point currPoint = MouseInjection.getCursorPos();
                                double mouseX = data.position.x * Screen.PrimaryScreen.Bounds.Width;
                                double mouseY = data.position.y * Screen.PrimaryScreen.Bounds.Height;

                               // Console.WriteLine("x: "+ data.position.x + " y: " + data.position.y);
                               if (vSign)
                               {
                                   if (mouseX > vSignX + Screen.PrimaryScreen.Bounds.Width * 0.5)
                                   {
                                       Console.WriteLine("swipe right");
                                       vSign = false;
                                       t.Stop();
                                       Console.WriteLine("VSign Stop");
                                          
                                   }
                                   else if (mouseX < vSignX - Screen.PrimaryScreen.Bounds.Width * 0.5)
                                   {
                                       Console.WriteLine("swipe left");
                                       vSign = false;
                                       t.Stop();
                                       Console.WriteLine("VSign Stop");
                                   }
                               }
                                MouseInjection.SetCursorPos((int)mouseX, (int)mouseY);
                           }
                           else
                           {
                               Console.WriteLine("Swipe...");
                           }
                        }
                        break;

                }
            }
            else
            {
                this.Dispatcher.Invoke(new Action(() => OnTouchlessControllerUXEvent(data)));
            }
        }

        public static DependencyObject GetScrollViewer(DependencyObject o)
        {
            // Return the DependencyObject if it is a ScrollViewer
            if (o is ScrollViewer)
            { return o; }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = VisualTreeHelper.GetChild(o, i);

                var result = GetScrollViewer(child);
                if (result == null)
                {
                    continue;
                }
                else
                {
                    return result;
                }
            }
            return null;

        }
    }
}
