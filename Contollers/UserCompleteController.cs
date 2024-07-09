// naming on this is important, default set up where our route will match the 
// name of the controller

using System.Data;
using Dapper;
using DotnetAPI.Data;
using DotnetAPI.Models;
using DotNetApi.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DotnetAPI.Controllers;
[Authorize]
[ApiController] // tag gives us some built in handling
[Route("[controller]")] // logic that is going to reach into our controller and insert into route
// for example //WeatherForecast
public class UserCompleteController : ControllerBase
{
    private readonly DataContextDapper _dapper;
    private readonly ReuseableSql _reuseableSql;

    public UserCompleteController(IConfiguration config)
    {
        _dapper = new DataContextDapper(config);
        _reuseableSql = new ReuseableSql(config);
        // this is a constructor
    }

    [HttpGet("GetUsers/{userId}/{isActive}")]
    // the above is an end point 
    public IEnumerable<UserComplete> GetUsers(int userId, bool isActive)
    {
        string sql = @"EXEC TutorialAppSchema.spUsers_Get";

        string stringParameters = "";
        DynamicParameters sqlParameters = new DynamicParameters();

        if (userId != 0)
        {
            stringParameters += ", @UserId=@UserIdParameter";
            sqlParameters.Add("@UserIdParameter", userId, DbType.Int32);
        }
        if (isActive)
        {
            stringParameters += ", @Active=@ActiveParameter";
            sqlParameters.Add("@ActiveParameter", isActive, DbType.Boolean);
        }

        if (stringParameters.Length > 0)
        {
            sql += stringParameters.Substring(1); // this ensures the first comma is left out for correct sql syntax
        }

        IEnumerable<UserComplete> users = _dapper.LoadDataWithParameters<UserComplete>(sql, sqlParameters);
        return users;
    }


    [HttpPut("UpsertUser")]
    public IActionResult UpsertUser(UserComplete user)
    // end point is looking for a model to be passed in with the payload
    {
        if (_reuseableSql.UpsertUser(user))
        {
            return Ok();
        }
        throw new Exception("Failed to Update User");
    }



    [HttpDelete("DeleteUser/{userId}")]
    public IActionResult DeleteUser(int userId)
    {
        string sql = @"
            EXEC TutorialAppSchema.spUser_Delete
                @UserId = @UserIdParam";

        DynamicParameters sqlParameters = new DynamicParameters();
        sqlParameters.Add("@UserIdParam", userId, DbType.Int32);

        if (_dapper.ExecuteSqlWithParameters(sql, sqlParameters))
        {
            return Ok();
        }
        throw new Exception("Failed to Delete User");
    }


}

internal class ReusableSql
{
}