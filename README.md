# OceanWeather
Ocean weather service for Marorka

/* The two C# scripts are the base for Marorka's weather service.
 * OceanWeatherToGribDatabase for downloading and storing the data to the database.
 * WebWeatherService for interacting with the Grib-NetCDF database.
 * nowcastROFTS_NWWIII.py is a demo script for downloading the best estimates of current ocean weather, could be used for updating the database.
 */

For the python scripts:
    Python 3.4 was used

    Modules needed:
        netCDF4
        numpy
        matplotlib
        datetime
        pylab

For the C# scripts:
    Cygwin shell is needed (32bit was used)
        Used packages for Cygwin:
            The default for all categories
            (plus extra from Devel see: http://cs.calvin.edu/curriculum/cs/112/resources/installingEclipse/cygwin/)
            gcc-core: C compiler
            gcc-g++: C++ compiler
            gdb: The GNU Debugger
            make: The GNU version of make
            gcc-debuginfo-4.9.2-2
            
    CDO (https://code.zmaw.de/projects/cdo/wiki/Cdo)
        Building CDO with Netcdf, HDF5 and GRIB2  support.

        Download CDO from https://code.zmaw.de/projects/cdo/files
        Download NetCDF from http://www.unidata.ucar.edu/downloads/netcdf/index.jsp. Use the C version.
        Download Grib API from https://software.ecmwf.int/wiki/display/GRIB/Releases
        Download Jasper from http://www.ece.uvic.ca/~frodo/jasper/#download
        Download HDF5 and zlib from ftp://ftp.unidata.ucar.edu/pub/netcdf/netcdf-4

        Create a directory that will hold the installation libs and include files.
        For this demonstration we use /opt/cdo-install (make sure that the directory is created)

        Install zlib using
        ./configure –prefix =/opt/cdo-install
         ‘make’, ‘make check’ and ‘make install’

        Install HDF5 using
        ./configure –with-zlib=/opt/cdo-install –prefix=/opt/cdo-install CFLAGS=-fPIC
        ‘make’, ‘make check’ and ‘make install’

        Install NetCDF using
        CPPFLAGS=-I/opt/cdo-install/include LDFLAGS=-L/opt/cdo-install/lib ./configure –prefix=/opt/cdo-install CFLAGS=-fPIC
        ‘make’, ‘make check’ and ‘make install’

        Install Jasper using
        ./configure –prefix=/opt/cdo-install  CFLAGS=-fPIC
        ‘make’, ‘make check’ and ‘make install’

        Install grib using
        ./configure –prefix=/opt/cdo-install CFLAGS=-fPIC –with-jasper=/opt/cdo-install
        ‘make’, ‘make check’ and ‘make install’

        Install cdo using
        ./configure –prefix=/opt/cdo-install CFLAGS=-fPIC  –with-netcdf=/opt/cdo-install –with-jasper=/opt/cdo-install –with-hdf5=/opt/cdo-install  –with-grib_api=/opt/cdo-install
        ‘make’, ‘make check’ and ‘make install’

        This should install CDO with grib, netcdf and HDF5 support. Note that the binaries are in /opt/cdo-install/bin. Add this folder to the path to make the binaries available everywhere.

        ********* Extra ********* (Not sure if needed)
        sds: Scientific DataSet library and tools
        https://sds.codeplex.com/

        *************************

    Degrib
        available from NOAA at http://www.nws.noaa.gov/mdl/degrib/download.php

    It is necessary to add both CDO and Degrib to the environment path

