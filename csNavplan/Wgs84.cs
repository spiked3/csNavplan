using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace csNavplan
{

    public class Wgs84 : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] String T = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(T));
        }

        #endregion

        public double Longitude { get { return _Longitude; } set { _Longitude = value; OnPropertyChanged(); } }
        double _Longitude = 0;
        public double Latitude { get { return _Latitude; } set { _Latitude = value; OnPropertyChanged(); } }
        double _Latitude = 0;

        public bool isNotZero { get { return Math.Abs(_Longitude) + Math.Abs(_Latitude) != 0.0; } }

        public static Wgs84 FromUtm(Utm u)
        {
            bool isNorthHemisphere = u.Zone.Last() >= 'N';

            var diflat = -0.00066286966871111111111111111111111111;
            var diflon = -0.0003868060578;

            var zone = int.Parse(u.Zone.Remove(u.Zone.Length - 1));
            var c_sa = 6378137.000000;
            var c_sb = 6356752.314245;
            var e2 = Math.Pow((Math.Pow(c_sa, 2) - Math.Pow(c_sb, 2)), 0.5) / c_sb;
            var e2cuadrada = Math.Pow(e2, 2);
            var c = Math.Pow(c_sa, 2) / c_sb;
            var x = u.Easting - 500000;
            var y = isNorthHemisphere ? u.Northing : u.Northing - 10000000;

            var s = ((zone * 6.0) - 183.0);
            var lat = y / (c_sa * 0.9996);
            var v = (c / Math.Pow(1 + (e2cuadrada * Math.Pow(Math.Cos(lat), 2)), 0.5)) * 0.9996;
            var a = x / v;
            var a1 = Math.Sin(2 * lat);
            var a2 = a1 * Math.Pow((Math.Cos(lat)), 2);
            var j2 = lat + (a1 / 2.0);
            var j4 = ((3 * j2) + a2) / 4.0;
            var j6 = ((5 * j4) + Math.Pow(a2 * (Math.Cos(lat)), 2)) / 3.0;
            var alfa = (3.0 / 4.0) * e2cuadrada;
            var beta = (5.0 / 3.0) * Math.Pow(alfa, 2);
            var gama = (35.0 / 27.0) * Math.Pow(alfa, 3);
            var bm = 0.9996 * c * (lat - alfa * j2 + beta * j4 - gama * j6);
            var b = (y - bm) / v;
            var epsi = ((e2cuadrada * Math.Pow(a, 2)) / 2.0) * Math.Pow((Math.Cos(lat)), 2);
            var eps = a * (1 - (epsi / 3.0));
            var nab = (b * (1 - epsi)) + lat;
            var senoheps = (Math.Exp(eps) - Math.Exp(-eps)) / 2.0;
            var delt = Math.Atan(senoheps / (Math.Cos(nab)));
            var tao = Math.Atan(Math.Cos(delt) * Math.Tan(nab));

            Wgs84 w_out = new Wgs84
            {
                Longitude = ((delt * (180.0 / Math.PI)) + s) + diflon,
                Latitude = ((lat + (1 + e2cuadrada * Math.Pow(Math.Cos(lat), 2) - (3.0 / 2.0) * e2cuadrada * Math.Sin(lat) * Math.Cos(lat) * (tao - lat)) * (tao - lat)) * (180.0 / Math.PI)) + diflat
            };
            return w_out;
        }

        public Utm AsUtm()
        {
            return Utm.FromWgs84(this);
        }

        public override string ToString()
        {
            return $"{Latitude:F5},{Longitude:F5}";
        }

        public static double gps2m(double lat_a, double lng_a, double lat_b, double lng_b)
        {
            double pk = 180.0 / Math.PI;
            double a1 = lat_a / pk;
            double a2 = lng_a / pk;
            double b1 = lat_b / pk;
            double b2 = lng_b / pk;

            double t1 = Math.Cos(a1) * Math.Cos(a2) * Math.Cos(b1) * Math.Cos(b2);
            double t2 = Math.Cos(a1) * Math.Sin(a2) * Math.Cos(b1) * Math.Sin(b2);
            double t3 = Math.Sin(a1) * Math.Sin(b1);
            double tt = Math.Acos(t1 + t2 + t3);

            return 6366000 * tt;
        }
    }

    public class Wgs84Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(string))
                return (value as Wgs84)?.ToString();
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(Wgs84))
            {
                try
                {
                    var tokens = ((string)value).Split(',');
                    double x = double.Parse(tokens[0]);
                    double y = double.Parse(tokens[1]);
                    return new Wgs84 { Longitude = y, Latitude = x };
                }
                catch (Exception)
                {
                    System.Diagnostics.Trace.WriteLine($"Wgs84Converter::ConvertBack threw exception, value = {value}");
                }
            }
            return null;
        }

    }
}