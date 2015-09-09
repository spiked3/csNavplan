using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csNavplan
{
    // todo: verify this with http://home.hiwaay.net/~taylorc/toolbox/geography/geoutm.html

    public class Utm
    {
        const double deg2rad = Math.PI / 180.0;
        public string Zone { get; set; }
        public double Easting { get; set; }
        public double Northing { get; set; }

        public Utm(double longitude, double latitude)
        {
            const double a = 6378137; //WGS84
            const double eccSquared = 0.00669438; //WGS84
            const double k0 = 0.9996;

            //Make sure the longitude is between -180.00 .. 179.9
            double LongTemp = (longitude + 180) - ((int)((longitude + 180) / 360)) * 360 - 180; // -180.00 .. 179.9;

            double LatRad = latitude * deg2rad;
            double LongRad = LongTemp * deg2rad;

            int ZoneNumber = ((int)((LongTemp + 180) / 6)) + 1;

            if (latitude >= 56.0 && latitude < 64.0 && LongTemp >= 3.0 && LongTemp < 12.0)
                ZoneNumber = 32;

            // Special zones for Svalbard
            if (latitude >= 72.0 && latitude < 84.0)
            {
                if (LongTemp >= 0.0 && LongTemp < 9.0) ZoneNumber = 31;
                else if (LongTemp >= 9.0 && LongTemp < 21.0) ZoneNumber = 33;
                else if (LongTemp >= 21.0 && LongTemp < 33.0) ZoneNumber = 35;
                else if (LongTemp >= 33.0 && LongTemp < 42.0) ZoneNumber = 37;
            }
            double LongOrigin = (ZoneNumber - 1) * 6 - 180 + 3;
            double LongOriginRad = LongOrigin * deg2rad;

            //compute the UTM Zone from the latitude and longitude
            Zone = ZoneNumber.ToString() + UTMLetterDesignator(latitude);

            const double eccPrimeSquared = (eccSquared) / (1 - eccSquared);

            double N = a / Math.Sqrt(1 - eccSquared * Math.Sin(LatRad) * Math.Sin(LatRad));
            double T = Math.Tan(LatRad) * Math.Tan(LatRad);
            double C = eccPrimeSquared * Math.Cos(LatRad) * Math.Cos(LatRad);
            double A = Math.Cos(LatRad) * (LongRad - LongOriginRad);

            double M = a * ((1 - eccSquared / 4 - 3 * eccSquared * eccSquared / 64 - 5 * eccSquared * eccSquared * eccSquared / 256) * LatRad
                            - (3 * eccSquared / 8 + 3 * eccSquared * eccSquared / 32 + 45 * eccSquared * eccSquared * eccSquared / 1024) * Math.Sin(2 * LatRad)
                            + (15 * eccSquared * eccSquared / 256 + 45 * eccSquared * eccSquared * eccSquared / 1024) * Math.Sin(4 * LatRad)
                            - (35 * eccSquared * eccSquared * eccSquared / 3072) * Math.Sin(6 * LatRad));

            Easting = k0 * N * (A + (1 - T + C) * A * A * A / 6 + (5 - 18 * T + T * T + 72 * C - 58 * eccPrimeSquared) 
                * A * A * A * A * A / 120) + 500000.0;

            Northing = k0 * (M + N * Math.Tan(LatRad) * (A * A / 2 + (5 - T + 9 * C + 4 * C * C) * A * A * A * A / 24
                + (61 - 58 * T + T * T + 600 * C - 330 * eccPrimeSquared) * A * A * A * A * A * A / 720));
            if (latitude < 0)
                Northing += 10000000.0; //10000000 meter offset for southern hemisphere
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
    }
}
