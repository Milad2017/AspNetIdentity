﻿// Copyright (c) Microsoft Corporation, Inc. All rights reserved.
// Licensed under the MIT License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     TokenProvider that generates time based codes using the user's security stamp
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public class TotpSecurityStampBasedTokenProvider<TUser, TKey> : IUserTokenProvider<TUser, TKey>
        where TUser : class, IUser<TKey>
        where TKey : IEquatable<TKey>
    {
        /// <summary>
        ///     This token provider does not notify the user by default
        /// </summary>
        /// <param name="token"></param>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual Task NotifyAsync(string token, UserManager<TUser, TKey> manager, TUser user)
        {
            return Task.FromResult(0);
        }

        /// <summary>
        ///     Returns true if the provider can generate tokens for the user, by default this is equal to
        ///     manager.SupportsUserSecurityStamp
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual Task<bool> IsValidProviderForUserAsync(UserManager<TUser, TKey> manager, TUser user)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            return Task.FromResult(manager.SupportsUserSecurityStamp);
        }

        /// <summary>
        ///     Generate a token for the user using their security stamp
        /// </summary>
        /// <param name="purpose"></param>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<string> GenerateAsync(string purpose, UserManager<TUser, TKey> manager, TUser user)
        {
            var token = await manager.CreateSecurityTokenAsync(user.Id).WithCurrentCulture();
            var modifier = await GetUserModifierAsync(purpose, manager, user).WithCurrentCulture();
            return Rfc6238AuthenticationService.GenerateCode(token, modifier).ToString("D6", CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///     Validate the token for the user
        /// </summary>
        /// <param name="purpose"></param>
        /// <param name="token"></param>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser, TKey> manager,
            TUser user)
        {
            int code;
            if (!Int32.TryParse(token, out code))
            {
                return false;
            }
            var securityToken = await manager.CreateSecurityTokenAsync(user.Id).WithCurrentCulture();
            var modifier = await GetUserModifierAsync(purpose, manager, user).WithCurrentCulture();
            return securityToken != null && Rfc6238AuthenticationService.ValidateCode(securityToken, code, modifier);
        }

        /// <summary>
        ///     Used for entropy in the token, uses the user.Id by default
        /// </summary>
        /// <param name="purpose"></param>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual Task<string> GetUserModifierAsync(string purpose, UserManager<TUser, TKey> manager, TUser user)
        {
            return Task.FromResult("Totp:" + purpose + ":" + user.Id);
        }
    }
}