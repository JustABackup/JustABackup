﻿using JustABackup.Base;
using JustABackup.Core.Entities;
using JustABackup.Database;
using JustABackup.Database.Entities;
using JustABackup.Database.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JustABackup.Core.Services
{
    public interface IProviderMappingService
    {
        Task<T> CreateProvider<T>(long providerInstanceId) where T : class;

        Task<T> CreateProvider<T>(ProviderInstance providerInstance) where T : class;

        PropertyType GetTypeFromProperty(PropertyInfo property, out Type genericParameter);

        string GetTemplateFromType(PropertyType type);

        object GetObjectFromString(string value, PropertyType propertyType, string genericType = null);
    }

    public class ProviderMappingService : IProviderMappingService
    {
        private DefaultContext dbContext;

        public ProviderMappingService(DefaultContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<T> CreateProvider<T>(long providerInstanceId) where T : class
        {
            ProviderInstance providerInstance = await dbContext
                .ProviderInstances
                .Include(pi => pi.Provider)
                .Include(pi => pi.Values)
                .ThenInclude(pip => pip.Property)
                .FirstOrDefaultAsync(pi => pi.ID == providerInstanceId);

            return await CreateProvider<T>(providerInstance);
        }

        public Task<T> CreateProvider<T>(ProviderInstance providerInstance) where T : class
        {
            Type providerType = Type.GetType(providerInstance.Provider.Namespace);
            T convertedProvider = Activator.CreateInstance(providerType) as T;

            foreach (var property in providerInstance.Values)
            {
                PropertyInfo propertyInfo = providerType.GetProperty(property.Property.TypeName);
                object originalValueType = this.GetObjectFromString(property.Value, property.Property.Type, property.Property.GenericType);

                propertyInfo.SetValue(convertedProvider, originalValueType);
            }

            return Task.FromResult(convertedProvider);
        }

        public object GetObjectFromString(string value, PropertyType propertyType, string genericType = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            switch (propertyType)
            {
                case PropertyType.Bool:
                    return Convert.ToBoolean(value);

                case PropertyType.Number:
                    return Convert.ToInt64(value);

                case PropertyType.String:
                    return value;

                case PropertyType.Authentication:
                    if (string.IsNullOrWhiteSpace(genericType))
                    {
                        return new AuthenticatedClient<object>(Convert.ToInt64(value), this, dbContext);
                    }
                    else
                    {
                        Type authenticatedClientType = typeof(AuthenticatedClient<>);
                        authenticatedClientType = authenticatedClientType.MakeGenericType(Type.GetType(genericType));
                        return Activator.CreateInstance(authenticatedClientType, Convert.ToInt64(value), this, dbContext);
                    }
            }

            return null;
        }

        public string GetTemplateFromType(PropertyType type)
        {
            switch (type)
            {
                case PropertyType.String: return "String";
                case PropertyType.Number: return "Number";
                case PropertyType.Bool: return "Boolean";
                case PropertyType.Authentication: return "AuthenticationProvider";

                default: return "String";
            }
        }

        public PropertyType GetTypeFromProperty(PropertyInfo property, out Type genericParameter)
        {
            genericParameter = null;

            if (property.PropertyType == typeof(string))
                return PropertyType.String;

            if (property.PropertyType == typeof(int))
                return PropertyType.Number;

            if (property.PropertyType == typeof(long))
                return PropertyType.Number;

            if (property.PropertyType == typeof(bool))
                return PropertyType.Bool;

            if (property.PropertyType.GetGenericTypeDefinition() == typeof(IAuthenticatedClient<>))
            {
                genericParameter = property.PropertyType.GenericTypeArguments[0];
                return PropertyType.Authentication;
            }

            throw new ArgumentOutOfRangeException(nameof(property));
        }
    }
}