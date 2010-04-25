﻿/*
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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Globalization;
using OpenMetaverse;
using log4net;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Scripting;
using OpenSim.Region.ScriptEngine.Shared;
using OpenSim.Region.ScriptEngine.Shared.CodeTools;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    public class EventQueue
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // How many ms to sleep if queue is empty
        private static ThreadPriority MyThreadPriority;

        public long LastExecutionStarted;
        public bool InExecution = false;
        public bool KillCurrentScript = false;

        //This thread
        public Thread EventQueueThread;
        
        private ScriptEngine m_ScriptEngine;
        private int SleepTime = 250;

        public EventQueue(ScriptEngine engine)//EventQueueManager eqm
        {
        	m_ScriptEngine = engine;
            CultureInfo USCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = USCulture;

            //eventQueueManager = eqm;
            ReadConfig();
            Start();
        }

        ~EventQueue()
        {
            Stop();
        }

        public void ReadConfig()
        {
        	SleepTime = m_ScriptEngine.ScriptConfigSource.GetInt("SleepTimeBetweenLoops", 250);
        	
        	string pri = m_ScriptEngine.ScriptConfigSource.GetString(
        		"ScriptThreadPriority", "BelowNormal");

        	switch (pri.ToLower())
        	{
        		case "lowest":
        			MyThreadPriority = ThreadPriority.Lowest;
        			break;
        		case "belownormal":
        			MyThreadPriority = ThreadPriority.BelowNormal;
        			break;
        		case "normal":
        			MyThreadPriority = ThreadPriority.Normal;
        			break;
        		case "abovenormal":
        			MyThreadPriority = ThreadPriority.AboveNormal;
        			break;
        		case "highest":
        			MyThreadPriority = ThreadPriority.Highest;
        			break;
        		default:
        			MyThreadPriority = ThreadPriority.BelowNormal;
        			m_log.Error(
        				"[ScriptEngine.DotNetEngine]: Unknown "+
        				"priority type \"" + pri +
        				"\" in config file. Defaulting to "+
        				"\"BelowNormal\".");
        			break;
        	}
        	// Now set that priority
        	if (EventQueueThread != null)
        		if (EventQueueThread.IsAlive)
        			EventQueueThread.Priority = MyThreadPriority;
        }

        /// <summary>
        /// Start thread
        /// </summary>
        private void Start()
        {
            EventQueueThread = Watchdog.StartThread(EventQueueThreadLoop, "EventQueueManagerThread_" + m_ScriptEngine.EventQueueThreadCount, MyThreadPriority, true);
        }

        public void Stop()
        {
            Watchdog.RemoveThread();
            m_ScriptEngine.EventQueueThreadCount--;
            if (EventQueueThread != null && EventQueueThread.IsAlive == true)
            {
                try
                {
                    EventQueueThread.Abort();               // Send abort
                }
                catch (Exception)
                {
                }
            }
        }

        // Queue processing thread loop
        private void EventQueueThreadLoop()
        {
            CultureInfo USCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = USCulture;
            while (true)
            {
                if (KillCurrentScript)
                    Stop();
            	DoProcessQueue();
            }
        }


        public void DoProcessQueue()
        {
        	try
        	{
                Thread.Sleep(SleepTime);
                if (ScriptEngine.EventQueue.Count == 0)
        			return;
                List<QueueItemStruct> NeedsToBeRequeued = new List<QueueItemStruct>();
        		// Something in queue, process
                lock (ScriptEngine.EventQueue)
        		{
                    LastExecutionStarted = DateTime.Now.Ticks;
                    InExecution = true;
                    for (int qc = 0; qc < ScriptEngine.EventQueue.Count; qc++)
        			{
        				// Get queue item
                        QueueItemStruct QIS = ScriptEngine.EventQueue.Dequeue();
                        //Suspended scripts get readded
                        if (QIS.ID.Suspended)
                        {
                            NeedsToBeRequeued.Add(QIS);
                            continue;
                        }
                        //Clear scripts that shouldn't be in the queue anymore
                        if (!m_ScriptEngine.NeedsRemoved.Contains(QIS.ID.ItemID))
                        {
                            try
                            {
                                QIS.ID.SetEventParams(QIS.llDetectParams);
                                int Running = 0;
                                Running = QIS.ID.Script.ExecuteEvent(
                                    QIS.ID.State,
                                    QIS.functionName,
                                    QIS.param, QIS.CurrentlyAt);
                                //Finished with nothing left.
                                if(Running == 0)
                                	continue;
                                //Did not finish and returned where it should start now
                                if (Running != 0 && !m_ScriptEngine.NeedsRemoved.Contains(QIS.ID.ItemID))
                                {
                                    QIS.CurrentlyAt = Running;
                                    NeedsToBeRequeued.Add(QIS);
                                }
                            }
                            catch (SelfDeleteException) // Must delete SOG
                            {
                                if (QIS.ID.part != null && QIS.ID.part.ParentGroup != null)
                                    m_ScriptEngine.World.DeleteSceneObject(
                                        QIS.ID.part.ParentGroup, false);
                            }
                            catch (ScriptDeleteException) // Must delete item
                            {
                                if (QIS.ID.part != null && QIS.ID.part.ParentGroup != null)
                                    QIS.ID.part.Inventory.RemoveInventoryItem(QIS.ID.ItemID);
                            }
                            catch (Exception)
                            {
                                //m_log.Error("Event Queue error: " + ex);
                            }
                        }
        			}
        		}
                foreach (QueueItemStruct QIS in NeedsToBeRequeued)
                {
                    ScriptEngine.EventQueue.Enqueue(QIS);
                }
                InExecution = true;
                m_ScriptEngine.NeedsRemoved.Clear();
        	}
        	catch (Exception ex)
        	{
                m_log.WarnFormat("[{0}]: Handled exception stage 2 in the Event Queue: " + ex.Message + " " + ex.StackTrace, m_ScriptEngine.ScriptEngineName);
        	}
        }
    }
}