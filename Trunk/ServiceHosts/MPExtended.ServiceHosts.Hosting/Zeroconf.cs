﻿#region Copyright (C) 2011 MPExtended
// Copyright (C) 2011 MPExtended Developers, http://mpextended.codeplex.com/
// 
// MPExtended is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MPExtended is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MPExtended. If not, see <http://www.gnu.org/licenses/>.
#endregion

// Inspired by the zeroconf code in the WifiRemote plugin, thanks!

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ZeroconfService;
using System.Net.NetworkInformation;
using MPExtended.Libraries.General;

namespace MPExtended.ServiceHosts.Hosting
{
    internal class Zeroconf
    {
        private const string DOMAIN = ""; // whole network

        private List<NetService> publishServices = new List<NetService>();
        private string serviceName = "";

        public void PublishServices(List<Service> services)
        {
            if (!CheckBonjourEnabled() || !CheckBonjourInstallation())
            {
                return;
            }

            serviceName = GetServiceName();
            foreach (Service srv in services)
            {
                // also publish some additional data
                Dictionary<string, string> additionalData = new Dictionary<string, string>();
                additionalData["hwAddr"] = GetHardwareAddresses();

                NetService net = new NetService(DOMAIN, srv.ServiceType, serviceName, srv.Port);
                net.TXTRecordData = NetService.DataFromTXTRecordDictionary(additionalData);
                net.DidNotPublishService += new NetService.ServiceNotPublished(FailedToPublishService);
                net.DidPublishService += new NetService.ServicePublished(PublishedService);
                net.Publish();
            }
            Log.Info("Published bonjour services");
        }

        private bool CheckBonjourInstallation()
        {
            try
            {
                Version ver = NetService.DaemonVersion;
                Log.Trace("Bonjour version {0} installed", ver.ToString());
                return true;
            }
            catch (Exception)
            {
                Log.Trace("Bonjour not installed");
                return false;
            }
        }

        private string GetServiceName()
        {
            string value = XElement.Load(Configuration.GetPath("Services.xml")).Element("bonjour").Element("pcname").Value;
            if (!String.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            try
            {
                return System.Environment.MachineName;
            }
            catch (Exception)
            {
                return "MPExtended Services";
            }
        }

        private string GetHardwareAddresses()
        {
            try
            {
                var addresses =
                    NetworkInterface.GetAllNetworkInterfaces()
                        .Where(x => x.OperationalStatus == OperationalStatus.Up)
                        .Where(x => x.GetPhysicalAddress() != null)
                        .Select(x => x.GetPhysicalAddress().ToString())
                        .Where(x => x.Length == 12);

                return String.Join(";", addresses);
            }
            catch (Exception e)
            {
                Log.Warn("Could not get hardware address", e);
                return "";
            }
        }

        private void FailedToPublishService(NetService service, DNSServiceException error)
        {
            string msg = String.Format("Failed to publish service {0}", service.Name);
            Log.Warn(msg, error);
        }

        private void PublishedService(NetService service)
        {
            Log.Debug("Published service {0}", service.Name);
        }

        private bool CheckBonjourEnabled()
        {
            XElement file = XElement.Load(Configuration.GetPath("Services.xml"));
            return file.Element("bonjour").Element("enabled").Value == "true";
        }
    }
}