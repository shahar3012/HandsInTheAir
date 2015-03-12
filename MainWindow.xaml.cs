using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
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
using KeyBoardInjectService;
using Newtonsoft.Json;
using System.IO;

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
        bool vSign = false;
        bool Pinch = false;
        int vSignX = 0;
        int vSignY = 0;
        double lastZ = -1;

        Gestures loadedGestures;

        public MainWindow()
        {
            InitializeComponent();
        }

        private Gestures getGestures()
        {
            JsonSerializer serializer = new JsonSerializer();
            try
            {
                using (StreamReader sr = new StreamReader(@"C:\Users\Hackathon-IDF\Documents\gestures.json"))
                {
                    using (JsonTextReader reader = new JsonTextReader(sr))
                    {
                        return serializer.Deserialize<Gestures>(reader);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.loadedGestures = getGestures();

            StartRealSense();

            UpdateConfiguration();

            StartFrameLoop();
        }
        System.Timers.Timer tPinch = new System.Timers.Timer();
        
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopRealSense();
        }

        private void StartRealSense()
        {
            Console.WriteLine("Starting HandsInTheAir");
            t.Enabled = true;
            t.Interval = 2000;
            t.Tick += t_Tick;
            tPinch.Enabled = true;
            tPinch.Interval = 500;
            tPinch.Elapsed += tPinch_Tick;
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

        void tPinch_Tick(object sender, EventArgs e)
        {
            ProcessGestures processGestures = this.loadedGestures.gestures.FirstOrDefault((process) => { return process.processName == ProcessHandle.getCurrProcessName(); });
            if (processGestures != null)
            {
                GestureDetails gestureDetails = processGestures.shortcuts.FirstOrDefault((gesture) => { return gesture.gestureType == "pinch"; });
                if (gestureDetails != null)
                {
                    Pinch = false;
                    tPinch.Stop();
                    ReleaseKeyList(gestureDetails.keys);
                }
            }
        }

        void t_Tick(object sender, EventArgs e)
        {
                vSign = false;
                vSignX = 0;
                vSignY = 0;
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
            ptc.AddGestureActionMapping("two_fingers_pinch_open", PXCMTouchlessController.Action.Action_None, new PXCMTouchlessController.OnFiredActionDelegate(OnFullPinch));


            pInfo.config = PXCMTouchlessController.ProfileInfo.Configuration.Configuration_Allow_Zoom;

            rc = ptc.SetProfile(pInfo);
            Console.WriteLine("Setting Profile: " + rc.ToString());
        }

        private void StartFrameLoop()
        {
            psm.StreamFrames(false);
        }


        private void OnFullPinch(PXCMTouchlessController.Action data)
        {
            ProcessGestures processGestures = this.loadedGestures.gestures.FirstOrDefault((process) => { return process.processName == ProcessHandle.getCurrProcessName(); });
            if (processGestures != null)
            {       
                GestureDetails gestureDetails = processGestures.shortcuts.FirstOrDefault((gesture) => { return gesture.gestureType == "pinch"; });
                if (gestureDetails != null)
                {
                    Console.WriteLine("pinch");
                    tPinch.Stop();
                    tPinch.Start();
                    if (!Pinch)
                    {
                        PressKeyList(gestureDetails.keys);
                    }

                    Pinch = true;
                }
            }
        }


       private void OnVSign(PXCMTouchlessController.Action data)
       {
           ProcessGestures processGestures = this.loadedGestures.gestures.FirstOrDefault((process) => { return process.processName == ProcessHandle.getCurrProcessName(); });
           if (processGestures != null)
           {
               GestureDetails gestureDetails = processGestures.shortcuts.FirstOrDefault((gesture) => { return gesture.gestureType == "swipeRight" || gesture.gestureType == "swipeLeft" || gesture.gestureType == "swipeUp" || gesture.gestureType == "swipDown"; });
                if (gestureDetails != null)
                {
                    vSign = true;
                    Console.WriteLine("VSign Start");
                    vSignX = (int)MouseInjection.getCursorPos().X;
                    vSignY = (int)MouseInjection.getCursorPos().Y;
                    t.Start();
                }
           }
       }

       System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
       

        private void OnTouchlessControllerUXEvent(PXCMTouchlessController.UXEventData data)
        {
            if (this.Dispatcher.CheckAccess())
            {
            string name = ProcessHandle.getCurrProcessName();

            ProcessGestures processGestures = 
                this.loadedGestures.gestures.FirstOrDefault(p => p.processName.Equals(name));
            if (processGestures != null)
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
                                GestureDetails gestureDetails = processGestures.shortcuts.FirstOrDefault((gesture) => { return gesture.gestureType == "tap";});
                                if (gestureDetails != null)
                                {
                                    if  (HandleHand.EnableSelect)
                                    {
                                        Console.WriteLine("Select");
                                        PressAndReleaseKeyList(gestureDetails.keys);
                                    }
                                }
                            }
                            break;
                        case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_StartScroll:
                            {
                                Console.WriteLine("Start Scroll");
                                HandleHand.ToggleZoomEnable();
                                HandleHand.ToggleSelectEnable();
                                MouseInjection.PressLeft();
                            }
                            break;
                        case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_EndScroll:
                            {
                                Console.WriteLine("End Scroll");
                                MouseInjection.ReleaseLeft();
                                HandleHand.ToggleZoomEnable();
                                HandleHand.ToggleSelectEnable();
                            }
                            break;
                        case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_Scroll:
                            {
                                Console.WriteLine("Scrolling...");
                                Console.WriteLine("X: " + data.position.x + " Y: " + data.position.y);
                                double mouseX = data.position.x * Screen.PrimaryScreen.Bounds.Width;
                                double mouseY = data.position.y * Screen.PrimaryScreen.Bounds.Height;
                                MouseInjection.SetCursorPos((int)mouseX, (int)mouseY);
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
                                    if (HandleHand.ZoomEnabled)
                                    {
                                        if (data.position.z > lastZ + 0.01)
                                        {
                                            GestureDetails gestureDetails = processGestures.shortcuts.FirstOrDefault((gesture) => { return gesture.gestureType == "zoomout"; });
                                            if (gestureDetails != null)
                                            {
                                                PressAndReleaseKeyList(gestureDetails.keys);
                                                Console.WriteLine("z: " + data.position.z);
                                            }
                                        }
                                        else if (data.position.z < lastZ - 0.01)
                                        {
                                             GestureDetails gestureDetails = processGestures.shortcuts.FirstOrDefault((gesture) => { return gesture.gestureType == "zoomin"; });
                                             if (gestureDetails != null)
                                             {
                                                 PressAndReleaseKeyList(gestureDetails.keys);
                                                 Console.WriteLine("z: " + data.position.z);
                                             }
                                        }

                                        lastZ = data.position.z;
                                    }
                            }
                            break;
                        case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_StartZoom:
                            {
                                //  HandleHand.ToggleSelectEnable();
                                lastZ = data.position.z;
                                Console.WriteLine("StartZoom");
                            }
                            break;
                        case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_EndZoom:
                            {
                                //   HandleHand.ToggleSelectEnable();
                                lastZ = -1;
                                Console.WriteLine("EndZoom");
                            }
                            break;
                        case PXCMTouchlessController.UXEventData.UXEventType.UXEvent_CursorMove:
                            {
                                if (true)
                                {

                                    double mouseX = data.position.x * Screen.PrimaryScreen.Bounds.Width;
                                    double mouseY = data.position.y * Screen.PrimaryScreen.Bounds.Height;

                                    if (vSign)
                                    {
                                        //Right-Left
                                        if (mouseX > vSignX + Screen.PrimaryScreen.Bounds.Width * 0.5 && vSign)
                                        {
                                             GestureDetails gestureDetails = processGestures.shortcuts.FirstOrDefault((gesture) => { return gesture.gestureType == "swipeRight"; });
                                             if (gestureDetails != null)
                                             {
                                                 Console.WriteLine("swipe right");
                                                 vSign = false;
                                                 t.Stop();
                                                 PressAndReleaseKeyList(gestureDetails.keys);
                                                 Console.WriteLine("VSign Stop");
                                             }
                                        }
                                        else if (mouseX < vSignX - Screen.PrimaryScreen.Bounds.Width * 0.5 && vSign)
                                        {
                                             GestureDetails gestureDetails = processGestures.shortcuts.FirstOrDefault((gesture) => { return gesture.gestureType == "swipeLeft"; });
                                             if (gestureDetails != null)
                                             {
                                                 Console.WriteLine("swipe left");
                                                 vSign = false;
                                                 t.Stop();
                                                 PressAndReleaseKeyList(gestureDetails.keys);
                                                 Console.WriteLine("VSign Stop");
                                             }
                                        }
                                        //Up-Down
                                        else if (mouseY > vSignY + Screen.PrimaryScreen.Bounds.Height * 0.5 && vSign)
                                        {
                                            GestureDetails gestureDetails = processGestures.shortcuts.FirstOrDefault((gesture) => { return gesture.gestureType == "swipeDown"; });
                                            if (gestureDetails != null)
                                            {
                                                Console.WriteLine("swipe down");
                                                vSign = false;
                                                t.Stop();
                                                PressAndReleaseKeyList(gestureDetails.keys);
                                                Console.WriteLine("VSign Stop");
                                            }
                                        }
                                        else if (mouseY < vSignY - Screen.PrimaryScreen.Bounds.Height * 0.5 && vSign)
                                        {
                                            GestureDetails gestureDetails = processGestures.shortcuts.FirstOrDefault((gesture) => { return gesture.gestureType == "swipeUp"; });
                                            if (gestureDetails != null)
                                            {
                                                Console.WriteLine("swipe up");
                                                vSign = false;
                                                t.Stop();
                                                PressAndReleaseKeyList(gestureDetails.keys);
                                                Console.WriteLine("VSign Stop");
                                            }
                                        }
                                    }

                                    MouseInjection.SetCursorPos((int)mouseX, (int)mouseY);
                                }
                            }
                            break;
                    }
            }
        }
            else
            {
                this.Dispatcher.Invoke(new Action(() => OnTouchlessControllerUXEvent(data)));
            }
        }

        public enum KEYSIGN
        {
            LEFTMOUSEPRESS = -1,
            RIGHTMOUSEPRESS = -2,
            MOUSEWHEELDOWN = -3,
            MOUSEWHEELUP = -4

        };

        public void PressAndReleaseKeyList(int[] keys)
        {
            PressKeyList(keys);
            ReleaseKeyList(keys);
        }

        public void PressKeyList(int[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                PressByKeyCode(keys[i]);
            }
        }

        public void ReleaseKeyList(int[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                ReleaseByKeyCode(keys[i]);
            }
        }

        public static void SendOrderByKeyCode(int keyCode)
        {
            switch (keyCode)
            {
                case (int)KEYSIGN.LEFTMOUSEPRESS:
                    {
                        MouseInjection.ClickLeftMouseButton();
                        break;
                    }
                case (int)KEYSIGN.RIGHTMOUSEPRESS:
                    {
                        MouseInjection.ClickRightMouseButton();
                        break;
                    }
                case (int)KEYSIGN.MOUSEWHEELUP:
                    {
                        MouseInjection.moveWheelUp();
                        break;
                    }
                case (int)KEYSIGN.MOUSEWHEELDOWN:
                    {
                        MouseInjection.moveWheelDown();
                        break;
                    }
                default:
                    {
                        KeyBoardInjector.InjectKey(keyCode);
                        break;
                    }
            }
        }
        public static void ReleaseByKeyCode(int keyCode)
        {
            switch (keyCode)
            {
                case (int)KEYSIGN.LEFTMOUSEPRESS:
                    {
                        MouseInjection.ReleaseLeft();
                        break;
                    }
                case (int)KEYSIGN.RIGHTMOUSEPRESS:
                    {
                        MouseInjection.ReleaseRight();
                        break;
                    }
                default:
                    {
                        KeyBoardInjector.ReleaseKey(keyCode);
                        break;
                    }
            }
        }
        public static void PressByKeyCode(int keyCode)
        {
            switch (keyCode)
            {
                case (int)KEYSIGN.LEFTMOUSEPRESS:
                    {
                        MouseInjection.PressLeft();
                        break;
                    }
                case (int)KEYSIGN.RIGHTMOUSEPRESS:
                    {
                        MouseInjection.PressRight();
                        break;
                    }
                case (int)KEYSIGN.MOUSEWHEELUP:
                    {
                        MouseInjection.moveWheelUp();
                        break;
                    }
                case (int)KEYSIGN.MOUSEWHEELDOWN:
                    {
                        MouseInjection.moveWheelDown();
                        break;
                    }
                default:
                    {
                        KeyBoardInjector.PressKey(keyCode);
                        break;
                    }
            }
        }
        }
    }
