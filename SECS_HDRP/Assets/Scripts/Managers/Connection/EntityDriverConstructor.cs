using System;

            if (m_Configuration.Type == NetworkType.Relay)
            {
                networkSettings.WithRelayParameters(ref m_Configuration.RelayClientData);
                DefaultDriverBuilder.RegisterClientDriver(world, ref driverStore, netDebug, networkSettings);
            } 