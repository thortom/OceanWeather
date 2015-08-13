using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.GDAL;

class ManageHYCOM
{
    private static Dataset data, lon, lat;

    // TODO: add pure infoFunction see GDALRead for reference
    public ManageHYCOM(string fileName, string dataType)
    {
        //Get this from the subdatasets in the file
        string subdataset = "NETCDF:\"" + fileName + "\":" + dataType;
        string subLatitude = "NETCDF:\"" + fileName + "\":Latitude";
        string subLongitude = "NETCDF:\"" + fileName + "\":Longitude";

        //initialize gdal and open the binary files
        Gdal.AllRegister();

        data = Gdal.Open(subdataset, Access.GA_ReadOnly);
        lat = Gdal.Open(subLatitude, Access.GA_ReadOnly);
        lon = Gdal.Open(subLongitude, Access.GA_ReadOnly);
    }

    public void close()
    {
        //to release the files
        data.Dispose();
        lat.Dispose();
        lon.Dispose();
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
        public Band dataBand;
        public int Id;
        public string FieldType;
        public DateTime ValidUntil;
        public string Unit;
        public Band latBand;
        public Band lonBand;
    }

    public IEnumerable<geospatialValue> getNextCoordinates()
    {
        geospatialValue geoData = new geospatialValue();

        //load info about all bands in list
        RasterBandInfo mRasterBand = LoadBandInfo(data, lat, lon);

        Band band = mRasterBand.dataBand;

        double[] dataArray = new double[band.XSize * band.YSize];
        double[] lonArray = new double[band.XSize * band.YSize];
        double[] latArray = new double[band.XSize * band.YSize];

        mRasterBand.dataBand.ReadRaster(0, 0, band.XSize, band.YSize, dataArray, band.XSize, band.YSize, 0, 0);
        mRasterBand.lonBand.ReadRaster(0, 0, band.XSize, band.YSize, lonArray, band.XSize, band.YSize, 0, 0);
        mRasterBand.latBand.ReadRaster(0, 0, band.XSize, band.YSize, latArray, band.XSize, band.YSize, 0, 0);

        //yield all available data in the dataset one pixel at a time
        int index = 0;
        for (int x = 0; x < band.XSize; x++)
        {
            for (int y = 0; y < band.YSize; y++)
            {
                geoData.timeStamp = mRasterBand.ValidUntil;
                geoData.dataType = mRasterBand.FieldType;
                geoData.Latitude = latArray[index];
                geoData.Longitude = lonArray[index];
                geoData.value = dataArray[index];

                yield return geoData;
                index++;
            }
        }
    }

    //TODO: make function getInfo() -> returns the metadata for each band -> string[] metadata = band.GetMetadata("");
    //TODO: take the metadata info strings as input, not hardcoded
    private static RasterBandInfo LoadBandInfo(Dataset data, Dataset lat, Dataset lon)
    {
        RasterBandInfo rasterband = new RasterBandInfo();

        // Only interseted in the data on the surface depth=0m
        int iBand = 1;

        Band dataBand = data.GetRasterBand(iBand);
        Band latBand = lat.GetRasterBand(iBand);
        Band lonBand = lon.GetRasterBand(iBand);

        rasterband.Id = iBand;
        rasterband.dataBand = dataBand;

        rasterband.FieldType = dataBand.GetMetadataItem("NETCDF_VARNAME", "");
        rasterband.ValidUntil = GetBandTime("NETCDF_DIMENSION_MT", dataBand);
        rasterband.Unit = dataBand.GetMetadataItem("NETCDF_DIMENSION_MT", "");

        rasterband.latBand = latBand;
        rasterband.lonBand = lonBand;

        return rasterband;
    }

    private static DateTime GetBandTime(string pKey, Band pBand)
    {
        DateTime returnDate = DateTime.MinValue;
        string ret = "";

        ret = pBand.GetMetadataItem(pKey, "");

        long numbDays = 0;
        if (long.TryParse(ret, out numbDays))
        {
            //days since 1900-12-31 00:00:00
            returnDate = new DateTime(1900, 12, 31).AddDays(numbDays);
        }

        return returnDate;
    }
}
