// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.All {
    using Microsoft.Azure.IIoT.Services.All.Runtime;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Hosting;
    using Autofac.Extensions.DependencyInjection;
    using Autofac;
    using System;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Mono app startup
    /// </summary>
    public class Startup {

        /// <summary>
        /// Configuration - Initialized in constructor
        /// </summary>
        public Config Config { get; }

        /// <summary>
        /// Hosting environment
        /// </summary>
        public IWebHostEnvironment Environment { get; }

        /// <summary>
        /// Create startup
        /// </summary>
        /// <param name="env"></param>
        /// <param name="configuration"></param>
        public Startup(IWebHostEnvironment env, IConfiguration configuration) :
            this(env, new Config(new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                .AddFromDotEnvFile()
                .AddFromKeyVault()
                .Build())) {
        }

        /// <summary>
        /// Create startup
        /// </summary>
        /// <param name="env"></param>
        /// <param name="configuration"></param>
        public Startup(IWebHostEnvironment env, Config configuration) {
            Environment = env;
            Config = configuration;
        }

        /// <summary>
        /// Configure services
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public void ConfigureServices(IServiceCollection services) {
            services.AddHeaderForwarding();
            services.AddHttpContextAccessor();
            services.AddHealthChecks();
            services.AddDistributedMemoryCache();
            services.AddApiVersioning();
        }

        /// <summary>
        /// Configure the application
        /// </summary>
        /// <param name="app"></param>
        /// <param name="appLifetime"></param>
        public void Configure(IApplicationBuilder app, IHostApplicationLifetime appLifetime) {
            var applicationContainer = app.ApplicationServices.GetAutofacRoot();

            app.UsePathBase();
            app.UseHeaderForwarding();
            app.UseHttpsRedirect();

            // Configure branches for business
            app.UseWelcomePage("/");

            // Minimal API surface
            app.AddStartupBranch<OpcUa.Registry.Startup>("/registry");
            app.AddStartupBranch<OpcUa.Vault.Startup>("/vault");
            app.AddStartupBranch<OpcUa.Twin.Startup>("/twin");
            app.AddStartupBranch<OpcUa.Publisher.Startup>("/publisher");
            app.AddStartupBranch<OpcUa.Events.Startup>("/events");
            app.AddStartupBranch<Common.Auth.Startup>("/auth");
            app.AddStartupBranch<Common.Users.Startup>("/users");
            app.AddStartupBranch<Common.Jobs.Startup>("/jobs");
            app.AddStartupBranch<Common.Jobs.Edge.Startup>("/edge/jobs");

            if (!Config.IsMinimumDeployment) {
                app.AddStartupBranch<OpcUa.Twin.Gateway.Startup>("/ua");
                app.AddStartupBranch<OpcUa.Twin.History.Startup>("/history");
            }

            app.UseHealthChecks("/healthz");

            // Start processors
            applicationContainer.Resolve<IHostProcess>().StartAsync().Wait();

            // If you want to dispose of resources that have been resolved in the
            // application container, register for the "ApplicationStopped" event.
            appLifetime.ApplicationStopped.Register(applicationContainer.Dispose);
        }

        /// <summary>
        /// Configure Autofac container
        /// </summary>
        /// <param name="builder"></param>
        public void ConfigureContainer(ContainerBuilder builder) {

            // Register service info and configuration interfaces
            builder.RegisterInstance(Config)
                .AsImplementedInterfaces().AsSelf();
            builder.RegisterInstance(Config.Configuration)
                .AsImplementedInterfaces();

            // Add diagnostics and auth providers
            builder.AddDiagnostics(Config);
            builder.RegisterModule<DefaultServiceAuthProviders>();

            builder.RegisterType<ProcessorHost>()
                .AsImplementedInterfaces().SingleInstance();
        }

        /// <summary>
        /// Injected processor host
        /// </summary>
        private sealed class ProcessorHost : IHostProcess, IDisposable, IHealthCheck {

            /// <inheritdoc/>
            public ProcessorHost(Config config) {
                _config = config;
            }

            /// <inheritdoc/>
            public void Start() {
                _cts = new CancellationTokenSource();

                var args = new string[0];

                // Minimal processes
                var processes = new List<Task> {
                    Task.Run(() => OpcUa.Registry.Sync.Program.Main(args), _cts.Token),
                    Task.Run(() => Processor.Onboarding.Program.Main(args), _cts.Token),
                    Task.Run(() => Processor.Tunnel.Program.Main(args), _cts.Token)
                };

                if (!_config.IsMinimumDeployment) {
                    processes.Add(Task.Run(() => Processor.Events.Program.Main(args),
                        _cts.Token));
                    processes.Add(Task.Run(() => Processor.Telemetry.Program.Main(args),
                        _cts.Token));
                    processes.Add(Task.Run(() => Processor.Telemetry.Cdm.Program.Main(args),
                        _cts.Token));
                }
                _runner = Task.WhenAll(processes.ToArray());
            }

            /// <inheritdoc/>
            public Task StartAsync() {
                // Delay start by 5 seconds to let api boot up
                return Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(_ => Start());
            }

            /// <inheritdoc/>
            public async Task StopAsync() {
                _cts.Cancel();
                try {
                    await _runner;
                }
                catch (AggregateException aex) {
                    if (aex.InnerExceptions.All(e => e is OperationCanceledException)) {
                        return;
                    }
                    throw aex;
                }
            }

            /// <inheritdoc/>
            public void Dispose() {
                Try.Async(StopAsync).Wait();
                _cts?.Dispose();
            }

            /// <inheritdoc/>
            public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
                CancellationToken cancellationToken) {
                return Task.FromResult(_runner == null || !_runner.IsFaulted ?
                    HealthCheckResult.Healthy() :
                    new HealthCheckResult(HealthStatus.Unhealthy, null, _runner.Exception));
            }

            private Task _runner;
            private CancellationTokenSource _cts;
            private readonly Config _config;
        }
    }
}