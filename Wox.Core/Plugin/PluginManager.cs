﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Wox.Infrastructure.Http;
using Wox.Plugin;

namespace Wox.Core.Plugin
{
    /// <summary>
    /// The entry for managing Wox plugins
    /// </summary>
    public static class PluginManager
    {
        public static String DebuggerMode { get; private set; }
        private static List<PluginPair> plugins = new List<PluginPair>();
        
        /// <summary>
        /// Directories that will hold Wox plugin directory
        /// </summary>
        private static List<string> pluginDirectories = new List<string>();

        static PluginManager()
        {
            pluginDirectories.Add(
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Plugins"));
            pluginDirectories.Add(
                Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".Wox"),"Plugins"));

            MakesurePluginDirectoriesExist();   
        }

        private static void MakesurePluginDirectoriesExist()
        {
            foreach (string pluginDirectory in pluginDirectories)
            {
                if (!Directory.Exists(pluginDirectory))
                {
                    Directory.CreateDirectory(pluginDirectory);
                }
            }
        }

        /// <summary>
        /// Load and init all Wox plugins
        /// </summary>
        public static void Init()
        {
            plugins.Clear();

            List<PluginMetadata> pluginMetadatas = PluginConfig.Parse(pluginDirectories);
            plugins.AddRange(new CSharpPluginLoader().LoadPlugin(pluginMetadatas));
            plugins.AddRange(new JsonRPCPluginLoader<PythonPlugin>().LoadPlugin(pluginMetadatas));

            foreach (PluginPair pluginPair in plugins)
            {
                PluginPair pair = pluginPair;
                ThreadPool.QueueUserWorkItem(o => pair.Plugin.Init(new PluginInitContext()
                {
                    CurrentPluginMetadata = pair.Metadata,
                    Proxy = HttpProxy.Instance,
                    API = App.Window
                }));
            }
        }

        public static List<PluginPair> AllPlugins
        {
            get
            {
                return plugins;
            }
        }

        public static bool HitThirdpartyKeyword(Query query)
        {
            if (string.IsNullOrEmpty(query.ActionName)) return false;

            return plugins.Any(o => o.Metadata.PluginType == PluginType.ThirdParty && o.Metadata.ActionKeyword == query.ActionName);
        }

        public static void ActivatePluginDebugger(string path)
        {
            DebuggerMode = path;
        }

        /// <summary>
        /// get specified plugin, return null if not found
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static PluginPair GetPlugin(string id)
        {
            return AllPlugins.FirstOrDefault(o => o.Metadata.ID == id);
        }
    }
}