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

        public CoordinateType CoordinateType
        {
            get { return (CoordinateType)GetValue(CoordinateTypeProperty); }
            set { SetValue(CoordinateTypeProperty, value); }
        }
        public static readonly DependencyProperty CoordinateTypeProperty =
            DependencyProperty.Register("CoordinateType", typeof(CoordinateType), typeof(NavPointEditDlg), new PropertyMetadata(CoordinateType.Local));

        public string Type
        {
            get { return (CoordinateType)GetValue(CoordinateTypeProperty) == CoordinateType.World ? "World Coordinate" : "Local Coordinate"; }
        }

        public Visibility WorldGridVisible
        {
            get { return (CoordinateType)GetValue(CoordinateTypeProperty) == CoordinateType.World ? Visibility.Visible : Visibility.Hidden  ; }
        }

        public Visibility LocalGridVisible
        {
            get { return (CoordinateType)GetValue(CoordinateTypeProperty) == CoordinateType.Local ? Visibility.Visible : Visibility.Hidden; }
        }

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

        Point PctPoint;

        public NavPointEditDlg(Point pp, BasePoint a = null, CoordinateType t = CoordinateType.Local ) // if a == null it is for a new point
        {
            DataContext = this;
            PctPoint = pp;
            Original = a;

            if (Original != null)
            {
                if (Original is WorldPoint)
                {
                    Utm = ((WorldPoint)Original).Utm;
                    Wgs84 = (Wgs84)Utm;
                    CoordinateType = CoordinateType.World;
                }
                else if (Original is LocalPoint)
                {
                    XY = ((LocalPoint)Original).XY;
                    CoordinateType = CoordinateType.Local;
                }
            }
            else
                CoordinateType = t;

            InitializeComponent();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            while (autoUpdateInProgress)
                Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));

            if (CoordinateType == CoordinateType.World)
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

        static bool autoUpdateInProgress = false;

        static void OnWgs84Changed(DependencyObject source, DependencyPropertyChangedEventArgs ea)
        {
            if (!autoUpdateInProgress)
            {
                autoUpdateInProgress = true;
                Wgs84 a = (Wgs84)source.GetValue(Wgs84Property);
                if (a != null)
                    source.SetValue(UtmProperty, a.GetUtm());
            }
            else
                autoUpdateInProgress = false;
        }

        static void OnUtmChanged(DependencyObject source, DependencyPropertyChangedEventArgs ea)
        {
            if (!autoUpdateInProgress)
            {
                autoUpdateInProgress = true;
                Utm a = (Utm)source.GetValue(UtmProperty);
                if (a != null)
                    source.SetValue(Wgs84Property, (Wgs84)a);
            }
            else
                autoUpdateInProgress = false;
        }
    }
}
