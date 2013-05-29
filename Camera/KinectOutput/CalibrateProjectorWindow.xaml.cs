using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Generic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace KinectOutput
{
    /// <summary>
    /// Interaction logic for CalibrateProjectorWindowxaml.xaml
    /// </summary>
    public partial class CalibrateProjectorWindow : Window
    {
        public CalibrateProjectorWindow()
        {
            InitializeComponent();
        }

        private string projectorId;
        private Action redraw;
        public static void Calibrate(string projectorId, Action redraw)
        {
            CalibrateProjectorWindow window = new CalibrateProjectorWindow();
            window.projectorId = projectorId;

            var init = Coordinator.LoadCalibration(projectorId);
            if (init.HasValue)
                window.RTinit = new Vector<double>[] { init.Value.F1, init.Value.F2, init.Value.F3, init.Value.P0 };
            window.redraw = redraw;
            window.Show();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitMatrix(4, 4, RTGrid, RT, RTinit);
        }

        NotifyVectorValue[] RT = new NotifyVectorValue[4];
        Vector<double>[] RTinit = null;

        private void update()
        {
            if (RT.All(row => row != null))
            {
                Coordinator.SetResult(projectorId, new CalibrationResult()
                {
                    F1 = RT[0],
                    F2 = RT[1],
                    F3 = RT[2],
                    P0 = RT[3],
                });
            }
            this.redraw();
        }

        private void InitMatrix(int rows, int cols, Grid grid, NotifyVectorValue[] store, Vector<double>[] init)
        {
            for (var c = 0; c < cols; c++)
            {
                var vals = store[c] = init != null ? new NotifyVectorValue(init[c]) : new NotifyVectorValue();
                for (var r = 0; r < rows; r++)
                {
                        var tb = new TextBox();
                        tb.GotFocus += (o, e) => { if (tb.IsFocused) tb.SelectAll(); };
                        Grid.SetColumn(tb, c);
                        Grid.SetRow(tb, r);
                        vals.PropertyChanged += (o, e) => this.update();
                        Binding valBind = new Binding("Value");
                        valBind.Mode = BindingMode.TwoWay;
                        valBind.NotifyOnValidationError = true;
                        valBind.ValidatesOnExceptions = true;
                        valBind.Source = vals[r];
                        BindingOperations.SetBinding(tb, TextBox.TextProperty, valBind);
                        grid.Children.Add(tb);
                }

            }
        }
    }

    public class NotifyVectorValue : INotifyPropertyChanged
    {
        public NotifyVectorValue()
        {
        }
        public NotifyVectorValue(Vector<double> vec)
        {
            X = vec[0];
            Y = vec[1];
            Z = vec[2];
            H = vec[3];
        }

        public static implicit operator Vector<double>(NotifyVectorValue vec)
        {
            return new DenseVector(new double[] { vec.X, vec.Y, vec.Z, vec.H });
        }

        public double this[int idx]
        {
            get
            {
                if (idx == 0) return X;
                if (idx == 1) return Y;
                if (idx == 2) return Z;
                if (idx == 3) return H;
                return 0;
            }
        }

        private double _x;

        public double X
        {
            get
            { 
                return _x; 
            }
            set 
            { 
                _x = value;
                OnPropertyChanged("X");
            }
        }

        private double _y;

        public double Y
        {
            get
            {
                return _y;
            }
            set
            {
                _y = value;
                OnPropertyChanged("Y");
            }
        }

        private double _z;

        public double Z
        {
            get
            {
                return _z;
            }
            set
            {
                _z = value;
                OnPropertyChanged("Z");
            }
        }

        private double _h;

        public double H
        {
            get
            {
                return _h;
            }
            set
            {
                _h = value;
                OnPropertyChanged("H");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}
