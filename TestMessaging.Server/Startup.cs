using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TestMessaging.Common;
using TestMessaging.RabbitMq;
using TestMessaging.Server.Processors;
using TestMessaging.Server.Processors.Implementations;

namespace TestMessaging.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<RabbitMqConfiguration>(Configuration.GetSection("Billing"));
            services.AddLogging();
            services.AddSingleton<IRabbitMqClientFactory, RabbitMqClientFactory>();
            services.AddSingleton(_ =>
            {
                var clientFactory = _.GetService<IRabbitMqClientFactory>();
                var client = clientFactory.CreateClient<MessageReceivedEventArgs>();
                return client;
            });
            services.AddSingleton<IMessagePublisher<MessageReceivedEventArgs>>(_ =>
            {
                var client = _.GetService<IRabbitMqClient<MessageReceivedEventArgs>>();
                return client;
            });
            services.AddSingleton<IMessageConsumer<MessageReceivedEventArgs>>(_ =>
            {
                var client = _.GetService<IRabbitMqClient<MessageReceivedEventArgs>>();
                return client;
            });

            services.TryAddEnumerable(new[]
            {
                ServiceDescriptor.Singleton<IMessageProcessor, AuthMessageProcessor>(),
                //ДАлее реализовать процессоры сообщений
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseWebSockets();

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        using (var socket = await context.WebSockets.AcceptWebSocketAsync())
                        {
                            var socketProcessor = app.ApplicationServices.GetRequiredService<IUserSocketProcessor>();
                            using (var cancellationTokenSource = new CancellationTokenSource())
                            {
                                await socketProcessor.StartMessageProcessing(socket, cancellationTokenSource.Token);
                            }
                            
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }

            });
        }
    }
}
