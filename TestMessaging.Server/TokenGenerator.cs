using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TestMessaging.Server
{
    public class TokenGenerator
    {
        private readonly SecurityKey _securityKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly JwtSecurityTokenHandler _tokenHandler;

        public TokenGenerator()
        {
            _securityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("asdv234234^&%&^%&^hjsdfb2%%%"));
            _issuer = "http://chating.com";
            _audience = "http://chating-crowd.com";
            _tokenHandler = new JwtSecurityTokenHandler();
        }

        public string GenerateToken(string userName)
        {
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userName),
                }),
                Expires = DateTime.UtcNow.AddYears(7),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256Signature)
            };

            var token = _tokenHandler.CreateToken(tokenDescriptor);
            return _tokenHandler.WriteToken(token);
        }

        public bool ValidateToken(string token)
        {
            try
            {
                _tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = _issuer,
                    ValidAudience = _audience,
                    IssuerSigningKey = _securityKey
                }, out SecurityToken validatedToken);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public string GetUserName(string token)
        {
            if (token == null)
            {
                return null;
            }

            var securityToken = _tokenHandler.ReadToken(token) as JwtSecurityToken;

            var stringClaimValue = securityToken?.Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier).Value;
            return stringClaimValue;
        }
    }
}