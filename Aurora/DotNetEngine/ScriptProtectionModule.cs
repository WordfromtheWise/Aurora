﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Net.Mail;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Client;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.ScriptEngine.Interfaces;

namespace OpenSim.Region.ScriptEngine.DotNetEngine
{
    public class ScriptProtectionModule: IScriptProtectionModule
	{
		#region Declares
		
		private enum Trust : int
    	{
        	Full = 5,
        	Medium = 3,
        	Low = 1
    	}

		IConfigSource m_source;
        ScriptEngine m_engine;
        Trust TrustLevel = Trust.Full;
        bool allowMacroScripting = true;

        Dictionary<UUID, List<string>> WantedClassesByItemID = new Dictionary<UUID, List<string>>();
        //First String: ClassName, Second String: Class Source
        Dictionary<string, string> ClassScripts = new Dictionary<string, string>();

        //String: ClassName, InstanceData: data of the script.
        Dictionary<string, InstanceData> ClassInstances = new Dictionary<string, InstanceData>();
        
        //Threat Level for scripts.
        ThreatLevel m_MaxThreatLevel = 0;
        bool m_OSFunctionsEnabled = false;
	    internal Dictionary<string, List<UUID> > m_FunctionPerms = new Dictionary<string, List<UUID> >();

        #endregion
        
        #region Constructor
        
        public ScriptProtectionModule(IConfigSource source, ScriptEngine engine)
		{
			m_source = source;
            m_engine = engine;
            if (m_engine.Config.GetBoolean("AllowOSFunctions", false))
                m_OSFunctionsEnabled = true;

		}
        
		#endregion
		
		#region ThreatLevels
		
		public ThreatLevel GetThreatLevel()
		{
			if(m_MaxThreatLevel != 0)
				return m_MaxThreatLevel;
			string risk = m_engine.Config.GetString("FunctionThreatLevel", "VeryLow");
			switch (risk)
			{
				case "None":
					m_MaxThreatLevel = ThreatLevel.None;
					break;
				case "VeryLow":
					m_MaxThreatLevel = ThreatLevel.VeryLow;
					break;
				case "Low":
					m_MaxThreatLevel = ThreatLevel.Low;
					break;
				case "Moderate":
					m_MaxThreatLevel = ThreatLevel.Moderate;
					break;
				case "High":
					m_MaxThreatLevel = ThreatLevel.High;
					break;
				case "VeryHigh":
					m_MaxThreatLevel = ThreatLevel.VeryHigh;
					break;
				case "Severe":
					m_MaxThreatLevel = ThreatLevel.Severe;
					break;
				default:
					break;
			}
		}
		
		public void CheckThreatLevel(ThreatLevel level, string function)
        {
            if (!m_OSFunctionsEnabled)
                Error("Runtime Error: ", String.Format("{0} permission denied.  All OS functions are disabled.", function)); // throws

            if (!m_FunctionPerms.ContainsKey(function))
            {
                string perm = m_engine.Config.GetString("Allow_" + function, "");
                if (perm == "")
                {
                    m_FunctionPerms[function] = null; // a null value is default
                }
                else
                {
                    bool allowed;

                    if (bool.TryParse(perm, out allowed))
                    {
                        // Boolean given
                        if (allowed)
                        {
                            m_FunctionPerms[function] = new List<UUID>();
                            m_FunctionPerms[function].Add(UUID.Zero);
                        }
                        else
                            m_FunctionPerms[function] = new List<UUID>(); // Empty list = none
                    }
                    else
                    {
                        m_FunctionPerms[function] = new List<UUID>();

                        string[] ids = perm.Split(new char[] {','});
                        foreach (string id in ids)
                        {
                            string current = id.Trim();
                            UUID uuid;

                            if (UUID.TryParse(current, out uuid))
                            {
                                if (uuid != UUID.Zero)
                                    m_FunctionPerms[function].Add(uuid);
                            }
                        }
                    }
                }
            }

            // If the list is null, then the value was true / undefined
            // Threat level governs permissions in this case
            //
            // If the list is non-null, then it is a list of UUIDs allowed
            // to use that particular function. False causes an empty
            // list and therefore means "no one"
            //
            // To allow use by anyone, the list contains UUID.Zero
            //
            if (m_FunctionPerms[function] == null) // No list = true
            {
                if (level > m_MaxThreatLevel)
                    Error("Runtime Error: ",
                        String.Format(
                            "{0} permission denied.  Allowed threat level is {1} but function threat level is {2}.",
                            function, m_MaxThreatLevel, level));
            }
            else
            {
                if (!m_FunctionPerms[function].Contains(UUID.Zero))
                {
                	//REFACTOR ISSUE!!!
                    //if (!m_FunctionPerms[function].Contains(m_host.OwnerID))
                        Error("Runtime Error: ",
                            String.Format("{0} permission denied.  Prim owner is not in the list of users allowed to execute this function.",
                            function));
                }
            }
        }

		internal void Error(string surMessage, string msg)
        {
            throw new Exception(surMessage + msg);
        }

        
		
		#endregion
        
        #region MacroScripting Protection
        
        public bool AllowMacroScripting
        {
            get
            {
                return allowMacroScripting;
            }
        }
        
        public void AddNewClassSource(string ClassName, string SRC, object ID)
        {
            if (!ClassScripts.ContainsKey(ClassName))
            {
                ClassScripts.Add(ClassName, SRC);
                if(ID != null)
                    ClassInstances.Add(ClassName, (InstanceData)ID);
            }
        }

        public string GetSRC(OpenMetaverse.UUID itemID, uint localID, UUID OwnerID)
        {
            string ReturnValue = "";
            List<string> SRCWanted = new List<string>();
            if (WantedClassesByItemID.ContainsKey(itemID))
            {
                WantedClassesByItemID.TryGetValue(itemID, out SRCWanted);
                foreach (string ClassName in SRCWanted)
                {
                	if (!ClassInstances.ContainsKey(ClassName))
                	{
                		//Its a web URL
                		if (TrustLevel == Trust.Low)
                			continue;
                		else
                			ReturnValue += ClassScripts[ClassName];
                	}
                	else
                	{
                    	InstanceData id = ClassInstances[ClassName];
                    	
                    	bool isInSameObject = (id.localID == localID);
                    	bool isSameOwner = (id.InventoryItem.OwnerID == OwnerID);
                    	if (isInSameObject)
                    	{
                    		//Only check for owner
                    		if (isSameOwner)
                    		{
                    			//No checks required
                    			ReturnValue += ClassScripts[ClassName];
                    		}
                    		else
                    		{
                    			if (TrustLevel == Trust.Low)
                    				continue;
                    			else
                    				ReturnValue += ClassScripts[ClassName];
                    		}
                    	}
                    	else
                    	{
                    		if (isSameOwner)
                    		{
                    			if (TrustLevel == Trust.Low)
                    				continue;
                    			else
                    				ReturnValue += ClassScripts[ClassName];
                    		}
                    		else
                    		{
                    			if (TrustLevel < Trust.Full)
                    				continue;
                    			else
                    				ReturnValue += ClassScripts[ClassName];
                    		}
                    	}
                    }
                }
            }
            return ReturnValue;
        }

        public void AddWantedSRC(UUID itemID, string ClassName)
        {
            List<string> SRCWanted = new List<string>();
            if(WantedClassesByItemID.ContainsKey(itemID))
            {
                WantedClassesByItemID.TryGetValue(itemID, out SRCWanted);
                WantedClassesByItemID.Remove(itemID);
            }
            SRCWanted.Add(ClassName);
            WantedClassesByItemID.Add(itemID, SRCWanted);
        }
        
        #endregion
    }
}