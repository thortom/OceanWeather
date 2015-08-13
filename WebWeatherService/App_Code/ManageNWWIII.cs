using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using OSGeo.GDAL;


public class ManageNWWIII
{
    private static Dataset ds;
    private static List<RasterBandInfo> mRasterBands = null;
    private static double originX, pixelWidth, rotationA, originY, rotationB, pixelHeight;

    // TODO: add pure infoFunction see GDALRead for reference
    public ManageNWWIII(string fileName, string dataType)
    {
        //initialize gdal and open the binary file
        System.Diagnostics.Debug.WriteLine("In NWWIII");
        Gdal.AllRegister();
        ds = Gdal.Open(fileName, Access.GA_ReadOnly);
        System.Diagnostics.Debug.WriteLine("File Open");

        //collect the transformation values
        double[] adfGeoTransform = new double[6];
        ds.GetGeoTransform(adfGeoTransform);
        originX     = adfGeoTransform[0];       /* top left x */
        pixelWidth  = adfGeoTransform[1];       /* w-e pixel resolution */
        rotationA   = adfGeoTransform[2];       /* rotation, 0 if image is "north up" */
        originY     = adfGeoTransform[3];       /* top left y */
        rotationB   = adfGeoTransform[4];       /* rotation, 0 if image is "north up" */
        pixelHeight = adfGeoTransform[5];       /* n-s pixel resolution */

        //// TODO: Fix for this bug https://trac.osgeo.org/gdal/ticket/2550
        // these values are gotten from gdal with python
        //originX = -0.25;
        //originY = 77.75;
        ////----------------------
    }

    public void close()
    {
        //to release the file
        ds.Dispose();
    }

    public struct geospatialValue
    {
        public double Longitude;
        public double Latitude;
        public DateTime timeStamp;
        public String dataType;
        public double value;
    }

    private struct RasterBandInfo
    {
        public Band band;
        public int Id;
        public string FieldType;
        public DateTime ValidUntil;
        public string Unit;
    }

    private struct Position
    {
        public double Longitude;
        public double Latitude;

        public Position(double Latitude, double Longitude)
        {
            this.Longitude = Longitude;
            this.Latitude = Latitude;
        }
    }

    public IEnumerable<geospatialValue> getNextCoordinates()
    {
        geospatialValue geoData = new geospatialValue();

        //load info about all bands in list
        mRasterBands = LoadBandInfo(ds);

        //yield all available data in the dataset one pixel at a time
        for (int i = 0; i < mRasterBands.Count; i++)
        {
            RasterBandInfo info = mRasterBands[i];

            Band band = info.band;

            double[] data = new double[band.XSize * band.YSize];

            band.ReadRaster(0, 0, band.XSize, band.YSize, data, band.XSize, band.YSize, 0, 0);

            int index = 0;
            for (int x = 0; x < band.XSize; x++)
            {
                for (int y = 0; y < band.YSize; y++)
                {
                    Position pos = getPosition(x, y);
                    geoData.Latitude = pos.Latitude;
                    geoData.Longitude = pos.Longitude;
                        
                    geoData.timeStamp = info.ValidUntil;
                    geoData.dataType = info.FieldType;
                    geoData.value = data[index];

                    yield return geoData;
                    index++;
                }
            }
        }

    }

    //TODO: make function getInfo() -> returns the metadata for each band -> string[] metadata = band.GetMetadata("");
    //TODO: take the metadata info strings as input, not hardcoded
    private static List<RasterBandInfo> LoadBandInfo(Dataset ds)
    {
        List<RasterBandInfo> rasterbands = new List<RasterBandInfo>();

        for (int iBand = 1; iBand <= ds.RasterCount; iBand++)
        {
            Band band = ds.GetRasterBand(iBand);

            RasterBandInfo info = new RasterBandInfo();
            info.Id = iBand;
            info.band = band;

            info.FieldType = band.GetMetadataItem("GRIB_ELEMENT", "");
            info.ValidUntil = GetBandTime("GRIB_VALID_TIME", band);
            info.Unit = band.GetMetadataItem("GRIB_UNIT", "");
            rasterbands.Add(info);

        }
        return rasterbands;
    }

    private static DateTime GetBandTime(string pKey, Band pBand)
    {
        DateTime returnDate = DateTime.MinValue;
        string ret = "";

        ret = pBand.GetMetadataItem(pKey, "");
            
        ret.Trim();
        string[] split = ret.Split(' ');

        string utctimestr = split.ToList().Where(a => !string.IsNullOrEmpty(a)).FirstOrDefault();

        long utctime = 0;
        if (long.TryParse(utctimestr, out utctime))
        {
            //seconds from unix start of time
            returnDate = new DateTime(1970, 1, 1).AddSeconds(utctime);
        }

        return returnDate;
    }

    private static Position getPosition(double x, double y)
    {
        // transform pixels to ground coordinates
        double dfGeoX = originX + pixelWidth * x + rotationA * y;
        double dfGeoY = originY + rotationB * x + pixelHeight * y;

        //shift to the center of the pixel
        dfGeoX += pixelWidth / 2.0;
        dfGeoY += pixelHeight / 2.0;

        return new Position(dfGeoY, dfGeoX);
    }
}

