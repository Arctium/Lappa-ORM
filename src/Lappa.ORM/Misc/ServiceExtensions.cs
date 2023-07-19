// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lappa.ORM.Misc
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddDatabase<TDatabase>(this IServiceCollection services, IConfiguration namedConfigurationSection) where TDatabase : Database<TDatabase>
        {
            services.Configure<ConnectionSettings>(typeof(TDatabase).Name, namedConfigurationSection);
            services.AddSingleton<IDatabase<TDatabase>, TDatabase>();

            return services;
        }

        public static IServiceCollection AddDatabase<TDatabase>(this IServiceCollection services, IConfigurationSection childConfigurationSection) where TDatabase : Database<TDatabase>
        {
            services.Configure<ConnectionSettings>(typeof(TDatabase).Name, childConfigurationSection.GetSection(typeof(TDatabase).Name));
            services.AddSingleton<IDatabase<TDatabase>, TDatabase>();

            return services;
        }

        public static IServiceCollection AddDatabase<TDatabase>(this IServiceCollection services, Action<ConnectionSettings> configureOptions) where TDatabase : Database<TDatabase>
        {
            services.Configure(typeof(TDatabase).Name, configureOptions);
            services.AddSingleton<IDatabase<TDatabase>, TDatabase>();

            return services;
        }
    }
}
