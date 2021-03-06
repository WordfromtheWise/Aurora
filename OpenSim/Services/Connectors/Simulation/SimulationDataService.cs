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
using OpenMetaverse;
using log4net;
using Nini.Config;
using System.Reflection;
using OpenSim.Services.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Data;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Services.Connectors
{
    public class SimulationDataService : ServiceBase, ISimulationDataService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        protected ISimulationDataStore m_database;

        public SimulationDataService(IConfigSource config)
            : base(config)
        {
            string dllName = String.Empty;
            string connString = String.Empty;

            // Try reading the [DatabaseService] section, if it exists
            IConfig dbConfig = config.Configs["DatabaseService"];
            if (dbConfig != null)
            {
                dllName = dbConfig.GetString("StorageProvider", String.Empty);
                connString = dbConfig.GetString("ConnectionString", String.Empty);
            }

            // Try reading the [SimulationDataStore] section
            IConfig simConfig = config.Configs["SimulationDataStore"];
            if (simConfig != null)
            {
                dllName = simConfig.GetString("StorageProvider", dllName);
                connString = simConfig.GetString("ConnectionString", connString);
            }

            // We tried, but this doesn't exist. We can't proceed
            if (dllName == String.Empty)
                dllName = "OpenSim.Data.Null.dll";

            m_database = LoadPlugin<ISimulationDataStore>(dllName, new Object[] { connString });
            if (m_database == null)
                throw new Exception("Could not find a storage interface in the given module");
        }

        #region ISimulationDataService Members

        public void StoreObject(SceneObjectGroup obj, UUID regionUUID)
        {
            m_database.StoreObject(obj, regionUUID);
        }

        public void RemoveObject(UUID uuid, UUID regionUUID)
        {
            m_database.RemoveObject(uuid, regionUUID);
        }

        public void StorePrimInventory(UUID primID, ICollection<TaskInventoryItem> items)
        {
            m_database.StorePrimInventory(primID, items);
        }

        public List<SceneObjectGroup> LoadObjects(UUID regionUUID, Scene scene)
        {
            return m_database.LoadObjects(regionUUID, scene);
        }

        public void StoreTerrain(double[,] terrain, UUID regionID, bool Revert)
        {
            m_database.StoreTerrain(terrain, regionID, Revert);
        }

        public double[,] LoadTerrain(UUID regionID, bool RevertMap)
        {
            return m_database.LoadTerrain(regionID, RevertMap);
        }

        public void StoreLandObject(LandData args)
        {
            m_database.StoreLandObject(args);
        }

        public void RemoveLandObject(UUID RegionID, UUID ParcelID)
        {
            m_database.RemoveLandObject(RegionID, ParcelID);
        }

        public List<LandData> LoadLandObjects(UUID regionUUID)
        {
            return m_database.LoadLandObjects(regionUUID);
        }

        public void StoreRegionSettings(RegionSettings rs)
        {
            m_database.StoreRegionSettings(rs);
        }

        public RegionSettings LoadRegionSettings(UUID regionUUID)
        {
            return m_database.LoadRegionSettings(regionUUID);
        }

        public void RemoveRegion(UUID regionUUID)
        {
            m_database.RemoveRegion(regionUUID);
        }

        #endregion
    }
}
