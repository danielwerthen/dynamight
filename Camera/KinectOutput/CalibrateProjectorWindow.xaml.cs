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
        NotifyVectorValue<double>[] RT = new NotifyVectorValue<double>[4];
        NotifyVectorValue<double>[] A = new NotifyVectorValue<double>[3];
        NotifyVectorValue<bool> normalize = new NotifyVectorValue<bool>(new bool[] { true, true, true, false });
        Vector<double>[] RTinit = null;
        Vector<double>[] Ainit = null;

        public static void Calibrate(string projectorId, Action redraw)
        {
            CalibrateProjectorWindow window = new CalibrateProjectorWindow();
            window.projectorId = projectorId;

            var init = Coordinator.GetCalibration(projectorId);
            if (init.HasValue)
            {
                if (init.Value.F1 != null && init.Value.F2 != null && init.Value.F3 != null && init.Value.P0 != null)
                    window.RTinit = new Vector<double>[] { init.Value.F1, init.Value.F2, init.Value.F3, init.Value.P0 };
                if (init.Value.A1 != null && init.Value.A2 != null && init.Value.A3 != null)
                    window.Ainit = new Vector<double>[] { init.Value.A1, init.Value.A2, init.Value.A3 };
            }
            if (window.RTinit == null)
            {
                window.RTinit = (new double[][] { new double[] { 1, 0, 0, 0 },
                    new double[] { 0, 1, 0, 0 },
                    new double[] { 0, 0, 1, 0 },
                    new double[] { 0, 0, 0, 1 }, }).Select(row => new DenseVector(row)).ToArray();
            }
            if (window.Ainit == null)
            {
                window.Ainit = (new double[][] { new double[] { 1, 0, 0 },
                    new double[] { 0, 1, 0},
                    new double[] { 0, 0, 1 }, }).Select(row => new DenseVector(row)).ToArray();
            }
            window.redraw = redraw;
            window.Show();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitMatrix(4, 4, RTGrid, RT, RTinit);
            InitMatrix(3, 3, AGrid, A, Ainit);
            InitNorms();
        }

        private void InitNorms()
        {
            var names = new string[] { "X", "Y", "Z", "H" };
            for (var r = 0; r < 3; r++)
            {
                CheckBox cb = new CheckBox();
                Grid.SetRow(cb, r);
                cb.Content = names[r] + "?";
                cb.Checked += (o, e) => this.update();
                cb.Unchecked += (o, e) => this.update();
                cb.IsChecked = normalize[r];
                Binding b = new Binding(names[r]);
                b.Source = normalize;
                b.Mode = BindingMode.TwoWay;
                BindingOperations.SetBinding(cb, CheckBox.IsCheckedProperty, b);
                NormGrid.Children.Add(cb);
            }
        }


        private void update()
        {
            if (RT.All(row => row != null))
            {
                Coordinator.SetResult(projectorId, new CalibrationResult()
                {
                    F1 = normalize[0] ? ((DenseVector)RT[0]).Normalize(1) : RT[0],
                    F2 = normalize[1] ? ((DenseVector)RT[1]).Normalize(1) : RT[1],
                    F3 = normalize[2] ? ((DenseVector)RT[2]).Normalize(1) : RT[2],
                    P0 = RT[3],
                    A1 = A[0],
                    A2 = A[1],
                    A3 = A[2],
                });
            }
            this.redraw();
        }

        private void InitMatrix(int rows, int cols, Grid grid, NotifyVectorValue<double>[] store, Vector<double>[] init)
        {
            var names = new string[] { "X", "Y", "Z", "H" };
            for (var c = 0; c < cols; c++)
            {
                var vals = store[c] = init != null ? new NotifyVectorValue<double>(init[c]) : new NotifyVectorValue<double>();
                for (var r = 0; r < rows; r++)
                {
                        var tb = new TextBox();
                        tb.GotFocus += (o, e) => { if (tb.IsFocused) tb.SelectAll(); };
                        Grid.SetColumn(tb, c);
                        Grid.SetRow(tb, r);
                        vals.PropertyChanged += (o, e) => this.update();
                        Binding valBind = new Binding(names[r]);
                        valBind.Mode = BindingMode.TwoWay;
                        valBind.NotifyOnValidationError = true;
                        valBind.ValidatesOnExceptions = true;
                        valBind.Source = vals;
                        BindingOperations.SetBinding(tb, TextBox.TextProperty, valBind);
                        grid.Children.Add(tb);
                }

            }
        }
    }

    public class NotifyVectorValue<T> : INotifyPropertyChanged
    {
        public NotifyVectorValue()
        {
        }
        
        public NotifyVectorValue(IEnumerable<T> vec)
        {
            X = vec.ElementAt(0);
            Y = vec.ElementAt(1);
            Z = vec.ElementAt(2);
            if (vec.Count() >= 4)
                H = vec.ElementAt(3);
        }

        public static implicit operator Vector<double>(NotifyVectorValue<T> vec)
        {
            return new DenseVector(new double[] { (double)(object)vec.X, (double)(object)vec.Y, (double)(object)vec.Z, (double)(object)vec.H });
        }

        public T this[int idx]
        {
            get
            {
                if (idx == 0) return X;
                if (idx == 1) return Y;
                if (idx == 2) return Z;
                if (idx == 3) return H;
                return default(T);
            }
        }

        private T _x;

        public T X
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

        private T _y;

        public T Y
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

        private T _z;

        public T Z
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

        private T _h;

        public T H
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
