using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Dapper;
using DotnetAPI.Data;
using DotnetAPI.Models;
using DotNetApi.Dtos;
using DotNetApi.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;

namespace DotNetApi.Controllers
{
    [Authorize]
    // we can use to tell conntroller that we want to authorise user to be able to use this controller
    // allow anonymous below allows user to access regardless
    [ApiController] // tag gives us some built in handling
    [Route("[controller]")] // logic that is going to reach into our controller and insert into route
                            // for example //WeatherForecast
    public class AuthController : ControllerBase
    {

        private readonly DataContextDapper _dapper;
        private readonly AuthHelper _authHelper;
        private readonly ReuseableSql _reuseableSql;
        private readonly IMapper _mapper;

        public AuthController(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
            _authHelper = new AuthHelper(config);
            _reuseableSql = new ReuseableSql(config);
            _mapper = new Mapper(new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<UserForRegistrationDto, UserComplete>();
            }));
            /* This constructor gives our auth controller direct access to config file 
            and anb instance of our datacontext with Dapper*/
        }

        [AllowAnonymous]
        [HttpPost("Register")]
        public IActionResult Register(UserForRegistrationDto userForRegistration)
        {
            if (userForRegistration.Password == userForRegistration.PasswordConfirm)
            {
                string sqlCheckUserExists = "SELECT Email FROM TutorialAppSchema.Auth WHERE Email = '" +
                    userForRegistration.Email + "'";

                IEnumerable<string> existingUsers = _dapper.LoadData<string>(sqlCheckUserExists);
                if (existingUsers.Count() == 0)
                {
                    UserForLoginDto userForSetPassword = new UserForLoginDto()
                    {
                        Email = userForRegistration.Email,
                        Password = userForRegistration.Password,
                    };
                    if (_authHelper.SetPassword(userForSetPassword))
                    {
                        UserComplete userComplete = _mapper.Map<UserComplete>(userForRegistration);
                        userComplete.Active = true;

                        if (_reuseableSql.UpsertUser(userComplete))
                        {
                            return Ok();
                        }
                        throw new Exception("Failed to add user.");

                    }
                    throw new Exception("Failed to register user.");
                }
                throw new Exception("User with this email already exists!");
            }
            throw new Exception("Passwords do not match!");
        }


        [HttpPut("ResetPassword")]

        public IActionResult ResetPassword(UserForLoginDto userForSetPassword)
        {
            if (_authHelper.SetPassword(userForSetPassword))
            {
                return Ok();
            }
            throw new Exception("Failed to update password!");
        }


        [AllowAnonymous]
        [HttpPost("Login")]
        public IActionResult Login(UserForLoginDto userForLogin)
        {
            string sqlForHashAndSalt = @"EXEC TutorialAppSchema.spLoginConfirmation_Get
                @Email = @EmailParam";

            // list of sql parameters
            DynamicParameters sqlParameters = new DynamicParameters();

            sqlParameters.Add("@EmailParam", userForLogin.Email, DbType.String);

            UserForLoginConfirmationDto userForConfirmation = _dapper
                .LoadDataSingleWithParameters<UserForLoginConfirmationDto>(sqlForHashAndSalt, sqlParameters);

            byte[] passwordHash = _authHelper.getPasswordHash(userForLogin.Password, userForConfirmation.PasswordSalt);

            // cant directly compare as they are objects
            // instead we need to loop through and compare the bytes at each index

            for (int index = 0; index < passwordHash.Length; index++)
            {
                if (passwordHash[index] != userForConfirmation.PasswordHash[index])
                {
                    return StatusCode(401, "Password was incorrect");
                }
            }

            string userIdSql = @"EXEC TutorialAppSchema.spUserId_Get 
            @Email = @EmailParam";

            int userId = _dapper.LoadDataSingleWithParameters<int>(userIdSql, sqlParameters);

            // can return token to user, so that they don't need to keep authorising
            // themselves over and over
            return Ok(new Dictionary<string, string> {
                {"token", _authHelper.CreateToken(userId)}
            });
        }


        [HttpGet("RefreshToken")]

        public IActionResult RefreshToken()
        {
            string userId = User.FindFirst("userId")?.Value + "";

            string userIdSql = @"EXEC TutorialAppSchema.spUserId_Get
            @UserId = @UserIdParam";

            DynamicParameters sqlParameters = new DynamicParameters();

            sqlParameters.Add("@UserIdParam", userId, DbType.Int32);

            int userIdFromDB = _dapper.LoadDataSingleWithParameters<int>(userIdSql, sqlParameters);

            return Ok(new Dictionary<string, string> {
                {"token", _authHelper.CreateToken(userIdFromDB)}
            });
        }



    }
}