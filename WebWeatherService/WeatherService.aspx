<%@ Page Language="C#" AutoEventWireup="true" CodeFile="WeatherService.aspx.cs" Inherits="WeatherService" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Weather Service</title>
    <link rel="stylesheet" type="text/css" href="Style/StyleSheet.css" />
</head>
<body>
    <form id="form1" runat="server">
        <div id="columnLeft">
            <asp:Image ID="logoMarorka" runat="server" src="Images/Marorka_logo.gif"/>

            <div id="variablesList">
            <!-- TODO: use checkboxlist for this http://www.w3schools.com/aspnet/control_checkboxlist.asp -->
            <h3>Variables:</h3>
                <asp:CheckBox ID="varCheck_dp" runat="server" Text="dp      = Wave Directon" /><br />
                <asp:CheckBox ID="varCheck_hs" runat="server" Text="hs      = Signigicant Height" /><br />
                <asp:CheckBox ID="varCheck_tp" runat="server" Text="tp      = Mean Period" /><br />
                <asp:CheckBox ID="varCheck_wind" runat="server" Text="wind      = U and V WindComponent" /><br /><br />

                <asp:CheckBox ID="varCheck_sal" runat="server" Text="salinity      = Salinity" /><br />
                <asp:CheckBox ID="varCheck_temp" runat="server" Text="temperature      = Temperature" /><br />
                <asp:CheckBox ID="varCheck_currentU" runat="server" Text="u      = U_CurrentComponent" /><br />
                <asp:CheckBox ID="varCheck_currentV" runat="server" Text="v      = V_CurrentComponent" /><br />
            </div>
        </div>
        <div id="columnRight">
            <asp:Image ID="worldMap" runat="server" src="Images/WorldMap.jpg" Width="200px"/><br />
            <h3>Lat/Lon subset coordinates subset<br />Bounding Box (decimal degrees):</h3>

            <asp:Label runat="server" Text="North"></asp:Label><br />
            <asp:TextBox ID="textBox_north" runat="server" Text="90"></asp:TextBox><br />

            <asp:Label runat="server" Text="South"></asp:Label><br />
            <asp:TextBox ID="textBox_south" runat="server" Text="-90"></asp:TextBox><br /><br />

            <asp:Label runat="server" Text="West"></asp:Label><br />
            <asp:TextBox ID="textBox_west" runat="server" Text="-180"></asp:TextBox><br />
            
            <asp:Label runat="server" Text="East"></asp:Label><br />
            <asp:TextBox ID="textBox_east" runat="server" Text="180"></asp:TextBox><br /><br />

            <h3>Time Range:</h3>
            <asp:Label runat="server" Text="Start date"></asp:Label><br />
            <asp:TextBox ID="textBox_timeStart" runat="server" Text="2011-01-04-12:04:00"></asp:TextBox><br />

            <asp:Label runat="server" Text="End date"></asp:Label><br />
            <asp:TextBox ID="textBox_timeEnd" runat="server" Text="2011-01-06-15:07:00"></asp:TextBox><br />

            <h3>Format</h3>
            <asp:DropDownList ID="dropDown_fileType" runat="server">
                <asp:ListItem>csv</asp:ListItem>
            </asp:DropDownList>
        </div><br />

        <asp:Button ID="buttonSubmit" runat="server" Text="Submit" OnClick="buttonSubmit_Click" />
        <asp:Button ID="buttonReset" runat="server" Text="Reset" OnClick="buttonReset_Click" />
        <p>Marorka Weather Database</p>
    </form>
</body>
</html>
