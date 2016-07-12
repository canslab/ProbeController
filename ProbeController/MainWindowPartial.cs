/*******************************************************************
 * 
 * This file deals with the internal methods, and logiccal components.
 * 
 * 
 *******************************************************************/

using JHStreamReceiver;
using ProbeController.Robot;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ProbeController
{
    public partial class MainWindow : Window
    {
        // Constants related to frame
        private const int FRAME_WIDTH = 640;
        private const int FRAME_HEIGHT = 480;
        private const int FRAME_DPI_X = 96;
        private const int FRAME_DPI_Y = 96;

        // button state
        private bool bLeftLEDButtonClicked = true;
        private bool bRightLEDButtonClicked = true;

        /// <summary>
        /// whether the streaming job is working or not.
        /// </summary>
        public bool bIsCameraWorking = false;

        
    }
}
