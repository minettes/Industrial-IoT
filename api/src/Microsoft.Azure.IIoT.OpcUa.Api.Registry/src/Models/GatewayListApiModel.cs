// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Platform.Registry.Api.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Gateway registration list
    /// </summary>
    [DataContract]
    public class GatewayListApiModel {

        /// <summary>
        /// Registrations
        /// </summary>
        [DataMember(Name = "items", Order = 0)]
        public List<GatewayApiModel> Items { get; set; }

        /// <summary>
        /// Continuation or null if final
        /// </summary>
        [DataMember(Name = "continuationToken", Order = 1,
            EmitDefaultValue = false)]
        public string ContinuationToken { get; set; }
    }
}
