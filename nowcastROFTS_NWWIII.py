from mpl_toolkits.basemap import Basemap
import numpy as np
import matplotlib.pyplot as plt
from pylab import *
import netCDF4
import datetime as dt
import time

def fixForLenght(item):
    if len(item) == 2:
        pass
    else:
        item = '0' + item
    return item

def getDate():
    currentTime = dt.datetime.now()
    year = str(currentTime.year)
    month = fixForLenght(str(currentTime.month))
    day = fixForLenght(str(currentTime.day))
    return year + month + day

def plotData(data, lat, lon, title="Plot Of The World"):
    plt.figure()
    m=Basemap(projection='mill',lat_ts=10,llcrnrlon=lon.min(), urcrnrlon=lon.max(),llcrnrlat=lat.min(),urcrnrlat=lat.max(), resolution='c')
    
    x, y = m(*np.meshgrid(lon,lat))
    m.pcolormesh(x,y,data,shading='flat',cmap=plt.cm.jet)
    m.colorbar(location='right')
    m.drawcoastlines()
    m.fillcontinents()
    m.drawmapboundary()
    m.drawparallels(np.arange(-90.,120.,30.),labels=[1,0,0,0])
    m.drawmeridians(np.arange(-180.,180.,60.),labels=[0,0,0,1])

    plt.title(title)
    plt.show()

def saveFile(inputFile, fileName):
    outputFile = netCDF4.Dataset(fileName, mode="w", format="NETCDF3_CLASSIC")
     
    #Copy dimensions
    for dname, the_dim in inputFile.dimensions.items():
        print(dname, len(the_dim))
        outputFile.createDimension(dname, len(the_dim))
     
    #Copy variables
    for v_name, varin in inputFile.variables.items():
        outVar = outputFile.createVariable(v_name, varin.datatype, varin.dimensions)
        print(varin.datatype)
        outVar[:] = varin[:]

    outputFile.close()

def main():
    #mydate = '20150422'
    mydate = time.strftime("%Y%m%d")
    mydate = getDate()

    # NWWIII
    url='http://nomads.ncep.noaa.gov:9090/dods/wave/nww3/nww3'+mydate+'/nww3'+mydate+'_00z'

    # Extract the significant wave height of combined wind waves and swell
    print("Start on NWWIII")
    file = netCDF4.Dataset(url)
    # saveFile(file, "NWWIII_TEST.nc")
    print("Got data from NWWIII")
    lat  = file.variables['lat'][:]
    lon  = file.variables['lon'][:]
    data = file.variables['htsgwsfc'][1,:,:]
    file.close()

    title = mydate + ': NWW3 Significant Wave Height from NOMADS'
    plotData(data, lat, lon, title)    

    # ROFTS
    url='http://nomads.ncep.noaa.gov:9090/dods/rtofs/rtofs_global'+mydate+ '/rtofs_glo_3dz_nowcast_daily_temp'

    print("Starting on ROFTS")
    file = netCDF4.Dataset(url)
    # saveFile(file, "ROFTS_TEST.nc")
    print("Got data from ROFTS")
    lat  = file.variables['lat'][:]
    lon  = file.variables['lon'][:]
    data = file.variables['temperature'][1,1,:,:]

    file.close()

    print("Starting on plotting ROFTS")
    title = mydate + ': Global RTOFS SST from NOMADS'
    plotData(data, lat, lon, title)

if __name__ == '__main__':
    main()
