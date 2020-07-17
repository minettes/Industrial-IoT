// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Platform.Discovery.Edge.Module.Controllers {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hosting;
    using Microsoft.Azure.IIoT.Azure.LogAnalytics;
    using Serilog;
    using Serilog.Events;
    using System;

    /// <summary>
    /// Diagnostic settings controller
    /// </summary>
    [Version(1)]
    [Version(2)]
    public class DiagnosticSettingsController : ISettingsController, ILogAnalyticsConfig {

        /// <summary>
        /// Called based on the reported connected property.
        /// </summary>
        public bool Connected { get; set; }

        /// <summary>
        /// Set and get the log level
        /// </summary>
        public string LogLevel {
            set {
                if (value == null) {
                    // Set default
                    LogControl.Level.MinimumLevel = LogEventLevel.Information;
                    _logger.Information("Setting log level to default level.");
                }
                else {
                    // The enum values are the same as the ones defined for serilog
                    if (!Enum.TryParse<LogEventLevel>(value, true,
                        out var level)) {
                        throw new ArgumentException(
                            $"Bad log level value {value} passed.");
                    }
                    _logger.Information("Setting log level to {level}", level);
                    LogControl.Level.MinimumLevel = level;
                }
            }
            // The enum values are the same as in serilog
            get => LogControl.Level.MinimumLevel.ToString();
        }

        /// <inheritdoc/>
        public string LogWorkspaceId { get; set; }
        /// <inheritdoc/>
        public string LogWorkspaceKey { get; set; }
        /// <inheritdoc/>
        public string LogType { get; set; }

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="logger"></param>
        public DiagnosticSettingsController(
            ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private readonly ILogger _logger;
    }
}
