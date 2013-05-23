using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
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

namespace KinectOutput
{
    /// <summary>
    /// Interaction logic for ConfidencePrintout.xaml
    /// </summary>
    public partial class ConfidencePrintout : UserControl
    {
        private Dictionary<string, Confidence> data = new Dictionary<string, Confidence>();
        private Dictionary<string, List<string>> perDevice = new Dictionary<string, List<string>>();
        public ConfidencePrintout()
        {
            InitializeComponent();
        }

        public void Inactivate(string sensorId)
        {
            if (perDevice.ContainsKey(sensorId))
            {
                perDevice[sensorId].ForEach(row => data[row].Active = false);
            }
        }

        public bool Measure(Skeleton skeleton, string sensorId)
        {
            var id = sensorId + skeleton.TrackingId;
            Confidence conf;
            if (data.ContainsKey(id))
            {
                conf = data[id];
                conf.Update(skeleton);
            }
            else
            {
                conf = data[id] = new Confidence(skeleton);
                perDevice[sensorId] = perDevice.ContainsKey(sensorId) ? perDevice[sensorId] : new List<string>();
                perDevice[sensorId].Add(id);
            }
            update();
            var res = IsOK(conf);
            

            return res;
        }

        private bool IsOK(Confidence conf)
        {
            return conf.InferredConfidence > 0.7 && conf.LengthConfidence > 0.7;
        }

        private void update()
        {
            int c = 1;
            TextDisplay.Text = string.Join("\n\n", data.Values.Where(row => row.Active).Select(row => string.Format("Character: {0}\nInferred: {1:0.00}\nLength: {2:0.00}\nOk? {3}", c++, row.InferredConfidence, row.LengthConfidence, IsOK(row) ? "YES" : "NO")));
        }
    }
}
