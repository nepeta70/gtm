using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace GtMotive.Estimate.Microservice.Infrastructure.Authorization
{
    /// <summary>
    /// Generates HS256-signed JWTs for local development, Docker Compose and manual testing
    /// scenarios where no external identity provider is available.
    /// The secret used here must match the API's Jwt:Secret (Jwt__Secret) configuration.
    /// This is a development convenience only and must never be used to issue production tokens.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class DevJwtTokenGenerator
    {
        /// <summary>
        /// Creates a signed JWT using the supplied symmetric secret.
        /// </summary>
        /// <param name="secret">The symmetric signing secret. Must match the API's Jwt:Secret / Jwt__Secret configuration.</param>
        /// <param name="subject">The value of the "sub" claim identifying the token bearer.</param>
        /// <param name="roles">Optional role claims to include in the token.</param>
        /// <param name="issuer">Optional issuer ("iss") claim. Must match Jwt:Issuer if the API validates it.</param>
        /// <param name="audience">Optional audience ("aud") claim. Must match Jwt:Audience if the API validates it.</param>
        /// <param name="lifetime">How long the token remains valid. Defaults to one hour.</param>
        /// <returns>The encoded JWT string.</returns>
        public static string GenerateToken(
            string secret,
            string subject,
            IEnumerable<string> roles = null,
            string issuer = null,
            string audience = null,
            TimeSpan? lifetime = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(secret);
            ArgumentException.ThrowIfNullOrWhiteSpace(subject);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, subject),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            if (roles is not null)
            {
                foreach (var role in roles.Where(role => !string.IsNullOrWhiteSpace(role)))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromHours(1));

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expires,
                signingCredentials: signingCredentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
