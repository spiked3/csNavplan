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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace csNavplan
{
    public partial class NavPointEditDlg : Window
    {
        BasePoint Original;
        public BasePoint Final { get; set; }

        public Utm Utm
        {
            get { return (Utm)GetValue(UtmProperty); }
            set { SetValue(UtmProperty, value); }
        }
        public static readonly DependencyProperty UtmProperty =
            DependencyProperty.Register("Utm", typeof(Utm), typeof(NavPointEditDlg), new PropertyMetadata(OnUtmChanged));

        public Wgs84 Wgs84
        {
            get { return (Wgs84)GetValue(Wgs84Property); }
            set { SetValue(Wgs84Property, value); }
        }
        public static readonly DependencyProperty Wgs84Property =
            DependencyProperty.Register("Wgs84", typeof(Wgs84), typeof(NavPointEditDlg), new PropertyMetadata(OnWgs84Changed));

        public Point XY
        {
            get { return (Point)GetValue(XYProperty); }
            set { SetValue(XYProperty, value); }
        }
        public static readonly DependencyProperty XYProperty =
            DependencyProperty.Register("XY", typeof(Point), typeof(NavPointEditDlg), new PropertyMetadata(new Point()));

        public bool? IsWorld
        {
            get { return (bool?)GetValue(IsWorldProperty); }
            set { SetValue(IsWorldProperty, value); }
        }
        public static readonly DependencyProperty IsWorldProperty =
            DependencyProperty.Register("IsWorld", typeof(bool?), typeof(NavPointEditDlg), new PropertyMetadata(false, OnIsWorldChanged));

        public Visibility WorldGridVisible
        {
            get { return (Visibility)GetValue(WorldGridVisibleProperty); }
            set { SetValue(WorldGridVisibleProperty, value); }
        }
        public static readonly DependencyProperty WorldGridVisibleProperty =
            DependencyProperty.Register("WorldGridVisible", typeof(Visibility), typeof(NavPointEditDlg), new PropertyMetadata(Visibility.Hidden));

        public Visibility LocalGridVisible
        {
            get { return (Visibility)GetValue(LocalGridVisibleProperty); }
            set { SetValue(LocalGridVisibleProperty, value); }
        }
        public static readonly DependencyProperty LocalGridVisibleProperty =
            DependencyProperty.Register("LocalGridVisible", typeof(Visibility), typeof(NavPointEditDlg), new PropertyMetadata(Visibility.Visible));

        Point PctPoint;

        public NavPointEditDlg(Point pp)  // create a new point w given pctPoint
        {
            PctPoint = pp;
            DataContext = this;
            IsWorld = false;
            InitializeComponent();
        }

        public NavPointEditDlg(BasePoint a)
        {
            System.Diagnostics.Debug.Assert(a != null);
            PctPoint = a.PctPoint;

            if (a is WorldPoint)
            {
                Utm = ((WorldPoint)a).Utm;
                Wgs84 = (Wgs84)Utm;
                IsWorld = true;
            }
            else if (a is LocalPoint)
            {
                XY = ((LocalPoint)a).XY;
                IsWorld = false;
            }

            Original = a;
            DataContext = this;
            InitializeComponent();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (IsWorld ?? false)
                Final = new WorldPoint(PctPoint, Utm);
            else
                Final = new LocalPoint(PctPoint, XY);

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        static void OnIsWorldChanged(DependencyObject source, DependencyPropertyChangedEventArgs ea)
        {
            var v = ((NavPointEditDlg)source).IsWorld ?? false;
            source.SetValue(WorldGridVisibleProperty, v ? Visibility.Visible : Visibility.Hidden);
            source.SetValue(LocalGridVisibleProperty, v ? Visibility.Hidden : Visibility.Visible);
        }

        static bool dontUpdate = false;
        //static readonly TimeSpan updateDelay = new TimeSpan(0, 0, 1);
        //public DispatcherTimer dt;  // used by edit dlg to re-calc wgs <-> utm

        static void OnWgs84Changed(DependencyObject source, DependencyPropertyChangedEventArgs ea)
        {
            if (!dontUpdate)
            {
                dontUpdate = true;
                Wgs84 a = (Wgs84)source.GetValue(Wgs84Property);
                if (a != null)
                    source.SetValue(UtmProperty, a.GetUtm());
            }
            else
                dontUpdate = false;
        }

        static void OnUtmChanged(DependencyObject source, DependencyPropertyChangedEventArgs ea)
        {
            if (!dontUpdate)
            {
                Utm a = (Utm)source.GetValue(UtmProperty);
                if (a != null)
                    source.SetValue(Wgs84Property, (Wgs84)a);
            }
            else
                dontUpdate = false;
        }
    }
}
