﻿using aspnetcore.ntier.BLL.Services.IServices;
using aspnetcore.ntier.DAL.Repositories.IRepositories;
using aspnetcore.ntier.DTO.DTOs;
using aspnetcore.ntier.Entity.Entities;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace aspnetcore.ntier.BLL.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    public AuthService(
        IUserRepository userRepository,
        IMapper mapper,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _configuration = configuration;
    }

    public async Task<UserDTO> Login(UserToLoginDTO userToLoginDTO)
    {
        var user = await _userRepository.Get(
            u => u.Username == userToLoginDTO.Username.ToLower() && u.Password == userToLoginDTO.Password);

        if (user == null)
            return null;

        var userToReturn = _mapper.Map<UserDTO>(user);
        userToReturn.Token = GenerateToken(user.UserId, user.Username);

        return userToReturn;
    }

    public async Task<UserDTO> Register(UserToRegisterDTO userToRegisterDTO)
    {
        userToRegisterDTO.Username = userToRegisterDTO.Username.ToLower();

        var addedUser = await _userRepository.Add(_mapper.Map<User>(userToRegisterDTO));

        var userToReturn = _mapper.Map<UserDTO>(addedUser);
        userToReturn.Token = GenerateToken(addedUser.UserId, addedUser.Username);

        return userToReturn;
    }

    private string GenerateToken(int userId, string username)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("AppSettings:Token").Value));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now.AddDays(1),
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}