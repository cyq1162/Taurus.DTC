using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Taurus.Plugin.DistributedTransaction;

namespace Microsoft.AspNetCore.Http
{
    public static partial class DTCExtensions
    {
        public static void AddTaurusDtc(this IServiceCollection services)
        {
            services.AddHttpContext();
        }

        public static IApplicationBuilder UseTaurusDtc(this IApplicationBuilder builder)
        {
            return UseTaurusDtc(builder, DTCStartType.None);
        }
        public static IApplicationBuilder UseTaurusDtc(this IApplicationBuilder builder, DTCStartType startType)
        {
            builder.UseHttpContext();
            switch (startType)
            {
                case DTCStartType.Client:
                    DTC.Client.Start();
                    break;
                case DTCStartType.Server:
                    DTC.Server.Start();
                    break;
                case DTCStartType.Both:
                    DTC.Start();
                    break;
            }
            return builder;
        }
    }
    public enum DTCStartType
    {
        /// <summary>
        /// 不设定，应用程序启动或重启时，不先启动数据扫描，由程序涉及调用相关函数时自动启动数据扫描。
        /// </summary>
        None,
        /// <summary>
        /// 启动时，进行客户端数据扫描。
        /// </summary>
        Client,
        /// <summary>
        /// 启动时，进行服务端数据扫描。
        /// </summary>
        Server,
        /// <summary>
        /// 启动时，对客户端和服务端都启动数据扫描。
        /// </summary>
        Both
    }
}
