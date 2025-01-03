﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Contracts;
using Entities;
using Entities.Exceptions;
using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared;

namespace Service
{
    internal sealed class AuthenticationService : IAuthenticationService, IApiService
    {
        private readonly JwtConfiguration _jwtConfig;
        private readonly ILogger<IApiService> _logger;
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;
        private User? _user;

        public AuthenticationService(ILogger<IApiService> logger, IMapper mapper, UserManager<User>
            userManager, IOptions<JwtConfiguration> configuration)
        {
            _logger = logger;
            _mapper = mapper;
            _userManager = userManager;
            _jwtConfig = configuration.Value;
        }

        public async Task<IdentityResult> RegisterUser(UserForRegistrationDto
            userForRegistrationDto)
        {
            var user = _mapper.Map<User>(userForRegistrationDto);
            var result = await _userManager.CreateAsync(user, userForRegistrationDto.Password!);

            if (result.Succeeded)
                await _userManager.AddToRolesAsync(user, userForRegistrationDto.Roles);

            return result;
        }

        public async Task<bool> ValidateUser(UserForAuthenticationDto userForAuth)
        {
            _user = await _userManager.FindByEmailAsync(userForAuth.UserName!);
            var result = _user != null && await _userManager.CheckPasswordAsync(_user,
                userForAuth.Password ?? string.Empty);
            if (!result)
                _logger.LogWarning(
                    "'{MethodName}': Authentication failed. Wrong username or password.",
                    nameof(ValidateUser));

            return result;
        }
        
        public async Task<User> GetAuthenticatedUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                throw new UserNotFoundException();
            }

            return user;
        }

        public async Task<TokenDto> CreateToken(bool populateExp)
        {
            var signingCredentials = GetSigningCredentials();
            var claims = await GetClaims();
            var tokenOptions = GenerateTokenOptions(signingCredentials, claims);

            if (_user == null) throw new UserNotFoundException();
            
            var refreshToken = GenerateRefreshToken();
            _user.RefreshToken = refreshToken;
            if (populateExp)
                _user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);
            await _userManager.UpdateAsync(_user);
            var accessToken = new JwtSecurityTokenHandler().WriteToken(tokenOptions);
            return new TokenDto(accessToken, refreshToken, _user.UserName);
        }

        public async Task<TokenDto> RefreshToken(TokenDto tokenDto)
        {
            var principal = GetPrincipalFromExpiredToken(tokenDto.AccessToken);
            if (principal?.Identity == null) throw new RefreshTokenBadRequestException();
            var user = await _userManager.FindByNameAsync(principal.Identity.Name!);
            if (user == null || user.RefreshToken != tokenDto.RefreshToken ||
                user.RefreshTokenExpiryTime <= DateTime.Now)
                throw new RefreshTokenBadRequestException();
            _user = user;
            return await CreateToken(false);
        }

        private JwtSecurityToken GenerateTokenOptions(SigningCredentials signingCredentials,
            List<Claim> claims)
        {
            var tokenOptions = new JwtSecurityToken
            (
                _jwtConfig.ValidIssuer,
                _jwtConfig.ValidAudience,
                claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_jwtConfig.Expires)),
                signingCredentials: signingCredentials
            );
            return tokenOptions;
        }

        private async Task<List<Claim>> GetClaims()
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, _user?.UserName!),
                new(ClaimTypes.NameIdentifier, _user?.Id!),
                new(ClaimTypes.Email, _user?.Email!)
            };
            var roles = await _userManager.GetRolesAsync(_user!);
            foreach (var role in roles) claims.Add(new Claim(ClaimTypes.Role, role));
            return claims;
        }

        private static SigningCredentials GetSigningCredentials()
        {
            var key = Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable
                ("ASPNETCORE_RANQUE_SECRET")!);
            var secret = new SymmetricSecurityKey(key);
            return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        Environment.GetEnvironmentVariable("ASPNETCORE_RANQUE_SECRET")!)),
                ValidateLifetime = true,
                ValidIssuer = _jwtConfig.ValidIssuer,
                ValidAudience = _jwtConfig.ValidAudience
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters,
                out var securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");
            return principal;
        }
    }
}