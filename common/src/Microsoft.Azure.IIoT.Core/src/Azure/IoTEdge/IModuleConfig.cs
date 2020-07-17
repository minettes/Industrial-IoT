// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Azure.IoTEdge {
    using Microsoft.Azure.IIoT.Messaging;

    /// <summary>
    /// Module configuration
    /// </summary>
    public interface IModuleConfig {

        /// <summary>
        /// IoTEdgeHub connection string
        /// </summary>
        string EdgeHubConnectionString { get; }

        /// <summary>
        /// Bypass cert validation with hub
        /// </summary>
        bool BypassCertVerification { get; }

        /// <summary>
        /// Transports to use
        /// </summary>
        TransportOption Transport { get; }
    }
}
