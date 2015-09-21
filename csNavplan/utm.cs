using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace csNavplan
{
    // you can verify this with http://home.hiwaay.net/~taylorc/toolbox/geography/geoutm.html

    public class Utm
    {
        const double deg2rad = Math.PI / 180.0;
        public string Zone { get; set; }
        public double Easting { get; set; }
        public double Northing { get; set; }

        static public Utm FromLonLat(double LonDeg, double LatDeg)
        {
            Utm u = new Utm();
            const double a = 6378137; //WGS84
            const double eccSquared = 0.00669438; //WGS84
            const double k0 = 0.9996;

            //Make sure the longitude is between -180.00 .. 179.9
            double LongTemp = (LonDeg + 180) - ((int)((LonDeg + 180) / 360)) * 360 - 180; // -180.00 .. 179.9;

            double LatRad = LatDeg * deg2rad;
            double LongRad = LongTemp * deg2rad;

            int ZoneNumber = ((int)((LongTemp + 180) / 6)) + 1;

            if (LatDeg >= 56.0 && LatDeg < 64.0 && LongTemp >= 3.0 && LongTemp < 12.0)
                ZoneNumber = 32;

            // Special zones for Svalbard
            if (LatDeg >= 72.0 && LatDeg < 84.0)
            {
                if (LongTemp >= 0.0 && LongTemp < 9.0) ZoneNumber = 31;
                else if (LongTemp >= 9.0 && LongTemp < 21.0) ZoneNumber = 33;
                else if (LongTemp >= 21.0 && LongTemp < 33.0) ZoneNumber = 35;
                else if (LongTemp >= 33.0 && LongTemp < 42.0) ZoneNumber = 37;
            }
            double LongOrigin = (ZoneNumber - 1) * 6 - 180 + 3;
            double LongOriginRad = LongOrigin * deg2rad;

            //compute the UTM Zone from the latitude and longitude
            u.Zone = ZoneNumber.ToString() + UTMLetterDesignator(LatDeg);

            const double eccPrimeSquared = (eccSquared) / (1 - eccSquared);

            double N = a / Math.Sqrt(1 - eccSquared * Math.Sin(LatRad) * Math.Sin(LatRad));
            double T = Math.Tan(LatRad) * Math.Tan(LatRad);
            double C = eccPrimeSquared * Math.Cos(LatRad) * Math.Cos(LatRad);
            double A = Math.Cos(LatRad) * (LongRad - LongOriginRad);

            double M = a * ((1 - eccSquared / 4 - 3 * eccSquared * eccSquared / 64 - 5 * eccSquared * eccSquared * eccSquared / 256) * LatRad
                            - (3 * eccSquared / 8 + 3 * eccSquared * eccSquared / 32 + 45 * eccSquared * eccSquared * eccSquared / 1024) * Math.Sin(2 * LatRad)
                            + (15 * eccSquared * eccSquared / 256 + 45 * eccSquared * eccSquared * eccSquared / 1024) * Math.Sin(4 * LatRad)
                            - (35 * eccSquared * eccSquared * eccSquared / 3072) * Math.Sin(6 * LatRad));

            u.Easting = k0 * N * (A + (1 - T + C) * A * A * A / 6 + (5 - 18 * T + T * T + 72 * C - 58 * eccPrimeSquared)
                * A * A * A * A * A / 120) + 500000.0;

            u.Northing = k0 * (M + N * Math.Tan(LatRad) * (A * A / 2 + (5 - T + 9 * C + 4 * C * C) * A * A * A * A / 24
                + (61 - 58 * T + T * T + 600 * C - 330 * eccPrimeSquared) * A * A * A * A * A * A / 720));
            if (LatDeg < 0)
                u.Northing += 10000000.0; //10000000 meter offset for southern hemisphere

            return u;
        }

        static char UTMLetterDesignator(double Lat)
        {
            char LetterDesignator;
            if ((84 >= Lat) && (Lat >= 72)) LetterDesignator = 'X';
            else if ((72 > Lat) && (Lat >= 64)) LetterDesignator = 'W';
            else if ((64 > Lat) && (Lat >= 56)) LetterDesignator = 'V';
            else if ((56 > Lat) && (Lat >= 48)) LetterDesignator = 'U';
            else if ((48 > Lat) && (Lat >= 40)) LetterDesignator = 'T';
            else if ((40 > Lat) && (Lat >= 32)) LetterDesignator = 'S';
            else if ((32 > Lat) && (Lat >= 24)) LetterDesignator = 'R';
            else if ((24 > Lat) && (Lat >= 16)) LetterDesignator = 'Q';
            else if ((16 > Lat) && (Lat >= 8)) LetterDesignator = 'P';
            else if ((8 > Lat) && (Lat >= 0)) LetterDesignator = 'N';
            else if ((0 > Lat) && (Lat >= -8)) LetterDesignator = 'M';
            else if ((-8 > Lat) && (Lat >= -16)) LetterDesignator = 'L';
            else if ((-16 > Lat) && (Lat >= -24)) LetterDesignator = 'K';
            else if ((-24 > Lat) && (Lat >= -32)) LetterDesignator = 'J';
            else if ((-32 > Lat) && (Lat >= -40)) LetterDesignator = 'H';
            else if ((-40 > Lat) && (Lat >= -48)) LetterDesignator = 'G';
            else if ((-48 > Lat) && (Lat >= -56)) LetterDesignator = 'F';
            else if ((-56 > Lat) && (Lat >= -64)) LetterDesignator = 'E';
            else if ((-64 > Lat) && (Lat >= -72)) LetterDesignator = 'D';
            else if ((-72 > Lat) && (Lat >= -80)) LetterDesignator = 'C';
            else LetterDesignator = 'Z'; //Latitude is outside the UTM limits
            return LetterDesignator;
        }

        // todo this needs to be verified and ATM I think is wrong, 
        //   todo may have something to do with the zone string format it wants
        public Point ToLonLat()
        {
            Point p_out = new Point(0, 0);  // x = longitude, y = latitude

            bool isNorthHemisphere = Zone.Last() >= 'N';

            var diflat = -0.00066286966871111111111111111111111111;
            var diflon = -0.0003868060578;

            var zone = int.Parse(Zone.Remove(Zone.Length - 1));
            var c_sa = 6378137.000000;
            var c_sb = 6356752.314245;
            var e2 = Math.Pow((Math.Pow(c_sa, 2) - Math.Pow(c_sb, 2)), 0.5) / c_sb;
            var e2cuadrada = Math.Pow(e2, 2);
            var c = Math.Pow(c_sa, 2) / c_sb;
            var x = Easting - 500000;
            var y = isNorthHemisphere ? Northing : Northing - 10000000;

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

            p_out.Y = ((delt * (180.0 / Math.PI)) + s) + diflon;
            p_out.X = ((lat + (1 + e2cuadrada * Math.Pow(Math.Cos(lat), 2) - (3.0 / 2.0) * e2cuadrada * Math.Sin(lat) * Math.Cos(lat) * (tao - lat)) * (tao - lat)) * (180.0 / Math.PI)) + diflat;
            return p_out;
        }

        public override string ToString()
        {
            return $"{Easting:F3}, {Northing:F3}, {Zone}";
        }
    }

    public class UtmConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(string))
                return (value as Utm)?.ToString();
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(Utm))
            {
                try
                {
                    var tokens = ((string)value).Split(',');
                    double x = double.Parse(tokens[0]);
                    double y = double.Parse(tokens[1]);
                    string z = tokens[2];
                    return new Utm { Easting = x, Northing = y, Zone = z };
                }
                catch (Exception)
                {
                    System.Diagnostics.Trace.WriteLine($"UtmConverter::ConvertBack threw exception, value = {value}");
                }
            }
            return null;
        }
    }
}
