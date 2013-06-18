using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dynamight.ImageProcessing.CameraCalibration.Utils
{
    public partial class ProjectorForm : Form
    {
        public ProjectorForm()
        {
            InitializeComponent();
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        public PictureBox Picture
        {
            get { return Display; }
        }
    }
}
