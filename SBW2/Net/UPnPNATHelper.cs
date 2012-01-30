using System;
using SBW2.Properties;

namespace SBW2.Net
{
    // This required a reference to the COM NATUPnP 1.0 Type Library to be added to the project.
    public static class UpnPnatHelper
    {
        private static NATUPNPLib.UPnPNATClass _UPnPNat = null;
        private static NATUPNPLib.UPnPNATClass UPnPNat
        {
            get { return _UPnPNat ?? (_UPnPNat = new NATUPNPLib.UPnPNATClass()); }
        }

        /// <summary>
        /// Gets a IStaticPortMappingCollection that contains all the current IStaticPortMapping's
        /// </summary>
        public static NATUPNPLib.IStaticPortMappingCollection StaticPortMappings
        {
            get
            {
                return UPnPNat.StaticPortMappingCollection;
            }
        }


        /// <summary>
        /// Adds a new Static Port Mapping
        /// </summary>
        /// <param name="externalPort">
        /// Specifies the external port for this port mapping.
        /// </param>
        /// <param name="protocol">
        /// Specifies the protocol. This parameter should be either UDP or TCP.
        /// </param>
        /// <param name="internalPort">
        /// Specifies the internal port for this port mapping.
        /// </param>
        /// <param name="internalClient">
        /// Specifies the name of the client on the private network that uses this port mapping.
        /// </param>
        /// <param name="enabled">
        /// Specifies whether the port is enabled.
        /// </param>
        /// <param name="description">
        /// Specifies a description for this port mapping.
        /// </param>
        /// <returns>The IStaticPortMapping that was added.</returns>
        public static NATUPNPLib.IStaticPortMapping Add(
            int externalPort, 
            string protocol, 
            int internalPort, 
            string internalClient, 
            bool enabled, 
            string description
            )
        {
            if (string.IsNullOrEmpty(protocol))
            {
                throw new ArgumentException(Resources.upnp_string1, "protocol");
            }
            if (!(protocol.ToLower() == "udp" || protocol.ToLower() == "tcp"))
            {
                throw new ArgumentException(Resources.upnp_string2, "protocol");
            }

            if (string.IsNullOrEmpty(internalClient))
            {
                throw new ArgumentException(Resources.upnp_string3, "internalClient");
            }

            return UPnPNat.StaticPortMappingCollection.Add(externalPort, protocol, internalPort, internalClient, enabled, description);
        }

        /// <summary>
        /// Removes an existing Static Port Mapping
        /// </summary>
        /// <param name="externalPort">
        /// Specifies the external port for this port mapping.
        /// </param>
        /// <param name="protocol">
        /// Specifies the protocol. This parameter should be either UDP or TCP.
        /// </param>
        public static void Remove(
            int externalPort,
            string protocol
            )
        {
            if (string.IsNullOrEmpty(protocol))
            {
                throw new ArgumentException(Resources.upnp_string1, "protocol");
            }
            if (!(protocol.ToLower() == "udp" || protocol.ToLower() == "tcp"))
            {
                throw new ArgumentException(Resources.upnp_string2, "protocol");
            }

            UPnPNat.StaticPortMappingCollection.Remove(externalPort, protocol);
        }

    }
}
