/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using OpenSim.Framework;
using OpenMetaverse;

namespace OpenSim.Services.Interfaces
{
    public interface IGridService
    {
        /// <summary>
        /// Register a region with the grid service.
        /// </summary>
        /// <param name="regionInfos"> </param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Thrown if region registration failed</exception>
        string RegisterRegion(UUID scopeID, GridRegion regionInfos, UUID oldSessionID, out UUID SessionID);

        /// <summary>
        /// Deregister a region with the grid service.
        /// </summary>
        /// <param name="regionID"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Thrown if region deregistration failed</exception>
        bool DeregisterRegion(UUID regionID, UUID SessionID);

        /// <summary>
        /// Get information about the regions neighbouring the given co-ordinates (in meters).
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        List<GridRegion> GetNeighbours(UUID scopeID, UUID regionID);

        GridRegion GetRegionByUUID(UUID scopeID, UUID regionID);

        /// <summary>
        /// Get the region at the given position (in meters)
        /// </summary>
        /// <param name="scopeID"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        GridRegion GetRegionByPosition(UUID scopeID, int x, int y);

        GridRegion GetRegionByName(UUID scopeID, string regionName);

        /// <summary>
        /// Get information about regions starting with the provided name. 
        /// </summary>
        /// <param name="name">
        /// The name to match against.
        /// </param>
        /// <param name="maxNumber">
        /// The maximum number of results to return.
        /// </param>
        /// <returns>
        /// A list of <see cref="RegionInfo"/>s of regions with matching name. If the
        /// grid-server couldn't be contacted or returned an error, return null. 
        /// </returns>
        List<GridRegion> GetRegionsByName(UUID scopeID, string name, int maxNumber);

        List<GridRegion> GetRegionRange(UUID scopeID, int xmin, int xmax, int ymin, int ymax);

        List<GridRegion> GetDefaultRegions(UUID scopeID);
        List<GridRegion> GetFallbackRegions(UUID scopeID, int x, int y);
		List<GridRegion> GetSafeRegions(UUID scopeID, int x, int y);
        List<GridRegion> GetHyperlinks(UUID scopeID);

        int GetRegionFlags(UUID scopeID, UUID regionID);

        string UpdateMap(UUID scopeID, GridRegion region, UUID sessionID);

        multipleMapItemReply GetMapItems(ulong regionHandle, GridItemType gridItemType);

        void RemoveAgent(UUID regionID, UUID agentID);

        void AddAgent(UUID regionID, UUID agentID, Vector3 Position);

        void SetRegionUnsafe(UUID ID);
    }

    public class GridRegion
    {

        /// <summary>
        /// The port by which http communication occurs with the region 
        /// </summary>
        public uint HttpPort
        {
            get { return m_httpPort; }
            set { m_httpPort = value; }
        }
        protected uint m_httpPort;

        /// <summary>
        /// A well-formed URI for the host region server (namely "http://" + ExternalHostName)
        /// </summary>
        public string ServerURI
        {
            get { return m_serverURI; }
            set { m_serverURI = value; }
        }
        protected string m_serverURI;

        public string RegionName
        {
            get { return m_regionName; }
            set { m_regionName = value; }
        }
        protected string m_regionName = String.Empty;

        public string RegionType
        {
            get { return m_regionType; }
            set { m_regionType = value; }
        }
        protected string m_regionType = String.Empty;

        protected string m_externalHostName;

        protected IPEndPoint m_internalEndPoint;

        public int RegionLocX
        {
            get { return m_regionLocX; }
            set { m_regionLocX = value; }
        }
        protected int m_regionLocX;

        public int RegionLocY
        {
            get { return m_regionLocY; }
            set { m_regionLocY = value; }
        }
        protected int m_regionLocY;

        protected UUID m_estateOwner;

        public UUID EstateOwner
        {
            get { return m_estateOwner; }
            set { m_estateOwner = value; }
        }

        public UUID RegionID = UUID.Zero;
        public UUID ScopeID = UUID.Zero;

        public UUID TerrainImage = UUID.Zero;
        public UUID TerrainMapImage = UUID.Zero;
        public byte Access;
        public int Maturity;
        public string RegionSecret = string.Empty;
        public string Token = string.Empty;

        public GridRegion()
        {
        }

        public GridRegion(int regionLocX, int regionLocY, IPEndPoint internalEndPoint, string externalUri)
        {
            m_regionLocX = regionLocX;
            m_regionLocY = regionLocY;

            m_internalEndPoint = internalEndPoint;
            m_externalHostName = externalUri;
        }

        public GridRegion(int regionLocX, int regionLocY, string externalUri, uint port)
        {
            m_regionLocX = regionLocX;
            m_regionLocY = regionLocY;

            m_externalHostName = externalUri;

            m_internalEndPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), (int)port);
        }

        public GridRegion(uint xcell, uint ycell)
        {
            m_regionLocX = (int)(xcell * Constants.RegionSize);
            m_regionLocY = (int)(ycell * Constants.RegionSize);
        }

        public GridRegion(RegionInfo ConvertFrom)
        {
            m_regionName = ConvertFrom.RegionName;
            m_regionType = ConvertFrom.RegionType;
            m_regionLocX = (int)(ConvertFrom.RegionLocX * Constants.RegionSize);
            m_regionLocY = (int)(ConvertFrom.RegionLocY * Constants.RegionSize);
            m_internalEndPoint = ConvertFrom.InternalEndPoint;
            m_externalHostName = ConvertFrom.ExternalHostName;
            m_httpPort = ConvertFrom.HttpPort;
            RegionID = ConvertFrom.RegionID;
            ServerURI = ConvertFrom.ServerURI;
            TerrainImage = ConvertFrom.RegionSettings.TerrainImageID;
            TerrainMapImage = ConvertFrom.RegionSettings.TerrainMapImageID;
            Access = ConvertFrom.AccessLevel;
            Maturity = ConvertFrom.RegionSettings.Maturity;
            RegionSecret = ConvertFrom.regionSecret;
            EstateOwner = ConvertFrom.EstateSettings.EstateOwner;
        }

        public GridRegion(GridRegion ConvertFrom)
        {
            m_regionName = ConvertFrom.RegionName;
            m_regionType = ConvertFrom.RegionType;
            m_regionLocX = ConvertFrom.RegionLocX;
            m_regionLocY = ConvertFrom.RegionLocY;
            m_internalEndPoint = ConvertFrom.InternalEndPoint;
            m_externalHostName = ConvertFrom.ExternalHostName;
            m_httpPort = ConvertFrom.HttpPort;
            RegionID = ConvertFrom.RegionID;
            ServerURI = ConvertFrom.ServerURI;
            TerrainImage = ConvertFrom.TerrainImage;
            TerrainMapImage = ConvertFrom.TerrainMapImage;
            Access = ConvertFrom.Access;
            Maturity = ConvertFrom.Maturity;
            RegionSecret = ConvertFrom.RegionSecret;
            EstateOwner = ConvertFrom.EstateOwner;
        }

        /// <value>
        /// This accessor can throw all the exceptions that Dns.GetHostAddresses can throw.
        ///
        /// XXX Isn't this really doing too much to be a simple getter, rather than an explict method?
        /// </value>
        public IPEndPoint ExternalEndPoint
        {
            get
            {
                // Old one defaults to IPv6
                //return new IPEndPoint(Dns.GetHostAddresses(m_externalHostName)[0], m_internalEndPoint.Port);

                IPAddress ia = null;
                // If it is already an IP, don't resolve it - just return directly
                if (IPAddress.TryParse(m_externalHostName, out ia))
                    return new IPEndPoint(ia, m_internalEndPoint.Port);

                try
                {
                    if (IPAddress.TryParse(m_externalHostName.Split(':')[0], out ia))
                        return new IPEndPoint(ia, m_internalEndPoint.Port);
                }
                catch { }
                // Reset for next check
                ia = null;
                try
                {
                    foreach (IPAddress Adr in Dns.GetHostAddresses(m_externalHostName))
                    {
                        if (ia == null)
                            ia = Adr;

                        if (Adr.AddressFamily == AddressFamily.InterNetwork)
                        {
                            ia = Adr;
                            break;
                        }
                    }
                }
                catch (SocketException e)
                {
                    throw new Exception(
                        "Unable to resolve local hostname " + m_externalHostName + " innerException of type '" +
                        e + "' attached to this exception", e);
                }

                return new IPEndPoint(ia, m_internalEndPoint.Port);
            }
        }

        public string ExternalHostName
        {
            get { return m_externalHostName; }
            set { m_externalHostName = value; }
        }

        public IPEndPoint InternalEndPoint
        {
            get { return m_internalEndPoint; }
            set { m_internalEndPoint = value; }
        }

        public ulong RegionHandle
        {
            get { return Util.UIntsToLong((uint)RegionLocX, (uint)RegionLocY); }
        }

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> kvp = new Dictionary<string, object>();
            kvp["uuid"] = RegionID.ToString();
            kvp["locX"] = RegionLocX.ToString();
            kvp["locY"] = RegionLocY.ToString();
            kvp["regionName"] = RegionName;
            kvp["regionType"] = RegionType;
            kvp["serverIP"] = ExternalHostName; //ExternalEndPoint.Address.ToString();
            kvp["serverHttpPort"] = HttpPort.ToString();
            kvp["serverURI"] = ServerURI;
            kvp["serverPort"] = InternalEndPoint.Port.ToString();
            kvp["regionMapTexture"] = TerrainImage.ToString();
            kvp["regionTerrainTexture"] = TerrainMapImage.ToString();
            kvp["access"] = Access.ToString();
            kvp["regionSecret"] = RegionSecret;
            kvp["owner_uuid"] = EstateOwner.ToString();
            kvp["Maturity"] = Maturity.ToString();
            kvp["Token"] = Token.ToString();
            // Maturity doesn't seem to exist in the DB
            return kvp;
        }

        public GridRegion(Dictionary<string, object> kvp)
        {
            if (kvp.ContainsKey("uuid"))
                RegionID = new UUID((string)kvp["uuid"]);

            if (kvp.ContainsKey("locX"))
                RegionLocX = Convert.ToInt32((string)kvp["locX"]);

            if (kvp.ContainsKey("locY"))
                RegionLocY = Convert.ToInt32((string)kvp["locY"]);

            if (kvp.ContainsKey("regionName"))
                RegionName = (string)kvp["regionName"];

            if (kvp.ContainsKey("regionType"))
                RegionType = (string)kvp["regionType"];

            if (kvp.ContainsKey("serverIP"))
            {
                //int port = 0;
                //Int32.TryParse((string)kvp["serverPort"], out port);
                //IPEndPoint ep = new IPEndPoint(IPAddress.Parse((string)kvp["serverIP"]), port);
                ExternalHostName = (string)kvp["serverIP"];
            }
            else
                ExternalHostName = "127.0.0.1";

            if (kvp.ContainsKey("serverPort"))
            {
                Int32 port = 0;
                Int32.TryParse((string)kvp["serverPort"], out port);
                InternalEndPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), port);
            }

            if (kvp.ContainsKey("serverHttpPort"))
            {
                UInt32 port = 0;
                UInt32.TryParse((string)kvp["serverHttpPort"], out port);
                HttpPort = port;
            }

            if (kvp.ContainsKey("serverURI"))
                ServerURI = (string)kvp["serverURI"];

            if (kvp.ContainsKey("regionMapTexture"))
                UUID.TryParse((string)kvp["regionMapTexture"], out TerrainImage);

            if (kvp.ContainsKey("regionTerrainTexture"))
                UUID.TryParse((string)kvp["regionTerrainTexture"], out TerrainMapImage);

            if (kvp.ContainsKey("access"))
                Access = Byte.Parse((string)kvp["access"]);

            if (kvp.ContainsKey("Maturity"))
                Maturity = int.Parse((string)kvp["Maturity"]);

            if (kvp.ContainsKey("regionSecret"))
                RegionSecret =(string)kvp["regionSecret"];

            if (kvp.ContainsKey("owner_uuid"))
                EstateOwner = new UUID(kvp["owner_uuid"].ToString());

            if (kvp.ContainsKey("Token"))
                Token = kvp["Token"].ToString();

        }
    }

}
