// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Platform.Registry.Models {

    /// <summary>
    /// Entity activation state
    /// </summary>
    public class EntityActivationStatusModel {

        /// <summary>
        /// Identifier of the entity
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Activation state
        /// </summary>
        public EntityActivationState? ActivationState { get; set; }
    }
}
