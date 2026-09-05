using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace GtMotive.Estimate.Microservice.InfrastructureTests.Infrastructure
{
    public static class TestAuthenticationExtensions
    {
        public static HttpClient WithTestToken(this HttpClient client, params string[] roles)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(roles);

            var claims = new List<Claim>
            {
                new("sub", "test-renter"),
                new(ClaimTypes.NameIdentifier, "test-renter")
            };
            foreach (var role in roles)
            {
                claims.Add(new(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtTestServerFixture.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: JwtTestServerFixture.Issuer,
                audience: JwtTestServerFixture.Audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(5),
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenString);
            return client;
        }
    }
}
