﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Nini.Config;
using OpenSim.Framework.Console;
using OpenMetaverse;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules
{
    public class Auth: IRegionModule, IAuth
    {
        List<string> CheckServers = new List<string>();
        List<string> AuthServersBannedList = new List<string>();
        IGenericData a_DataService = null;

        /// <summary>
        /// Authenticates the user on a Login.
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool LoginAuthenticateUser(string userName, string password)
        {
            string truePassword = a_DataService.Query("UserName", userName, "users", "Password")[0];
            if (truePassword == password)
                return true;
            return false;
        }

        /// <summary>
        /// This checks whether the user's homeserver is allowed to connect to the world.
        /// </summary>
        /// <param name="serverIP"></param>
        /// <returns></returns>
        public bool CheckAuthenticationServer(IPEndPoint serverIP)
        {
            if (AuthServersBannedList.Contains(serverIP.Address.ToString()))
                return false;
            return true;
        }

        /// <summary>
        /// This is called when a user attempts to connect to the world. This may be from another sim
        /// in the world, or a sim outside the world. This checks for banning before they are allowed to connect.
        /// </summary>
        /// <param name="Identifier"></param>
        /// <returns></returns>
        public bool CheckUserAccount(string Identifier)
        {
            return true;
        }

        /// <summary>
        /// Creates a user account; this is called from IAccountBase, this creates the authentication side of it.
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        public void CreateUserAccount(string userName, string password)
        {
            string[] values = new string[2];
            values[0] = userName;
            values[1] = password;
            a_DataService.Insert("Authentication", values);
        }

        #region IModuleInterface Members

        public string Name
        {
            get { return "GenericAuthPlugin"; }
        }

        public void Close()
        {
        }

        public void PostInitialise()
        {
            a_DataService = Aurora.DataManager.DataManager.GetGenericPlugin();
        }
        Scene m_scene;
        public void Initialise(Scene scene, IConfigSource source)
        {
            m_scene = scene;
            scene.RegisterModuleInterface<IAuth>(this);
            scene.AddCommand(this, "create userauth", "create userauth", "Creates a new User Auth", CreateUserAuth);
            if (CheckServers.Count == 0)
            {
                string bannedAuthServers = source.Configs["AuroraAuth"].GetString("BannedAuthServers", "");
                if (bannedAuthServers == "")
                    return;
                string[] servers = bannedAuthServers.Split(',');
                foreach (string server in servers)
                {
                    CheckServers.Add(server);
                }
            }
        }

        protected void CreateUserAuth(string module, string[] cmdparams)
        {
            string firstName = "";
            string lastName = "";

            if (cmdparams.Length < 3)
                firstName = MainConsole.Instance.CmdPrompt("First name", "Default");
            else firstName = cmdparams[2];

            if (cmdparams.Length < 4)
                lastName = MainConsole.Instance.CmdPrompt("Last name", "User");
            else lastName = cmdparams[3];

            UserAccount account = m_scene.UserAccountService.GetUserAccount(UUID.Zero, firstName, lastName);
            CreateUserAuth(account.PrincipalID.ToString(), firstName, lastName);
        }

        public void CreateUserAuth(string UUID, string firstName, string lastName)
        {
            List<string> values = new List<string>();
            values.Add(UUID);
            values.Add(firstName + " " + lastName);
            values.Add(firstName);
            values.Add(lastName);
            values.Add(" ");
            values.Add(" ");
            values.Add("0");
            values.Add(" ");
            values.Add(" ");
            values.Add("0");
            values.Add(" ");
            values.Add(" ");
            values.Add(" ");
            values.Add(" ");
            values.Add(" ");
            values.Add("0");
            values.Add("0");
            values.Add("0");
            values.Add("0");
            values.Add(" ");
            values.Add(" ");
            values.Add(" ");
            values.Add(" ");
            values.Add("0");
            values.Add(" ");
            values.Add("0");
            values.Add(" ");
            values.Add(" ");
            values.Add("0");
            values.Add("0");
            values.Add(" ");
            values.Add(" ");
            values.Add(" ");
            values.Add(" ");
            values.Add(" ");
            IGenericData GD = Aurora.DataManager.DataManager.GetGenericPlugin();
            GD.Insert("usersauth", values.ToArray());
        }

        public bool IsSharedModule
        {
            get { return true; }
        }

        #endregion
    }
}