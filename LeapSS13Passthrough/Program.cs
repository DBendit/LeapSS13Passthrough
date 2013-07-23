using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Leap;
using WindowsInput;

/*
 * TODO:
 *  - Vertical movement
 *  - Finger tracking
 *  
 * IDEAS:
 *  - Keyboard toggle for rotation?
 *  - Individual finger tracking (holding scalpel)
 */

namespace LeapSS13Passthrough
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create a sample listener and controller
            SS13Listener listener = new SS13Listener(true);
            Controller controller = new Controller();
            controller.SetPolicyFlags(Leap.Controller.PolicyFlag.POLICYBACKGROUNDFRAMES);

            // Have the sample listener receive events from the controller
            controller.AddListener(listener);

            // Keep this process running until Enter is pressed
            Console.WriteLine("Press Enter to quit...");
            Console.ReadLine();

            // Remove the sample listener when done
            controller.RemoveListener(listener);
            controller.Dispose();
        }
    }

    class SS13Listener : Listener
    {
        private Object thisLock = new Object();
        private Frame m_oLastFrame;
        private InputSimulator m_is = new InputSimulator();
        private bool m_bInGame;
        private bool m_bRotating;
        private long m_lLastClick;
        private bool m_bClicking;

        private const double PIXELSPERMM = 15.748031; // 400 dpi
        private const int DOUBLECLICKTIME = 500000; // Microseconds

        public SS13Listener(bool InGame): base()
        {
            m_bInGame = InGame;
        }

        private void SafeWriteLine(String line)
        {
#if DEBUG
            lock (thisLock)
            {
                Console.WriteLine(line);
            }
#endif
        }

        public override void OnInit(Controller controller)
        {
            SafeWriteLine("Initialized");
        }

        public override void OnConnect(Controller controller)
        {
            SafeWriteLine("Connected");
            //controller.EnableGesture(Gesture.GestureType.TYPECIRCLE);
            controller.EnableGesture(Gesture.GestureType.TYPEKEYTAP);
            //controller.EnableGesture(Gesture.GestureType.TYPESCREENTAP);
            //controller.EnableGesture(Gesture.GestureType.TYPESWIPE);
        }

        public override void OnDisconnect(Controller controller)
        {
            //Note: not dispatched when running in a debugger.
            SafeWriteLine("Disconnected");
        }

        public override void OnExit(Controller controller)
        {
            SafeWriteLine("Exited");
        }

        public override void OnFrame(Controller controller)
        {
            // Get the most recent frame and report some basic information
            Frame frame = controller.Frame();

            if (!frame.Hands.Empty)
            {
                // Get the first hand
                Hand hand = frame.Hands[0];

                if (m_oLastFrame != null)
                {
                    //if (hand.RotationProbability(m_lastFrame) > .25)
                    if (m_bRotating)
                        RotateHand(hand.RotationAngle(m_oLastFrame, Vector.Left), hand.RotationAngle(m_oLastFrame, Vector.Backward), hand.SphereRadius);
                    else
                        MoveHand(hand.Translation(m_oLastFrame));
                    
                }
            }

            foreach (Gesture gest in frame.Gestures())
            {
                if (gest.Type == Gesture.GestureType.TYPEKEYTAP)
                {
                    SafeWriteLine("Click - " + frame.Timestamp);
                    if (m_bClicking && frame.Timestamp - m_lLastClick < DOUBLECLICKTIME)
                    {
                        m_bRotating = !m_bRotating;
                        m_lLastClick = 0;
                        m_bClicking = false;
                        SafeWriteLine("Double - " + frame.Timestamp);
                    }
                    else
                    { 
                        m_lLastClick = frame.Timestamp;
                        m_bClicking = true;
                    }
                    
                }
            }

            if (m_bClicking && frame.Timestamp - m_lLastClick >= DOUBLECLICKTIME)
            {
                m_is.Mouse.LeftButtonClick();
                m_lLastClick = 0;
                m_bClicking = false;
                SafeWriteLine("Single - " + frame.Timestamp);
            }

            m_oLastFrame = frame;
        }

        private void MoveHand(Vector vec)
        {
            m_is.Mouse.MoveMouseBy((int)Math.Floor(vec.x * PIXELSPERMM), (int)Math.Floor(vec.z * PIXELSPERMM));
        }

        private void RotateHand(double pitch, double roll, double handRadiusInMM)
        {

            double rollDistance = roll * handRadiusInMM;
            double pitchDistance = pitch * handRadiusInMM;

            if (m_bInGame)
            {
                m_is.Mouse.RightButtonDown();
                m_is.Mouse.MoveMouseBy((int)Math.Floor(rollDistance * PIXELSPERMM), (int)Math.Floor(pitchDistance * PIXELSPERMM));
                m_is.Mouse.RightButtonUp();
            }
        }
    }
}
