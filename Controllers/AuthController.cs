﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using T2207A_API.Entities;
using T2207A_API.Models.User;
using T2207A_API.DTOs;
using BCrypt.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace T2207A_API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly T2207aApiContext _context;
        private readonly IConfiguration _config;

        public AuthController(T2207aApiContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        private string GenJWT(User user)
        {
            var secretKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_config["JWT:Key"]));
            var signatureKey = new SigningCredentials(secretKey,
                                    SecurityAlgorithms.HmacSha256);
            var payload = new[]
            {
                new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
                new Claim(ClaimTypes.Email,user.Email),
                new Claim(ClaimTypes.Name,user.Fullname),
                new Claim(ClaimTypes.Role,"user")
            };
            var token = new JwtSecurityToken(
                    _config["JWT:Issuer"],
                    _config["JWT:Audience"],
                    payload,
                    expires: DateTime.Now.AddMinutes(60),
                    signingCredentials: signatureKey
                );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }



        [HttpPost]
        [Route("register")]
        public IActionResult Register(UserRegister model)
        {
            try
            {
                var salt = BCrypt.Net.BCrypt.GenerateSalt(10);
                var hashed = BCrypt.Net.BCrypt.HashPassword(model.password, salt);
                var user = new User
                {
                    Email = model.email,
                    Fullname = model.full_name,
                    Password = hashed
                };
                _context.Users.Add(user);
                _context.SaveChanges();
                return Ok(new UserDTO
                {
                    id = user.Id,
                    email = user.Email,
                    full_name = user.Fullname,
                    token = GenJWT(user)
                });
            }
            catch (Exception e)
            {
                return Unauthorized(e.Message);
            }
        }
        [HttpPost]
        [Route("login")]
        public IActionResult Login(UserLogin model)
        {
            try
            {
                var user = _context.Users.Where(u => u.Email.Equals(model.email))
                            .First();
                if (user == null)
                {
                    throw new Exception("Email or Password is not correct");
                }
                bool verified = BCrypt.Net.BCrypt.Verify(model.password, user.Password);
                if (!verified)
                {
                    throw new Exception("Email or Password is not correct");
                }
                return Ok(new UserDTO
                {
                    id = user.Id,
                    email = user.Email,
                    full_name = user.Fullname,
                    token = GenJWT(user)
                });

            }
            catch (Exception e)
            {
                return Unauthorized(e.Message);
            }
        }

        [HttpGet]
        [Route("profile")]
        public IActionResult Profile()
        {
            // get info from token
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            if (!identity.IsAuthenticated)
            {
                return Unauthorized("Not Authorized");
            }
            try
            {
                var userClaims = identity.Claims;
                var userId = userClaims.FirstOrDefault(c =>
                    c.Type == ClaimTypes.NameIdentifier)?.Value;
                var user = _context.Users.Find(Convert.ToInt32(userId));
                return Ok(new UserDTO // đúng ra phải là UserProfileDTO
                {
                    id = user.Id,
                    email = user.Email,
                    full_name = user.Fullname
                });
            }
            catch (Exception e)
            {
                return Unauthorized(e.Message);
            }
            return Ok();
        }
    }
}